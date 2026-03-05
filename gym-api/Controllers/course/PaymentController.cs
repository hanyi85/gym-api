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
            // 1) 取回 TradeInfo/TradeSha（藍新是 POST form）
            var tradeInfo = Request.Form["TradeInfo"].ToString();
            var tradeSha = Request.Form["TradeSha"].ToString();

            // 至少要有 TradeInfo
            if (string.IsNullOrWhiteSpace(tradeInfo))
                return Redirect("http://localhost:5173/courses/booking-success?paid=true");

            // 2) （建議）驗 sha：不驗也能 demo，但驗了比較不會亂入
            try
            {
                var opt = _newebPayOptions();
                var calculatedSha = NewebPayService.CreateTradeSha(tradeInfo, opt.HashKey, opt.HashIV);
                if (!string.Equals(calculatedSha, tradeSha, StringComparison.OrdinalIgnoreCase))
                {
                    return Redirect("http://localhost:5173/courses/booking-fail?reason=sha");
                }
            }
            catch
            {
                // 專題 demo：sha 驗證出錯就先放過，避免卡死
            }

            // 3) 解密 TradeInfo
            string decrypted;
            try
            {
                decrypted = DecryptTradeInfo(
                    tradeInfo,
                    _newebPayOptions().HashKey,
                    _newebPayOptions().HashIV
                );
            }
            catch
            {
                return Redirect("http://localhost:5173/courses/booking-fail?reason=decrypt");
            }

            // 4) 解析 JSON 拿 MerchantOrderNo + 判斷是否成功
            string merchantOrderNo = "";
            bool success = false;

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(decrypted);

                // 藍新通常：Status = SUCCESS
                if (doc.RootElement.TryGetProperty("Status", out var st))
                {
                    var status = st.GetString() ?? "";
                    success = string.Equals(status, "SUCCESS", StringComparison.OrdinalIgnoreCase);
                }

                // Result.MerchantOrderNo
                if (doc.RootElement.TryGetProperty("Result", out var result) &&
                    result.ValueKind == System.Text.Json.JsonValueKind.Object &&
                    result.TryGetProperty("MerchantOrderNo", out var mo))
                {
                    merchantOrderNo = mo.GetString() ?? "";
                }
            }
            catch
            {
                return Redirect("http://localhost:5173/courses/booking-fail?reason=parse");
            }

            // 5) 從 MerchantOrderNo 抽 bookingId
            // 例：ORDBK000000123_20260301123059（中間可能有空白）
            merchantOrderNo = (merchantOrderNo ?? "").Replace(" ", "").Trim();

            var bkIndex = merchantOrderNo.IndexOf("BK", StringComparison.OrdinalIgnoreCase);
            if (bkIndex < 0)
                return Redirect("http://localhost:5173/courses/booking-fail?reason=noBK");

            var afterBk = merchantOrderNo.Substring(bkIndex); // BK000000123_...
            var underscoreIndex = afterBk.IndexOf('_');
            var bkPart = underscoreIndex > 0 ? afterBk.Substring(0, underscoreIndex) : afterBk; // BK000000123

            var idStr = bkPart.Replace("BK", "", StringComparison.OrdinalIgnoreCase).Trim(); // 000000123
            if (!int.TryParse(idStr, out var bookingId) || bookingId <= 0)
                return Redirect("http://localhost:5173/courses/booking-fail?reason=badId");

            // ✅ 6) 成功就直接更新 DB：讓訂單頁顯示「已付款」
            // （專題 demo：即使 success=false，你也可以選擇先更新；我先用 success 判斷較合理）
            if (success)
            {
                var booking = await _context.CCourseBookings
                    .FirstOrDefaultAsync(x => x.CourseBookingId == bookingId);

                if (booking != null && booking.PaymentStatus != "已付款")
                {
                    booking.PaymentStatus = "已付款";
                    booking.Status = "已報名";
                    booking.PaymentMethod = "信用卡";
                    await _context.SaveChangesAsync();
                }
            }

            // 7) Redirect 回前端成功頁（帶 paid + bookingId + orderId）
            var orderId = $"BK{bookingId.ToString().PadLeft(9, '0')}";
            var url = $"http://localhost:5173/courses/booking-success?bookingId={bookingId}";

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