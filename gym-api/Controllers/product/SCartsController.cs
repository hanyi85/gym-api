using gym_api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gym_api.Controllers.product
{
    [Route("api/SCarts")]
    [ApiController] // 啟用 API 自動行為與 Swagger 偵測
    [Tags("購物車管理")] // 在 Swagger UI 上顯示的分類標籤
    public class SCartsController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public SCartsController(dbFitness2Context context)
        {
            _context = context;
        }

        /// <summary>
        /// 取得所有購物車內容
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<SCart>>> GetSCarts()
        {
            return await _context.SCarts
                .Include(s => s.Spec)
                .Include(s => s.User)
                .ToListAsync();
        }

        /// <summary>
        /// 根據 ID 查詢購物車項目
        /// </summary>
        /// <param name="id">購物車 ID</param>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SCart>> GetSCart(int id)
        {
            var sCart = await _context.SCarts
                .Include(s => s.Spec)
                .Include(s => s.User)
                .FirstOrDefaultAsync(m => m.CartId == id);

            if (sCart == null)
            {
                return NotFound();
            }

            return sCart;
        }

        /// <summary>
        /// 新增項目到購物車
        /// </summary>
        //[HttpPost]
        //[ProducesResponseType(StatusCodes.Status201Created)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //public async Task<ActionResult<SCart>> PostSCart(SCart sCart)
        //{
        //    if (!ModelState.IsValid) return BadRequest(ModelState);

        //    _context.SCarts.Add(sCart);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction(nameof(GetSCart), new { id = sCart.CartId }, sCart);
        //}

        /// <summary>
        /// 修改購物車項目
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateQuantity(int id, [FromBody] int newQty)
        {
            var sCart = await _context.SCarts.FindAsync(id);
            if (sCart == null) return NotFound();

            sCart.Quantity = newQty;
            await _context.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// 刪除購物車項目
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSCart(int id)
        {
            var sCart = await _context.SCarts.FindAsync(id);
            if (sCart == null)
            {
                return NotFound(new { message = "找不到該項購物車商品" });
            }

            _context.SCarts.Remove(sCart);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "刪除時發生錯誤: " + ex.Message });
            }

            return Ok(new { message = "成功從購物車移除商品" });
        }

        private bool SCartExists(int id)
        {
            return _context.SCarts.Any(e => e.CartId == id);
        }

        [HttpPost("AddToCart")]
        public async Task<IActionResult> AddToCart([FromBody] DTO.SCartsDTO request)
        {
            if (request == null || request.SpecId <= 0 || request.Quantity <= 0)
            {
                return BadRequest("無效的商品資料或數量");
            }

            var existingCartItem = await _context.SCarts
                .FirstOrDefaultAsync(c => c.UserId == request.UserId && c.SpecId == request.SpecId);

            if (existingCartItem != null)
            {
                existingCartItem.Quantity += request.Quantity;
                existingCartItem.TimeStamp = DateTime.Now;
                _context.Entry(existingCartItem).State = EntityState.Modified;
            }
            else
            {
                var newCartItem = new SCart
                {
                    SpecId = request.SpecId,
                    Quantity = request.Quantity,
                    Price = request.Price,
                    UserId = request.UserId,
                    TimeStamp = DateTime.Now
                };
                _context.SCarts.Add(newCartItem);
            }

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "成功加入購物車" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "儲存購物車時發生錯誤：" + ex.Message);
            }
        }

        [HttpGet("User/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetCartByUser(int userId)
        {
            var cartItems = await _context.SCarts
        .Include(s => s.Spec)
            .ThenInclude(spec => spec.PIdNavigation)
                .ThenInclude(p => p.SImages) 
        .Include(s => s.Spec)
            .ThenInclude(spec => spec.SImages) 
                .Where(s => s.UserId == userId)
                .Select(s => new {
                    CartId = s.CartId,
                    SpecId = s.SpecId,
                    Quantity = s.Quantity,
                    Price = s.Price,
                    Name = s.Spec.PIdNavigation.PName + " - " + s.Spec.SpecName,
                    ImagePath = s.Spec.SImages.Where(img => img.ImageType == "Spec").Select(img => img.Picture).FirstOrDefault()
                        ?? s.Spec.PIdNavigation.SImages.Where(img => img.ImageType == "Main").Select(img => img.Picture).FirstOrDefault(),
                    Subtotal = s.Price * s.Quantity
                })
                .ToListAsync();

            return Ok(cartItems);
        }
    }
}