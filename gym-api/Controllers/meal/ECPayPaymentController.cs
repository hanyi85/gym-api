using gym_api.DTOs;
using gym_api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gym_api.Controllers.meal
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("餐點綠界付費管理")]
    public class ECPayPaymentController : ControllerBase
    {
       
            private readonly dbFitness2Context _context;

            public ECPayPaymentController(dbFitness2Context context)
            {
                _context = context;
            }

            [HttpPost("Create")]
            public IActionResult CreatePayment([FromBody] MealCreatePaymentDto dto)
            {
            Console.WriteLine("新增訂單 進來了");
            var order = _context.TMealOrders
                    .FirstOrDefault(o => o.FOrderId == dto.OrderId);

                if (order == null)
                    return BadRequest("找不到訂單");

                order.FOrderStatus = "己付款，待取餐";
                _context.SaveChanges();

            string merchantId = "3002607";
                string hashKey = "pwFHCqoQZGmho4w6";
                string hashIV = "EkRm7iFT261dpevs";

            var tradeNo = $"T{DateTime.Now:yyMMddHHmmss}{dto.OrderId}";

            var parameters = new Dictionary<string, string>
                {
                    { "MerchantID", merchantId },
                    { "MerchantTradeNo", tradeNo },
                    { "MerchantTradeDate", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") },
                    { "PaymentType", "aio" },
                    { "TotalAmount", order.FTotalAmount.ToString() },
                    { "TradeDesc", "健身餐訂單" },
                    { "ItemName", "健身餐一批" },
                    { "ReturnURL", "https://indehiscent-bristol-enragedly.ngrok-free.dev/api/ECPayPayment/Callback" },
                    { "ClientBackURL", "http://localhost:5173/meals/result/" + dto.OrderId },
                    { "ChoosePayment", "ALL" },
                    { "EncryptType", "1" }
                };

                    string checkMacValue = GenerateCheckMacValue(parameters, hashKey, hashIV);
                    parameters.Add("CheckMacValue", checkMacValue);

                    string form = GenerateAutoSubmitForm(parameters);

                    return Content(form, "text/html");
                }

            private string GenerateCheckMacValue(Dictionary<string, string> parameters,string hashKey,string hashIV)
            {
                var sorted = parameters.OrderBy(p => p.Key)
                    .Select(p => $"{p.Key}={p.Value}");

                string raw = $"HashKey={hashKey}&" +
                             string.Join("&", sorted) +
                             $"&HashIV={hashIV}";

                raw = System.Web.HttpUtility.UrlEncode(raw).ToLower();

                using var sha256 = System.Security.Cryptography.SHA256.Create();
                byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(raw));

                return BitConverter.ToString(bytes)
                    .Replace("-", "")
                    .ToUpper();
            }

            private string GenerateAutoSubmitForm(Dictionary<string, string> parameters)
            {
                var sb = new StringBuilder();
                sb.Append("<form id='ecpayForm' method='post' action='https://payment-stage.ecpay.com.tw/Cashier/AioCheckOut/V5'>");

                foreach (var param in parameters)
                {
                    sb.Append($"<input type='hidden' name='{param.Key}' value='{param.Value}' />");
                }

                sb.Append("</form>");
                sb.Append("<script>document.getElementById('ecpayForm').submit();</script>");

                return sb.ToString();
            }

        private int ParseOrderId(string merchantTradeNo)
        {
            // 移除開頭 T
            var body = merchantTradeNo.Substring(1);

            // 前 12 位是時間
            var orderIdStr = body.Substring(12);

            return int.Parse(orderIdStr);
        }

        [HttpPost("Callback")]
        public IActionResult Callback()
        {
            Console.WriteLine(" Callback 進來了");

            var form = Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());

            foreach (var item in form)
            {
                Console.WriteLine($"{item.Key} = {item.Value}");
            }

            // 1️ 驗證 CheckMacValue
            if (!form.ContainsKey("CheckMacValue"))
            {
                Console.WriteLine(" 缺少 CheckMacValue (可能是 ClientBackURL)");
                return Content("1|OK");
            }

            bool isValid = ValidateCheckMacValue(form);
            Console.WriteLine("驗證結果: " + isValid);

            if (!isValid)
            {
                return Content("0|CheckMacValue 驗證失敗");
            }

            // 2️ 判斷是否付款成功
            if (form.ContainsKey("RtnCode") && form["RtnCode"] == "1")
            {
                string merchantTradeNo = form["MerchantTradeNo"];
                int orderId = ParseOrderId(merchantTradeNo);
                Console.WriteLine("解析出的 orderId = " + orderId);

                var order = _context.TMealOrders.FirstOrDefault(o => o.FOrderId == orderId);
                Console.WriteLine(order == null ? "找不到訂單" : " 找到訂單");

                if (order != null)
                {
                    order.FOrderStatus = "已付款，待取餐";
                    _context.SaveChanges();
                }
            }

            // 一定要回傳 1|OK
            return Content("1|OK");
        }

        private bool ValidateCheckMacValue(Dictionary<string, string> data)
        {
            string hashKey = "pwFHCqoQZGmho4w6";
            string hashIV = "EkRm7iFT261dpevs";

            string checkMacValue = data["CheckMacValue"];
            data.Remove("CheckMacValue");

            var sorted = data.OrderBy(x => x.Key)
                .Select(x => $"{x.Key}={x.Value}");

            string raw = $"HashKey={hashKey}&" +
                         string.Join("&", sorted) +
                         $"&HashIV={hashIV}";

            raw = System.Web.HttpUtility.UrlEncode(raw).ToLower();

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(raw));

            string newCheckMacValue = BitConverter.ToString(bytes)
                .Replace("-", "")
                .ToUpper();

            return newCheckMacValue == checkMacValue;
        }
    }
}
