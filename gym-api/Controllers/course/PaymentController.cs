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
        public async Task<IActionResult> Return()
        {
            // 1) 取回 TradeInfo/TradeSha
            var tradeInfo = Request.Form["TradeInfo"].ToString();
            var tradeSha = Request.Form["TradeSha"].ToString();

            // 專題先不驗也行，但至少要有 TradeInfo
            if (string.IsNullOrEmpty(tradeInfo))
                return Redirect("http://localhost:5173/courses/booking-success?paid=true");

            // 2) 解密 TradeInfo（你已經有方法）
            var decrypted = DecryptTradeInfo(
                tradeInfo,
                _newebPayOptions().HashKey,
                _newebPayOptions().HashIV
            );

            // 3) 解析 JSON 拿 MerchantOrderNo
            // NewebPay 解密後是 JSON 字串，MerchantOrderNo 通常在 Result.MerchantOrderNo
            string merchantOrderNo = "";
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(decrypted);
                merchantOrderNo = doc.RootElement
                    .GetProperty("Result")
                    .GetProperty("MerchantOrderNo")
                    .GetString() ?? "";
            }
            catch
            {
                // 解析失敗就先回首頁
                return Redirect("http://localhost:5173/courses");
            }

            // merchantOrderNo 長這樣： ORD BK000000123_20260301123059
            // 或 ORD{req.OrderId}_{timestamp}
            // 我們要把 BK 後面的數字抓出來
            var bkIndex = merchantOrderNo.IndexOf("BK", StringComparison.OrdinalIgnoreCase);
            if (bkIndex < 0)
                return Redirect("http://localhost:5173/courses");

            // 取 BK... 到底線前
            var afterBk = merchantOrderNo.Substring(bkIndex); // BK000000123_...
            var underscoreIndex = afterBk.IndexOf('_');
            var bkPart = underscoreIndex > 0 ? afterBk.Substring(0, underscoreIndex) : afterBk;

            // bkPart = BK000000123
            var idStr = bkPart.Replace("BK", "");
            if (!int.TryParse(idStr, out int bookingId))
                return Redirect("http://localhost:5173/courses");

      

            // 5) Redirect 回前端（A 版）
            // 這邊我建議帶 bookingId，成功頁會更穩（你 QR 也需要）
            var url = $"http://localhost:5173/courses/{Uri.EscapeDataString(slug)}/booking/success?paid=true&bookingId={bookingId}";
            return Redirect(url);
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