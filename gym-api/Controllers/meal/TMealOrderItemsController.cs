using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gym_api.Models;

namespace gym_api.Controllers.meal
{
    [ApiController]
    [Route("api/[controller]")]
    [Tags("餐點訂單明細管理")]
    public class TMealOrderItemsController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public TMealOrderItemsController(dbFitness2Context context)
        {
            _context = context;
        }

        // GET: api/TMealOrderItems
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TMealOrderItem>>> GetOrderItems()
        {
            return await _context.TMealOrderItems
                .Include(i => i.FMeal)
                .Include(i => i.FOrder)
                .Include(i => i.FPickTime)
                .ToListAsync();
        }

        // GET: api/TMealOrderItems/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TMealOrderItem>> GetOrderItem(int id)
        {
            var item = await _context.TMealOrderItems
                .Include(i => i.FMeal)
                .Include(i => i.FOrder)
                .Include(i => i.FPickTime)
                .FirstOrDefaultAsync(i => i.FOrderItemId == id);

            if (item == null)
                return NotFound();

            return item;
        }

        // 依會員ID取未取餐QRCODE
        // GET: api/TMealOrderItems/Qrcode/{userId}
        [HttpGet("Qrcode/{userId}")]
        public async Task<IActionResult> GetUserQrCodes(int userId)
        {
            var items = await _context.TMealOrderItems
                .Where(i => i.FOrder.FUserId == userId && !i.FPickupStatus)
                .Select(i => new
                {
                    i.FOrderItemId,
                    i.FQrContent,
                    i.FMeal.FMealName,
                    i.FQty,
                    i.FUnitPrice,
                    i.FSubtotal,
                    i.FPickDate,
                    PickStart = i.FPickTime.FStartTime,
                    PickEnd = i.FPickTime.FEndTime,
                    VenueName = i.FOrder.FVenue.VenueName,
                    i.FPickupStatus
                })
                .ToListAsync();

            return Ok(items);
        }

        // POST: api/TMealOrderItems
        [HttpPost]
        public async Task<ActionResult<TMealOrderItem>> CreateOrderItem(TMealOrderItem item)
        {
            // 自動計算小計
            item.FSubtotal = item.FQty * item.FUnitPrice;

            _context.TMealOrderItems.Add(item);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetOrderItem),
                new { id = item.FOrderItemId },
                item);
        }

        // PUT: api/TMealOrderItems/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrderItem(int id, TMealOrderItem item)
        {
            if (id != item.FOrderItemId)
                return BadRequest();

            // 重新計算小計
            item.FSubtotal = item.FQty * item.FUnitPrice;

            _context.Entry(item).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.TMealOrderItems.Any(e => e.FOrderItemId == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // DELETE: api/TMealOrderItems/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrderItem(int id)
        {
            var item = await _context.TMealOrderItems.FindAsync(id);

            if (item == null)
                return NotFound();

            _context.TMealOrderItems.Remove(item);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        //  依會員查詢明細（Vue 必用）
        // GET: api/TMealOrderItems/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<TMealOrderItem>>> GetOrderItemsByUser(int userId)
        {
            var items = await _context.TMealOrderItems
                .Include(i => i.FMeal)        // 關聯餐點
                .Include(i => i.FPickTime)    // 關聯取餐時段
                .Include(i => i.FOrder)       // 關聯訂單 (用來過濾會員)
                .Where(i => i.FOrder.FUserId == userId)   // 依會員過濾
                .OrderByDescending(i => i.FOrder.FOrderAt)
                .ToListAsync();

            if (items == null || !items.Any())
            {
                return NotFound();
            }

            return Ok(items);
        }
    }
}

