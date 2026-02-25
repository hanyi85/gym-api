using gym_api.Models;
using gym_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace gym_api.Controllers.course
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("課程金流管理")]
    public class PaymentController : ControllerBase
    {
        private readonly NewebPayService _newebPay;
        private readonly dbFitness2Context _context;
        public PaymentController(NewebPayService newebPay, dbFitness2Context context)
        {
            _newebPay = newebPay;
            _context = context;
        }

        // ===============================
        // 1️⃣ 建立藍新付款資料
        // ===============================
        public class CreatePaymentRequest
        {
            public string OrderId { get; set; }
            public int Amount { get; set; }
            public string ItemDesc { get; set; } = "課程訂單";
        }

        [HttpPost("newebpay/create")]
        public IActionResult Create([FromBody] CreatePaymentRequest req)
        {
            // ⚠️ 這裡之後可以改成用 OrderId 查資料庫
            var merchantOrderNo = $"ORD{req.OrderId}_{DateTime.Now:yyyyMMddHHmmss}";

            var returnUrl = "https://localhost:7218/api/Payment/newebpay/return";
            var notifyUrl = "https://你的ngrok網址/api/payment/newebpay/notify";

            var payload = _newebPay.CreateMpgPayload(
                merchantOrderNo: merchantOrderNo,
                amt: req.Amount,
                itemDesc: req.ItemDesc,
                returnUrl: returnUrl,
                notifyUrl: notifyUrl
            );

            return Ok(payload);
        }

        // ===============================
        // 2️⃣ 接收藍新付款結果（伺服器通知）
        // ===============================
        [HttpPost("newebpay/notify")]
        public IActionResult Notify()
        {
            try
            {
                var tradeInfo = Request.Form["TradeInfo"].ToString();
                var tradeSha = Request.Form["TradeSha"].ToString();

                if (string.IsNullOrEmpty(tradeInfo) || string.IsNullOrEmpty(tradeSha))
                    return BadRequest("Missing TradeInfo or TradeSha");

                // 驗證 SHA
                var calculatedSha = NewebPayService.CreateTradeSha(
                    tradeInfo,
                    _newebPayOptions().HashKey,
                    _newebPayOptions().HashIV
                );

                if (!string.Equals(calculatedSha, tradeSha, StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest("TradeSha 驗證失敗");
                }

                // 解密 TradeInfo
                var decrypted = DecryptTradeInfo(
                    tradeInfo,
                    _newebPayOptions().HashKey,
                    _newebPayOptions().HashIV
                );

                // TODO: 這裡可以解析 JSON，更新訂單狀態
                Console.WriteLine("藍新回傳資料：");
                Console.WriteLine(decrypted);

                return Ok("1|OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Notify Error: " + ex.Message);
                return BadRequest();
            }
        }

        // ===============================
        // 取得 Options（因為 Service 沒公開 HashKey）
        // ===============================
        private NewebPayOptions _newebPayOptions()
        {
            var field = typeof(NewebPayService)
                .GetField("_opt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            return (NewebPayOptions)field!.GetValue(_newebPay)!;
        }

        // ===============================
        // 解密 TradeInfo
        // ===============================
        private string DecryptTradeInfo(string tradeInfoHex, string hashKey, string hashIv)
        {
            var encryptedBytes = Convert.FromHexString(tradeInfoHex);

            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            aes.Key = Encoding.UTF8.GetBytes(hashKey.Trim());
            aes.IV = Encoding.UTF8.GetBytes(hashIv.Trim());

            using var decryptor = aes.CreateDecryptor();
            var decrypted = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

            var result = Encoding.UTF8.GetString(RemovePadding(decrypted));

            return result;
        }

        private byte[] RemovePadding(byte[] data)
        {
            int pad = data[^1];
            return data.Take(data.Length - pad).ToArray();
        }

        [HttpPost("newebpay/return")]
        public IActionResult Return()
        {
            // 藍新會用 Form POST 回來（TradeInfo/TradeSha 在 Request.Form）
            // 專題先不驗也可以，先導回成功頁，確保流程跑通

            return Redirect("http://localhost:5173/courses/booking-success?paid=true");
        }

        [AllowAnonymous]
        [HttpGet("newebpay/status")]
        public async Task<IActionResult> QueryStatus(int courseBookingId)
        {
            var booking = await _context.CCourseBookings
                .FirstOrDefaultAsync(x => x.CourseBookingId == courseBookingId);

            if (booking == null)
                return NotFound("找不到訂單");

            if (booking.PaymentStatus == "已付款")
                return Ok("已付款");

            // 這裡應該改成真的去查藍新
            // 目前先做 demo 測試成功版
            // 你等下再改成真的 QueryTradeInfo

            booking.PaymentStatus = "已付款";
            booking.Status = "已報名";
            booking.PaymentMethod = "信用卡";

            await _context.SaveChangesAsync();

            return Ok("付款成功，已更新資料庫");
        }
        [HttpGet("booking-id-by-schedule")]
        public async Task<IActionResult> GetBookingIdBySchedule(int scheduleId, int userId = 1)
        {
            var id = await _context.CCourseBookings
                .Where(x => x.ScheduleId == scheduleId && x.UserId == userId)
                .OrderByDescending(x => x.CourseBookingId)
                .Select(x => x.CourseBookingId)
                .FirstOrDefaultAsync();

            if (id == 0) return NotFound("找不到該 schedule 的訂單");

            return Ok(new { courseBookingId = id });
        }
    }
}