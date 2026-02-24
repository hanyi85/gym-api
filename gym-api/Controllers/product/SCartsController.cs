using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gym_api.Models;
using Microsoft.AspNetCore.Http;

namespace gym_api.Controllers.product
{
    [Route("api/[controller]")]
    [ApiController] // 啟用 API 自動行為與 Swagger 偵測
    [Tags("購物車管理")] // 在 Swagger UI 上顯示的分類標籤
    public class SCartsApiController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public SCartsApiController(dbFitness2Context context)
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
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PutSCart(int id, SCart sCart)
        {
            if (id != sCart.CartId)
            {
                return BadRequest("ID 不符");
            }

            _context.Entry(sCart).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SCartExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        /// <summary>
        /// 刪除購物車項目
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteSCart(int id)
        {
            var sCart = await _context.SCarts.FindAsync(id);
            if (sCart == null)
            {
                return NotFound();
            }

            _context.SCarts.Remove(sCart);
            await _context.SaveChangesAsync();

            return NoContent();
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
    }
}