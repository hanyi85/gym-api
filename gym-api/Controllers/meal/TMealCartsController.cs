using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using gym_api.Models;
using gym_api.DTOs;

namespace gym_api.Controllers.meal
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("餐點我的購物車管理")]
    public class TMealCartsController : ControllerBase
    {
        private readonly dbFitness2Context _context;
        public TMealCartsController(dbFitness2Context context)
        {
            _context = context;
        }

        // POST: api/TMealCarts/MealAddToCart
        [HttpPost("MealAddToCart")]
        public async Task<IActionResult> AddToCart(MealAddToCartDto dto)
        {
            //var userId = GetUserId(); // 從登入 token 取

            var order = await _context.TMealOrders
                .FirstOrDefaultAsync(o => o.FUserId == dto.FUserId && o.FOrderStatus == "Cart");

            if (order == null)
            {
                order = new TMealOrder
                {
                    FUserId = dto.FUserId,
                    FCartCreateAt = DateTime.Now,
                    FOrderStatus = "Cart"
                };

                _context.TMealOrders.Add(order);
                await _context.SaveChangesAsync();
            }

            var meal = await _context.TMeals.FindAsync(dto.FMealId);

            var item = new TMealOrderItem
            {
                FOrderId = order.FOrderId,
                FMealId = dto.FMealId,
                FQty = dto.FQty,
                FUnitPrice = meal.FPrice,
                FSubtotal = meal.FPrice * dto.FQty,
                FPickDate = dto.FPickDate,
                FPickTimeId = dto.FPickTimeId,
                FPickupStatus = false
            };

            _context.TMealOrderItems.Add(item);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: api/TMealCarts/Cart/{userId}
        [HttpGet("Cart/{userId}")]
        public async Task<IActionResult> GetCart(int userId)
        {
            //var userId = GetUserId();

            var order = await _context.TMealOrders
                .Include(o => o.TMealOrderItems)
                .ThenInclude(i => i.FMeal)
                .FirstOrDefaultAsync(o => o.FUserId == userId && o.FOrderStatus == "Cart");

            if (order == null)
                return Ok(new { orderId = 0, items = new List<object>() });

            var result = new
            {
                orderId = order.FOrderId,
                items = order.TMealOrderItems.Select(i => new
                {
                    orderItemId = i.FOrderItemId,
                    mealId = i.FMealId,
                    mealName = i.FMeal.FMealName,
                    imageUrl=i.FMeal.FImageUrl,
                    pickDate = i.FPickDate,
                    pickTimeId = i.FPickTimeId,
                    qty = i.FQty,
                    unitPrice = i.FUnitPrice,
                    subtotal = i.FSubtotal
                })
            };

            return Ok(result);
        }

        //Delete:api/TMealCarts/item/{orderitemid}
        [HttpDelete("Item/{orderitemid}")]
        public async Task<IActionResult> DeleteItem(int orderitemid)
        {
            var item = await _context.TMealOrderItems.FindAsync(orderitemid);

            if (item == null)
                return NotFound();

            _context.TMealOrderItems.Remove(item);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: api/TMealCarts/UpdateCart
        [HttpPut("UpdateCart")]
        public async Task<IActionResult> UpdateCart(MealUpdateCartDto dto)
        {
            var order = await _context.TMealOrders
                .Include(o => o.TMealOrderItems)
                .FirstOrDefaultAsync(o => o.FOrderId == dto.OrderId);

            if (order == null)
                return NotFound();

            foreach (var itemDto in dto.Items)
            {
                var item = order.TMealOrderItems
                    .FirstOrDefault(i => i.FOrderItemId == itemDto.OrderItemId);

                if (item == null)
                    continue;

                // 更新使用者修改的資料
                item.FPickDate = itemDto.PickDate;
                item.FPickTimeId = itemDto.PickTimeId;
                item.FQty = itemDto.Qty;

                //  後端重新抓價格
                var meal = await _context.TMeals
                    .FirstOrDefaultAsync(m => m.FMealId == item.FMealId);

                item.FUnitPrice = meal.FPrice;
                item.FSubtotal = meal.FPrice * item.FQty;
            }

            //  更新總金額
            order.FTotalAmount = order.TMealOrderItems.Sum(i => i.FSubtotal);

            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: api/TMealCarts/Checkout/{userId}
        [HttpPost("Checkout/{userId}")]
        public async Task<IActionResult> Checkout(MealCheckoutDto dto,int userId)
        {
            //var userId = GetUserId();

            var order = await _context.TMealOrders
                .Include(o => o.TMealOrderItems)
                .FirstOrDefaultAsync(o => o.FUserId == userId && o.FOrderStatus == "Cart");

            if (order == null)
                return BadRequest("沒有購物車");

            order.FOrderName = dto.Name;
            order.FOrderPhone = dto.Phone;
            order.FOrderEmail = dto.Email;
            order.FVenueId = dto.VenueId;
            order.FPayMethod = dto.PayMethod;
            order.FOrderAt = DateTime.Now;
            order.FTotalAmount = order.TMealOrderItems.Sum(i => i.FSubtotal);
            //  在這裡產生 QRCode
            foreach (var item in order.TMealOrderItems)
            {
                item.FQrContent = Guid.NewGuid().ToString("N");
            }

            await _context.SaveChangesAsync();

            return Ok(order.FOrderId);
        }

        // POST: api/TMealCarts/OrderFinish/{userId}
        [HttpPost("OrderFinish/{userId}")]
        public async Task<IActionResult> OrderFinish(int userId)
        {
            //var userId = GetUserId();

            var order = await _context.TMealOrders
                .Include(o => o.TMealOrderItems)
                .FirstOrDefaultAsync(o => o.FUserId == userId && o.FOrderStatus == "Cart");

            if (order == null)
                return BadRequest("沒有購物車");  
            order.FOrderStatus = "待付款";
            await _context.SaveChangesAsync();

            return Ok(order.FOrderId);
        }
    }
}
