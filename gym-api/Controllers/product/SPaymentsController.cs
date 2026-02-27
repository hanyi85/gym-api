using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gym_api.Models;
using Microsoft.AspNetCore.Http;
using gym_api.DTO;

namespace gym_api.Controllers.product
{
    [Route("api/[controller]")]
    [ApiController]
    public class SPaymentsController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public SPaymentsController(dbFitness2Context context)
        {
            _context = context;
        }

        // GET: api/SPayments
        /// <summary>
        /// 取得所有付款方式列表
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SPayment>>> GetSPayments()
        {
            return await _context.SPayments.ToListAsync();
        }

        // GET: api/SPayments/5
        /// <summary>
        /// 根據 ID 查詢特定付款方式
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<SPayment>> GetSPayment(int id)
        {
            var sPayment = await _context.SPayments.FindAsync(id);

            if (sPayment == null)
            {
                return NotFound(new { message = $"找不到 ID 為 {id} 的付款資料" });
            }

            return sPayment;
        }

        // POST: api/SPayments
        /// <summary>
        /// 新增付款方式
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<SPayment>> PostSPayment(SPaymentDTO sPaymentDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 將 DTO 的資料手動對應到資料庫 Model
            var sPayment = new SPayment
            {
                Payment = sPaymentDto.Payment,
                HandlingFee = sPaymentDto.HandlingFee,
                Activate = sPaymentDto.Activate
                // PayId 是資料庫自動產生的，所以這裡不用寫
            };

            _context.SPayments.Add(sPayment);
            await _context.SaveChangesAsync();

            // 成功後回傳 201 Created，並指向查詢該筆資料的 Get 方法
            return CreatedAtAction(nameof(GetSPayment), new { id = sPayment.PayId }, sPayment);
        }

        // PUT: api/SPayments/5
        /// <summary>
        /// 修改特定付款方式
        /// </summary>
        /// <param name="id">要修改的資料 ID</param>
        /// <param name="dto">修改內容 (僅需填寫 Payment, HandlingFee, Activate)</param>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSPayment(int id, SPaymentDTO dto)
        {
            // 1. 先從資料庫找出原本的那筆資料
            var sPayment = await _context.SPayments.FindAsync(id);

            if (sPayment == null)
            {
                return NotFound(new { message = $"找不到 ID 為 {id} 的資料" });
            }

            // 2. 將 DTO 的新值覆蓋到資料庫實體上
            sPayment.Payment = dto.Payment;
            sPayment.HandlingFee = dto.HandlingFee;
            sPayment.Activate = dto.Activate;

            // 3. 儲存變更
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SPaymentExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(new { message = "修改成功", data = sPayment });
        }

        // DELETE: api/SPayments/5
        /// <summary>
        /// 刪除特定付款方式
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSPayment(int id)
        {
            var sPayment = await _context.SPayments.FindAsync(id);
            if (sPayment == null)
            {
                return NotFound();
            }

            _context.SPayments.Remove(sPayment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "刪除成功" });
        }

        private bool SPaymentExists(int id)
        {
            return _context.SPayments.Any(e => e.PayId == id);
        }
    }
}