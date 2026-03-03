using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using gym_api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace gym_api.Controllers.meal
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("餐點員工QRcode取餐")]
    public class TMealPickUpQrcodeController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public TMealPickUpQrcodeController(dbFitness2Context context)
        {
            _context = context;
        }

        // GET: api/TMealPickUpQrcode/confirm/{qrContent}
        //  查詢 QRCode
        [HttpGet("confirm/{qrContent}")]
        public IActionResult Confirm(string qrContent)
        {
            var item = _context.TMealOrderItems
                .Include(o => o.FMeal)
                .Include(o => o.FPickTime)
                .Include(o => o.FOrder)
                    .ThenInclude(o => o.FVenue)
                .Include(o => o.FOrder)
                    .ThenInclude(o => o.FUser)
                .FirstOrDefault(o => o.FQrContent == qrContent);

            if (item == null)
                return NotFound(new { message = "QRCode 無效或不存在" });

            return Ok(new
            {
                item.FOrderItemId,
                MealName = item.FMeal.FMealName,
                item.FQty,
                VenueName = item.FOrder.FVenue.VenueName,
                PickDate = item.FPickDate.ToString("yyyy-MM-dd"),
                PickTime = $"{item.FPickTime.FStartTime} ~ {item.FPickTime.FEndTime}",
                UserName = item.FOrder.FUser.Name,
                Phone = item.FOrder.FUser.Phone,
                PayMethod=item.FOrder.FPayMethod,
                item.FPickupStatus
            });
        }

        // GET: apiTMealPickUpQrcode/pickup/{orderItemId}
        //  確認取餐
        [HttpPost("Pickup/{orderItemId}")]
        public IActionResult Pickup(int orderItemId)
        {
            var item = _context.TMealOrderItems
                .Include(x => x.FOrder)
                .FirstOrDefault(x => x.FOrderItemId == orderItemId);

            if (item == null)
                return NotFound();

            if (item.FPickupStatus)
                return BadRequest(new { message = "此餐點已領取" });

            item.FPickupStatus = true;
            _context.SaveChanges();

            bool allPickedUp = _context.TMealOrderItems
                .Where(x => x.FOrderId == item.FOrderId)
                .All(x => x.FPickupStatus);

            if (allPickedUp)
            {
                item.FOrder.FOrderStatus = "訂單完成";
                _context.SaveChanges();
            }

            return Ok(new { message = "取餐完成" });
        }
    }
}
