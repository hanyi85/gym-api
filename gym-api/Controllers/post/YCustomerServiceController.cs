using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gym_api.Models;
using Newtonsoft.Json;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.DependencyInjection;

namespace gym_api.Controllers.post
{
    [Route("api/[controller]")]
    [ApiController]
    public class YCustomerServiceController : ControllerBase
    {
        private readonly dbFitness2Context _context;
        private readonly IServiceScopeFactory _scopeFactory; // 用於建立背景任務的 Scope

        // Google reCAPTCHA Secret Key
        private readonly string _reCaptchaSecret = "6LcNAHcsAAAAAAj_BM62lS9Ab6Kplzo0B8wPq1X_";

        // Gmail SMTP 設定
        private readonly string _gmailAccount = "o09750397@gmail.com";
        private readonly string _gmailAppPassword = "ywlnjieakdcxewen";

        public YCustomerServiceController(dbFitness2Context context, IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _scopeFactory = scopeFactory;
        }

        [HttpGet("Categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.YQuestionCategories
                .Where(c => c.IsActive == true)
                .Select(c => new
                {
                    id = c.QuestionCategoryId,
                    name = c.Name
                })
                .ToListAsync();

            return Ok(categories);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromForm] CustomerServiceUploadDto uploadData)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "資料驗證失敗" });
            }

            // 1. Google reCAPTCHA 驗證
            if (string.IsNullOrEmpty(uploadData.CaptchaToken))
            {
                return BadRequest(new { success = false, message = "請完成機器人驗證" });
            }

            using var client = new HttpClient();
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", _reCaptchaSecret),
                new KeyValuePair<string, string>("response", uploadData.CaptchaToken)
            });

            var response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
            var resultJson = await response.Content.ReadAsStringAsync();
            var captchaResult = JsonConvert.DeserializeObject<ReCaptchaInternalResult>(resultJson);

            if (captchaResult == null || !captchaResult.Success)
            {
                return BadRequest(new { success = false, message = "機器人驗證失敗，請重新整理頁面再試" });
            }

            // 2. 執行資料存檔
            try
            {
                var newRecord = new YCustomerService
                {
                    Name = uploadData.Name,
                    Phone = uploadData.Phone,
                    Email = uploadData.Email,
                    QuestionCategoryId = uploadData.QuestionCategoryId,
                    Detail = uploadData.Detail,
                    Status = 0, // 0: 待處理
                    CreatedAt = DateTime.Now,
                    IsDeleted = false,
                    Sex = 0
                };

                if (uploadData.ImageFile != null && uploadData.ImageFile.Length > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        await uploadData.ImageFile.CopyToAsync(ms);
                        newRecord.Image = ms.ToArray();
                    }
                }

                _context.YCustomerServices.Add(newRecord);
                await _context.SaveChangesAsync();

                // 3. 異步寄送通知信 (傳入 ID，避免 Context 釋放問題)
                int recordId = newRecord.CustomerServiceId;
                _ = Task.Run(() => SendSupportConfirmEmailAsync(recordId));

                return Ok(new { success = true, message = "回報成功送出！" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"伺服器發生錯誤: {ex.Message}" });
            }
        }

        // --- 私有輔助方法：SMTP 寄送 ---
        private async Task SendSupportConfirmEmailAsync(int recordId)
        {
            // 在 Task.Run 中必須建立新的 Scope 來獲取 DbContext
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<dbFitness2Context>();

                try
                {
                    // 從資料庫抓取最新紀錄與分類名稱
                    var record = await dbContext.YCustomerServices
                        .FirstOrDefaultAsync(r => r.CustomerServiceId == recordId);

                    if (record == null) return;

                    var category = await dbContext.YQuestionCategories
                        .FirstOrDefaultAsync(c => c.QuestionCategoryId == record.QuestionCategoryId);
                    string categoryName = category?.Name ?? "一般問題";

                    // 強制指定 TLS 1.2
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    using (var smtpClient = new SmtpClient("smtp.gmail.com"))
                    {
                        smtpClient.Port = 587;
                        smtpClient.Credentials = new NetworkCredential(_gmailAccount, _gmailAppPassword);
                        smtpClient.EnableSsl = true;
                        smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

                        using (var mailMessage = new MailMessage())
                        {
                            mailMessage.From = new MailAddress(_gmailAccount, "FitnessBar 客服團隊");
                            mailMessage.Subject = $"【FitnessBar】您的客服回報已受理 - 案件編號 #{record.CustomerServiceId}";
                            mailMessage.To.Add(record.Email);
                            mailMessage.IsBodyHtml = true;

                            mailMessage.Body = $@"
                            <div style='max-width: 600px; margin: auto; font-family: Microsoft JhengHei, sans-serif; border: 1px solid #eee; border-radius: 10px; overflow: hidden;'>
                                <div style='background-color: #f3722c; padding: 25px; text-align: center;'>
                                    <h2 style='color: white; margin: 0;'>我們已收到您的訊息</h2>
                                </div>
                                <div style='padding: 30px; color: #444;'>
                                    <p>親愛的 <strong>{record.Name}</strong> 您好：</p>
                                    <p>感謝您的回饋！我們已成功收到您的回報，客服團隊將盡快檢視您的內容，並於 1-2 個工作天內回覆至您的信箱。</p>
                                    
                                    <div style='background-color: #fafafa; border-radius: 8px; padding: 20px; margin: 25px 0; border: 1px solid #f0f0f0;'>
                                        <p style='margin: 0 0 10px 0;'><strong>案件編號：</strong> #{record.CustomerServiceId}</p>
                                        <p style='margin: 0 0 10px 0;'><strong>問題類別：</strong> {categoryName}</p>
                                        <p style='margin: 0 0 10px 0;'><strong>回報內容：</strong></p>
                                        <div style='color: #666; font-size: 14px; background: white; padding: 10px; border-radius: 4px;'>{record.Detail}</div>
                                    </div>

                                    <p style='font-size: 14px; color: #888;'>※ 此信件為系統自動發送，請勿直接回覆。若有其他緊急事宜，請致電官方專線。</p>
                                </div>
                                <div style='background-color: #333; padding: 15px; text-align: center; color: white; font-size: 12px;'>
                                    © 2026 FitnessBar 健身Bar - 讓運動成為一種生活
                                </div>
                            </div>";

                            await smtpClient.SendMailAsync(mailMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 記錄到 Debug 視窗，方便排查
                    System.Diagnostics.Debug.WriteLine($"[SMTP Error] {ex.Message}");
                }
            }
        }

        // --- DTO 類別 ---
        public class CustomerServiceUploadDto
        {
            public string Name { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public int QuestionCategoryId { get; set; }
            public string Detail { get; set; } = string.Empty;
            public string? CaptchaToken { get; set; }
            public IFormFile? ImageFile { get; set; }
        }

        private class ReCaptchaInternalResult
        {
            [JsonProperty("success")]
            public bool Success { get; set; }
            [JsonProperty("error-codes")]
            public List<string>? ErrorCodes { get; set; }
        }
    }
}