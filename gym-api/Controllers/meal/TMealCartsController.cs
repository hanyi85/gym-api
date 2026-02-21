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

        // GET: api/TMealCarts/MealAddToCart
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
    }
}
