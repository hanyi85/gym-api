using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace gym_api.Controllers
{
    // 定義前端傳來的資料結構
    public class LinePayRequestDto
    {
        public int Fee { get; set; }
        public string EventName { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    [Tags("LINEPAY")]
    public class YLINEPAYController : ControllerBase
    {
        private readonly string _channelId = "2009062308";
        private readonly string _channelSecret = "85e328e8b1b73e71004799a938720ea0";
        private readonly string _linePayBaseUrl = "https://sandbox-api-pay.line.me";

        [HttpPost("RegisterWithLinePay")]
        public async Task<IActionResult> RegisterWithLinePay([FromBody] LinePayRequestDto dto)
        {
            try
            {
                if (dto == null || dto.Fee <= 0)
                    return BadRequest(new { message = "金額無效" });

                string orderId = "JOIN_" + DateTime.Now.Ticks.ToString();

                var linePayBody = new
                {
                    amount = dto.Fee,
                    currency = "TWD",
                    orderId = orderId,
                    packages = new[]
                    {
                        new {
                            id = "pkg_01",
                            amount = dto.Fee,
                            name = dto.EventName ?? "活動報名費",
                            products = new[]
                            {
                                new { name = dto.EventName ?? "活動報名費", quantity = 1, price = dto.Fee }
                            }
                        }
                    },
                    redirectUrls = new
                    {
                        confirmUrl = "http://localhost:5173/payment/confirm",
                        cancelUrl = "http://localhost:5173/payment/cancel"
                    }
                };

                // 使用 System.Text.Json 序列化
                string jsonBody = JsonSerializer.Serialize(linePayBody);
                string uri = "/v3/payments/request";
                string nonce = Guid.NewGuid().ToString();

                string signature = GenerateSignature(_channelSecret, uri, jsonBody, nonce);

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-LINE-ChannelId", _channelId);
                    client.DefaultRequestHeaders.Add("X-LINE-Authorization-Nonce", nonce);
                    client.DefaultRequestHeaders.Add("X-LINE-Authorization", signature);

                    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(_linePayBaseUrl + uri, content);
                    var result = await response.Content.ReadAsStringAsync();

                    // 直接回傳 LINE Pay 的 JSON 給前端
                    return Content(result, "application/json");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        private string GenerateSignature(string secret, string uri, string body, string nonce)
        {
            var signatureRaw = secret + uri + body + nonce;
            var key = Encoding.UTF8.GetBytes(secret);
            var data = Encoding.UTF8.GetBytes(signatureRaw);

            using (var hmac = new HMACSHA256(key))
            {
                var hash = hmac.ComputeHash(data);
                return Convert.ToBase64String(hash);
            }
        }
    }
}