using gym_api.DTO;
using gym_api.Models;
using gym_api.Services;
using Microsoft.AspNetCore.Http; // 用於生產狀態碼標籤
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace gym_api.Controllers.product
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("訂單管理")]

    public class SOrderController : ControllerBase
    {
        private readonly dbFitness2Context _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly EmailService _emailService;


        public SOrderController(dbFitness2Context context, IHttpClientFactory httpClientFactory, EmailService emailService)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _emailService = emailService;
        }

        // GET: api/SOrders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SOrder>>> GetSOrders()
        {
            return await _context.SOrders
                .Include(s => s.Dis)
                .Include(s => s.Pay)
                .Include(s => s.Ship)
                .Include(s => s.User)
                .ToListAsync();
        }

        // GET: api/SOrders/5
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SOrder>> GetSOrder(int id)
        {
            var sOrder = await _context.SOrders
                .Include(s => s.Dis)
                .Include(s => s.Pay)
                .Include(s => s.Ship)
                .Include(s => s.User)
                .FirstOrDefaultAsync(m => m.OId == id);

            if (sOrder == null)
            {
                return NotFound();
            }

            return sOrder;
        }

        // POST: api/SOrders
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] SOrderDTO dto)
        {
            if (dto == null || dto.items == null || !dto.items.Any())
                return BadRequest("訂單資料或商品明細不可為空");

            string randomOrderNumber = "ORD" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. 建立訂單主檔 (SOrder)
                    var order = new SOrder
                    {
                        UserId = 1,
                        MName = dto.mName,
                        MPhone = dto.mPhone,
                        Email = dto.email,
                        MAddress = dto.mAddress.Length > 30 ? dto.mAddress.Substring(0, 30) : dto.mAddress,
                        Date = DateTime.Now,
                        OStatus = "處理中",
                        ShipFee = dto.shipFee,
                        Total = dto.total,
                        Note = dto.note ?? "",
                        PayId = dto.payId,
                        ShipId = dto.shipId,
                        Pay = null,
                        Ship = null,
                        PayStatus = "待付款",
                        OrderNumber = randomOrderNumber
                    };

                    _context.Entry(order).Reference(o => o.Pay).IsModified = false;
                    _context.Entry(order).Reference(o => o.Ship).IsModified = false;

                    _context.SOrders.Add(order);
                    await _context.SaveChangesAsync(); // 先儲存以取得自動編號的 OId

                    // 2. 建立訂單明細 (SOrderDetail)
                    foreach (var item in dto.items)
                    {
                        var specExists = await _context.SSpecifications.AnyAsync(s => s.SpecId == item.specId);
                        if (!specExists) throw new Exception($"找不到 ID 為 {item.specId} 的商品規格");

                        var detail = new SOrderDetail
                        {
                            OId = order.OId,
                            PName = item.pName,
                            SpecId = item.specId,
                            SPrice = item.price,
                            Quantity = item.quantity,
                            Subtotal = item.price * item.quantity
                        };
                        _context.SOrderDetails.Add(detail);
                    }

                    // 在呼叫 PayPal 前，確保所有明細已寫入資料庫
                    await _context.SaveChangesAsync();

                    // ==========================================
                    // ⚡ 這裡就是加入 PayPal 邏輯的地方 ⚡
                    // ==========================================

                    if (dto.payId == 3)
                    {
                        // 呼叫向 PayPal 請求訂單 ID 的方法 (稍後定義在下方)
                        string paypalOrderId = await CreatePayPalOrder(order.Total, order.OrderNumber);

                        // 如果成功取得 PayPal ID，就提交事務並回傳給前端
                        await transaction.CommitAsync();

                        return Ok(new
                        {
                            success = true,
                            orderNo = randomOrderNumber,
                            approvalUrl = paypalOrderId // 傳給 Vue 觸發付款視窗
                        });
                    }

                    // ==========================================

                    // 如果不是 PayPal (例如貨到付款)，直接提交
                    await transaction.CommitAsync();
                    try
                    {
                        await _emailService.SendOrderConfirmationEmail(order.Email, order.OrderNumber, order.Total, "貨到付款");
                    }
                    catch (Exception ex)
                    {
                        // 僅記錄 Log，不影響回傳給前端的成功結果
                        Console.WriteLine($"寄信失敗: {ex.Message}");
                    }
                    return Ok(new { success = true, orderNo = randomOrderNumber });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, "交易失敗：" + ex.Message);
                }
            }
        }

        private async Task<string> CreatePayPalOrder(decimal total, string orderNumber)
        {
            var client = _httpClientFactory.CreateClient();
            string clientId = "AVYXZxqBbHTn6VzJhCNHheqsQ_k8Aux3-jS1a9tVE2Ibp_BgS2smxr-Q58YqkJ8O0BW9HQprCqhyxoDH";
            string secret = "EG8a66plWflCBImJ5LdiX1YeoBt2ioHwsPzq1eWILbiE_WpC0_gie9F0giesPXtmEbrMjkXfrKVgrnxs";
            var auth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{clientId}:{secret}"));

            var requestBody = new
            {
                intent = "CAPTURE",
                purchase_units = new[] {
            new {
                reference_id = orderNumber,
                amount = new { currency_code = "TWD", value = Math.Round(total).ToString() }
            }
        },
                // ⚡ 這裡非常重要：設定支付完要跳回哪裡
                application_context = new
                {
                    return_url = "http://localhost:5173/shop/payment-callback", // 支付成功跳轉頁
                    cancel_url = "http://localhost:5173/shop/Shop-Booking"         // 取消支付跳回頁
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api-m.sandbox.paypal.com/v2/checkout/orders");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
            request.Content = JsonContent.Create(requestBody);

            var response = await client.SendAsync(request);
            var jsonDoc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var root = jsonDoc.RootElement;

            // ⚡ 遍歷 JSON 尋找跳轉網址
            string approvalUrl = "";
            if (root.TryGetProperty("links", out var links))
            {
                foreach (var link in links.EnumerateArray())
                {
                    if (link.GetProperty("rel").GetString() == "approve")
                    {
                        approvalUrl = link.GetProperty("href").GetString();
                        break;
                    }
                }
            }
            if (string.IsNullOrEmpty(approvalUrl))
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"PayPal 沒回傳跳轉網址。錯誤原因：{errorBody}");
            }
            return approvalUrl; // 改為回傳完整網址
        }

        [HttpPost("{payPalOrderId}")]
        public async Task<IActionResult> CaptureOrder(string payPalOrderId)
        {
            var client = _httpClientFactory.CreateClient();


            // 1. 同樣需要驗證 (建議將這段抽出成私有方法)
            string clientId = "AVYXZxqBbHTn6VzJhCNHheqsQ_k8Aux3-jS1a9tVE2Ibp_BgS2smxr-Q58YqkJ8O0BW9HQprCqhyxoDH";
            string secret = "EG8a66plWflCBImJ5LdiX1YeoBt2ioHwsPzq1eWILbiE_WpC0_gie9F0giesPXtmEbrMjkXfrKVgrnxs";
            var auth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{clientId}:{secret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://api-m.sandbox.paypal.com/v2/checkout/orders/{payPalOrderId}/capture");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
            request.Content = new StringContent("", System.Text.Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                // 【方法 A：解析 PayPal 回傳的 JSON】
                var jsonResponse = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();


                // 從 purchase_units[0].reference_id 抓取你當初傳給 PayPal 的訂單編號
                string orderNo = jsonResponse.GetProperty("purchase_units")[0]
                                     .GetProperty("reference_id")
                                     .GetString();

                // 修正 3：使用 FirstOrDefaultAsync 尋找訂單 (請確認有 using Microsoft.EntityFrameworkCore;)
                var order = await _context.SOrders.FirstOrDefaultAsync(o => o.OrderNumber == orderNo);

                if (order != null)
                {
                    order.PayStatus = "已付款";
                    await _context.SaveChangesAsync();
                    try
                    {
                        await _emailService.SendOrderConfirmationEmail(order.Email, order.OrderNumber, order.Total, "PayPal 已付款");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"寄信失敗: {ex.Message}");
                    }

                    // 回傳成功狀態與訂單編號給前端，讓前端可以跳轉並顯示單號
                    return Ok(new { status = "COMPLETED", orderNo = orderNo });
                }

                return NotFound("找不到對應的資料庫訂單");
            }

            // 若 PayPal 回傳失敗，抓取錯誤訊息
            var errorMsg = await response.Content.ReadAsStringAsync();

            return BadRequest("付款請款失敗");
        }



        // PUT: api/SOrders/5
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PutSOrder(int id, SOrder sOrder)
        {
            if (id != sOrder.OId)
            {
                return BadRequest();
            }

            _context.Entry(sOrder).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SOrderExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/SOrders/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteSOrder(int id)
        {
            var sOrder = await _context.SOrders.FindAsync(id);
            if (sOrder == null)
            {
                return NotFound();
            }

            _context.SOrders.Remove(sOrder);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SOrderExists(int id)
        {
            return _context.SOrders.Any(e => e.OId == id);
        }

        // GET: api/SOrders/byOrderNumber/ORDXXXX
        [HttpGet("byOrderNumber/{orderNumber}")]
        public async Task<IActionResult> GetOrderByOrderNumber(string orderNumber)
        {
            var apiUrl = "http://localhost:7218"; // 後端本身的網址 + 端口

            var order = await _context.SOrders
                .Include(o => o.SOrderDetails)
                    .ThenInclude(d => d.Spec)
                        .ThenInclude(s => s.SImages)  // 規格圖
                .Include(o => o.SOrderDetails)
                    .ThenInclude(d => d.Spec)
                        .ThenInclude(s => s.PIdNavigation)  // 主商品
                            .ThenInclude(p => p.SImages)   // 主商品圖
                .Include(o => o.Pay)
                .Include(o => o.Ship)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

            if (order == null)
                return NotFound(new { message = $"找不到訂單號碼 {orderNumber}" });

            var result = new
            {
                order.OId,
                order.OrderNumber,
                order.Date,
                oStatus = order.OStatus,
                order.Total,
                order.MName,
                order.MPhone,
                order.Email,
                order.MAddress,
                order.ShipFee,
                order.PayStatus,
                ship = order.Ship != null ? new { order.Ship.ShipId, order.Ship.Shipping } : null,
                pay = order.Pay != null ? new { order.Pay.PayId, order.Pay.Payment } : null,
                sOrderDetails = order.SOrderDetails.Select(d => new
                {
                    d.OdId,
                    d.PName,
                    d.SpecId,
                    d.SPrice,
                    d.Quantity,
                    d.Subtotal,
                    spec = d.Spec != null ? new
                    {
                        // ⚡ 圖片邏輯：先規格圖 > 主圖 > 預設圖
                        ImagePath = apiUrl + "/" + (
                            d.Spec.SImages
                                .OrderByDescending(img => img.SpecId == d.SpecId)           // 優先規格圖
                                .ThenByDescending(img => img.MainPicture && img.SpecId == null) // 主圖
                                .ThenByDescending(img => img.PicId)
                                .Select(img => img.Picture)
                                .FirstOrDefault()
                            ?? d.Spec.PIdNavigation.SImages
                                .OrderByDescending(img => img.MainPicture)           // fallback 主商品圖
                                .ThenByDescending(img => img.PicId)
                                .Select(img => img.Picture)
                                .FirstOrDefault()
                            ?? "images/products/default.jpg"                         // 預設圖
                        ).TrimStart('/')
                    } : null
                })
            };

            return Ok(result);
        }

        // GET: api/SOrders/byMember/47
        [HttpGet("byMember/{userId}")]
        public async Task<IActionResult> GetOrdersByMember(int userId)
        {
            // 查詢該會員的所有訂單，並依照日期倒序排列（最新的在前面）
            var orders = await _context.SOrders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.Date)
                .Select(o => new
                {
                    o.OId,
                    o.OrderNumber,
                    o.Date,
                    oStatus = o.OStatus,
                    o.Total,
                    o.PayStatus
                })
                .ToListAsync();

            if (orders == null || !orders.Any())
            {
                // 回傳空陣列而非 404，對前端處理「尚無訂單」比較方便
                return Ok(new List<object>());
            }

            return Ok(orders);
        }
    }
}