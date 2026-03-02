using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gym_api.Models;
using gym_api.DTOs;

namespace gym_api.Controllers.meal
{
    [ApiController]
    [Route("api/[controller]")]
    [Tags("餐點訂單管理")]
    public class TMealOrdersController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public TMealOrdersController(dbFitness2Context context)
        {
            _context = context;
        }

        // GET: api/TMealOrders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TMealOrder>>> GetOrders()
        {
            return await _context.TMealOrders
                .Include(o => o.FUser)
                .Include(o => o.FVenue)
                .ToListAsync();
        }

        // GET: api/TMealOrders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TMealOrder>> GetOrder(int id)
        {
            var order = await _context.TMealOrders
                .Include(o => o.FUser)
                .Include(o => o.FVenue)
                .FirstOrDefaultAsync(o => o.FOrderId == id);

            if (order == null)
                return NotFound();

            return order;
        }
        //取得使用顧客所以餐點
        // GET: api/TMealOrders/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult> GetOrdersByUser(int userId)
        {
            var orders = await _context.TMealOrders
             .Where(o => o.FUserId == userId)
             .OrderByDescending(o => o.FOrderAt)
             .Select(o => new
             {
                 o.FOrderId,
                 o.FOrderName,
                 o.FOrderPhone,
                 o.FOrderEmail,
                 o.FOrderAt,
                 o.FTotalAmount,
                 o.FOrderStatus,
                 o.FPayMethod,
                 o.FVenue,


                 Items = o.TMealOrderItems.Select(i => new
                 {
                     i.FOrderItemId,            
                     i.FMeal,
                     i.FQty,
                     i.FUnitPrice,
                     i.FSubtotal,
                     i.FPickDate,
                     i.FPickTime,
                     i.FQrContent,
                     i.FPickupStatus
                 })
             })
             .ToListAsync();
            return Ok(orders);
        }

        // POST: api/TMealOrders
        [HttpPost]
        public async Task<ActionResult<TMealOrder>> CreateOrder(TMealOrder order)
        {
            order.FCartCreateAt = DateTime.Now;
            order.FOrderAt = DateTime.Now;

            _context.TMealOrders.Add(order);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetOrder),
                new { id = order.FOrderId },
                order);
        }

        // PUT: api/TMealOrders/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, TMealOrder order)
        {
            if (id != order.FOrderId)
                return BadRequest();

            _context.Entry(order).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.TMealOrders.Any(e => e.FOrderId == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // DELETE: api/TMealOrders/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.TMealOrders.FindAsync(id);

            if (order == null)
                return NotFound();

            _context.TMealOrders.Remove(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        //取得取餐分店
        // GET: api/TMealOrders/MealVenues
        [HttpGet("MealVenues")]
        public async Task<ActionResult<IEnumerable<MealVenueDto>>> GetVrnues()
        {
            return await _context.CVenues
         .Select(v => new MealVenueDto
         {
             VenueId = v.VenueId,
             VenueName = v.VenueName
         })
         .ToListAsync();

        }

        //訂餐結果回傳
        //Get:api/TMealOrders/result/{orderId}
        [HttpGet("result/{orderId}")]
        public async Task<IActionResult> GetResult(int orderId)
        {
            var order = await _context.TMealOrders
                .Include(o => o.FVenue)
                .Include(o => o.TMealOrderItems)
                .ThenInclude(i => i.FMeal)
                .Include(o => o.TMealOrderItems)
                .ThenInclude(i => i.FPickTime)

                .FirstOrDefaultAsync(o => o.FOrderId == orderId);

            if (order == null)
                return NotFound();

            return Ok(new
            {
                order,
                items = order.TMealOrderItems
            });
        }
    }
}
