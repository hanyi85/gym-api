using gym_api.DTO;
using gym_api.Models;
using Microsoft.AspNetCore.Http; // 用於生產狀態碼標籤
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gym_api.Controllers.product
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("訂單管理")]
    public class SOrderController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public SOrderController(dbFitness2Context context)
        {
            _context = context;
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

                        // ⚡ 關鍵：只設 FK，不設 navigation property
                        PayId = dto.payId,
                        ShipId = dto.shipId,
                        Pay = null,
                        Ship = null,

                        PayStatus = "待付款",
                        OrderNumber = randomOrderNumber
                    };

                    // 🔹 明確告訴 EF 這兩個 navigation 不要追蹤
                    _context.Entry(order).Reference(o => o.Pay).IsModified = false;
                    _context.Entry(order).Reference(o => o.Ship).IsModified = false;

                    _context.SOrders.Add(order);
                    await _context.SaveChangesAsync();

                    foreach (var item in dto.items)
                    {
                        var specExists = await _context.SSpecifications.AnyAsync(s => s.SpecId == item.specId);
                        if (!specExists)
                        {
                            throw new Exception($"找不到 ID 為 {item.specId} 的商品規格，請重新整理購物車。");
                        }
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

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new { success = true, orderNo = randomOrderNumber });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    return StatusCode(500, "資料庫寫入失敗：" + innerMessage);
                }
            }
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