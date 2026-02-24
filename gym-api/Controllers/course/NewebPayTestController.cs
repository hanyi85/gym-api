using gym_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace gym_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NewebPayTestController : ControllerBase
    {
        private readonly NewebPayService _newebPay;

        public NewebPayTestController(NewebPayService newebPay)
        {
            _newebPay = newebPay;
        }

        [HttpGet("payload")]
        public IActionResult GetPayload()
        {
            // 這裡先用假訂單資料測試
            var payload = _newebPay.CreateMpgPayload(
                merchantOrderNo: "TEST" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                amt: 100,
                itemDesc: "專題測試訂單",
                returnUrl: "http://localhost:5173/payment/success",
                notifyUrl: "https://example.com/api/newebpay/notify" // 先隨便，測 service 用
            );

            return Ok(payload);
        }
    }
}