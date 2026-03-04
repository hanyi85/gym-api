using gym_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace gym_api.Controllers.post
{
    // 統一接收前端傳來的報名資訊
    public class LinePayRequestDto
    {
        public int PostId { get; set; }
        public int EventId { get; set; }
        public int? UserId { get; set; }
        public string Name { get; set; }
        public string Sex { get; set; } // "1", "0", "2"
        public string Email { get; set; }
        public string Phone { get; set; }
        public decimal Fee { get; set; }
        public string PaymentMethod { get; set; }
        public string CaptchaToken { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class YLINEPAYController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        // LINE Pay 憑證 (由您提供)
        private readonly string _channelId = "2009062308";
        private readonly string _channelSecret = "85e328e8b1b73e71004799a938720ea0";
        private readonly string _linePayBaseUrl = "https://sandbox-api-pay.line.me";

        // Gmail SMTP 設定 (由您提供)
        private readonly string _gmailAccount = "o09750397@gmail.com";
        private readonly string _gmailAppPassword = "ywlnjieakdcxewen";

        public YLINEPAYController(dbFitness2Context context)
        {
            _context = context;
        }

        /// <summary>
        /// 1. 建立訂單並取得 LINE Pay 付款連結
        /// </summary>
        [HttpPost("RequestPayment")]
        public async Task<IActionResult> RequestPayment([FromBody] LinePayRequestDto dto)
        {
            if (dto == null) return BadRequest(new { message = "請提供完整的報名資料" });

            try
            {
                // A. 建立資料庫紀錄 (PayStatus = 0 代表尚未付款)
                int.TryParse(dto.Sex, out int sexValue);
                var newJoin = new YJoinForm
                {
                    EventId = dto.EventId,
                    UserId = dto.UserId == 0 ? null : dto.UserId,
                    Name = dto.Name,
                    Sex = sexValue,
                    Phone = dto.Phone,
                    Email = dto.Email,
                    Fee = dto.Fee,
                    PayMethod = 1, // 1 代表 LINE Pay
                    PayStatus = 0,
                    CreatedAt = DateTime.Now,
                    Status = 1, // 1 代表有效
                    IsDeleted = false
                };

                _context.YJoinForms.Add(newJoin);
                await _context.SaveChangesAsync();

                // B. 準備 LINE Pay 請求內容
                string orderId = $"FIT_{newJoin.JoinFormId}_{DateTime.Now.Ticks}";
                var linePayBody = new
                {
                    amount = (int)dto.Fee,
                    currency = "TWD",
                    orderId = orderId,
                    packages = new[] {
                        new {
                            id = "pkg_01",
                            amount = (int)dto.Fee,
                            name = "FitnessBar 活動報名費",
                            products = new[] {
                                new {
                                    name = "報名費用",
                                    quantity = 1,
                                    price = (int)dto.Fee
                                }
                            }
                        }
                    },
                    redirectUrls = new
                    {
                        // 付款成功後 Vue 要跳轉的地址
                        confirmUrl = $"http://localhost:5173/post/confirm?joinId={newJoin.JoinFormId}",
                        cancelUrl = "http://localhost:5173/post/list"
                    }
                };

                string jsonBody = JsonConvert.SerializeObject(linePayBody);
                string nonce = Guid.NewGuid().ToString();
                string signature = GenerateSignature(_channelSecret, "/v3/payments/request", jsonBody, nonce);

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("X-LINE-ChannelId", _channelId);
                client.DefaultRequestHeaders.Add("X-LINE-Authorization-Nonce", nonce);
                client.DefaultRequestHeaders.Add("X-LINE-Authorization", signature);

                var response = await client.PostAsync($"{_linePayBaseUrl}/v3/payments/request",
                    new StringContent(jsonBody, Encoding.UTF8, "application/json"));

                var result = await response.Content.ReadAsStringAsync();
                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                // 這樣你會在瀏覽器 F12 看到到底是哪裡出錯 (例如：哪個欄位不能是 Null)
                return StatusCode(500, new
                {
                    message = "伺服器內部出錯",
                    error = ex.Message,
                    stackTrace = ex.StackTrace,
                    inner = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// 2. 付款確認 (Vue 跳轉回來後呼叫)
        /// </summary>
        [HttpGet("ConfirmPayment")]
        public async Task<IActionResult> ConfirmPayment(string transactionId, int joinId)
        {
            // 1. 撈取訂單，確保訂單存在且尚未付款
            var joinRecord = await _context.YJoinForms
                .Include(j => j.Event).ThenInclude(e => e.Post)
                .FirstOrDefaultAsync(j => j.JoinFormId == joinId);

            if (joinRecord == null) return NotFound(new { message = "查無此報名紀錄" });
            if (joinRecord.PayStatus == 1) return Ok(new { returnCode = "0000", message = "此訂單已完成付款", email = joinRecord.Email });

            try
            {
                // 2. 準備呼叫 LINE Pay Confirm API (這是最關鍵的一步，沒這段等於沒收錢)
                var confirmBody = new
                {
                    amount = (int)joinRecord.Fee,
                    currency = "TWD"
                };

                string jsonBody = JsonConvert.SerializeObject(confirmBody);
                string nonce = Guid.NewGuid().ToString();
                // 注意：URI 必須包含 transactionId
                string requestUri = $"/v3/payments/{transactionId}/confirm";
                string signature = GenerateSignature(_channelSecret, requestUri, jsonBody, nonce);

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("X-LINE-ChannelId", _channelId);
                client.DefaultRequestHeaders.Add("X-LINE-Authorization-Nonce", nonce);
                client.DefaultRequestHeaders.Add("X-LINE-Authorization", signature);

                var response = await client.PostAsync($"{_linePayBaseUrl}{requestUri}",
                    new StringContent(jsonBody, Encoding.UTF8, "application/json"));

                var resultText = await response.Content.ReadAsStringAsync();
                dynamic resultJson = JsonConvert.DeserializeObject(resultText);

                // 3. 判斷 LINE Pay 回傳結果
                if (resultJson.returnCode == "0000")
                {
                    // 4. 正式更新資料庫
                    joinRecord.PayStatus = 1;
                    joinRecord.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();

                    // 5. 異步寄信 (不阻塞主執行緒，讓前端快點拿到結果)
                    _ = Task.Run(() => SendSuccessEmailAsync(joinRecord));

                    return Ok(new
                    {
                        returnCode = "0000",
                        message = "付款成功",
                        email = joinRecord.Email // 回傳 Email 讓前端顯示
                    });
                }
                else
                {
                    return BadRequest(new { message = "LINE Pay 確認失敗", linePayError = resultJson.returnMessage });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "伺服器確認程序出錯", error = ex.Message });
            }
        }

        // --- 私有輔助方法：SMTP 寄信 ---
        private async Task SendSuccessEmailAsync(YJoinForm join)
        {
            try
            {
                // 1. 提取活動詳細資訊 (使用 Null-conditional 確保安全)
                var eventTitle = join.Event?.Post?.Title ?? "健身活動";
                var eventVenue = join.Event?.Venue ?? "活動現場";
                var eventDate = join.Event?.StartDate.ToString("yyyy/MM/dd HH:mm") ?? "另行通知";

                // 2. 設定 SMTP 客戶端 (使用 using 確保發送完畢後正確關閉連線)
                using (var smtpClient = new SmtpClient("smtp.gmail.com"))
                {
                    smtpClient.Port = 587;
                    smtpClient.Credentials = new NetworkCredential(_gmailAccount, _gmailAppPassword);
                    smtpClient.EnableSsl = true;

                    // 3. 建立郵件內容 (使用 using 確保資源釋放)
                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress(_gmailAccount, "FitnessBar 健身小幫手");
                        mailMessage.Subject = $"🔥 報名成功！{eventTitle} - {join.Name} 期待您的參與";
                        mailMessage.IsBodyHtml = true;
                        mailMessage.To.Add(join.Email);

                        // HTML 郵件模板
                        mailMessage.Body = $@"
                <div style='max-width: 600px; margin: auto; font-family: Microsoft JhengHei, sans-serif; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
                    <div style='background-color: #f3722c; padding: 20px; text-align: center;'>
                        <h1 style='color: white; margin: 0;'>報名確認通知</h1>
                    </div>
                    <div style='padding: 30px; color: #333; line-height: 1.6;'>
                        <p style='font-size: 18px;'>您好，<strong>{join.Name}</strong>：</p>
                        <p>恭喜您！我們已成功收到款項，您的名額已正式保留。以下是活動的詳細資訊：</p>
                        
                        <div style='background-color: #fff8f5; border-left: 5px solid #f3722c; padding: 15px; margin: 20px 0;'>
                            <p style='margin: 5px 0;'><strong>📌 活動名稱：</strong> {eventTitle}</p>
                            <p style='margin: 5px 0;'><strong>📅 活動時間：</strong> {eventDate}</p>
                            <p style='margin: 5px 0;'><strong>📍 活動地點：</strong> {eventVenue}</p>
                        </div>

                        <table style='width: 100%; border-collapse: collapse; margin-top: 20px;'>
                            <tr style='background-color: #f8f8f8;'>
                                <th style='padding: 10px; border-bottom: 1px solid #ddd; text-align: left;'>報名編號</th>
                                <td style='padding: 10px; border-bottom: 1px solid #ddd;'>#{join.JoinFormId}</td>
                            </tr>
                            <tr>
                                <th style='padding: 10px; border-bottom: 1px solid #ddd; text-align: left;'>實付金額</th>
                                <td style='padding: 10px; border-bottom: 1px solid #ddd; color: #d9534f; font-weight: bold;'>NT$ {join.Fee}</td>
                            </tr>
                            <tr style='background-color: #f8f8f8;'>
                                <th style='padding: 10px; border-bottom: 1px solid #ddd; text-align: left;'>付款方式</th>
                                <td style='padding: 10px; border-bottom: 1px solid #ddd;'>LINE Pay</td>
                            </tr>
                        </table>

                        <p style='margin-top: 30px;'>請於活動開始前 15 分鐘抵達現場進行簽到。期待見到您！</p>
                    </div>
                    <div style='background-color: #eeeeee; padding: 15px; text-align: center; font-size: 12px; color: #777;'>
                        此信件由系統自動發出，請勿直接回覆。<br>
                        © 2026 FitnessBar 健身Bar 版權所有
                    </div>
                </div>";

                        // 4. 執行發送
                        await smtpClient.SendMailAsync(mailMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                // 記錄錯誤 (在實際專案中建議使用 ILogger 記錄到檔案或資料庫)
                // 這裡暫時輸出至控制台，確保寄信失敗不會導致整個 API 崩潰
                Console.WriteLine($"[Email Error] 寄送失敗給 {join.Email}: {ex.Message}");
            }
        }        // --- 私有輔助方法：LINE Pay 簽章 ---
        private string GenerateSignature(string secret, string uri, string body, string nonce)
        {
            var signatureRaw = $"{secret}{uri}{body}{nonce}";
            var keyByte = Encoding.UTF8.GetBytes(secret);
            var messageBytes = Encoding.UTF8.GetBytes(signatureRaw);

            using var hmacsha256 = new HMACSHA256(keyByte);
            byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
            return Convert.ToBase64String(hashmessage);
        }
        
    }
}