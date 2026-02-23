using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gym_api.Models;
using Microsoft.AspNetCore.Http;

namespace gym_api.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController] // 此特性會自動處理模型驗證 (ModelState.IsValid)
    [Tags("常見問題")]
    public class YFaqsController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public YFaqsController(dbFitness2Context context)
        {
            _context = context;
        }

        // GET: api/YFaqs
        [HttpGet]
        public async Task<IActionResult> GetYFaqs()
        {
            try
            {
                var data = await _context.YFaqs
                    .Include(y => y.QuestionCategory) 
                    .Select(y => new {
                        y.Question,
                        y.Answer,
                        // 抓取分類名稱，如果分類是空的就顯示 "一般問題"
                        CategoryName = y.QuestionCategory != null ? y.QuestionCategory.Name : "一般問題"
                    })
                    .ToListAsync();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"資料抓取失敗: {ex.Message}");
            }
        }
        // GET: api/YFaqs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<YFaq>> GetYFaq(int id)
        {
            var yFaq = await _context.YFaqs
                .Include(y => y.QuestionCategory)
                .FirstOrDefaultAsync(m => m.FaqId == id);

            if (yFaq == null)
            {
                return NotFound();
            }

            return yFaq;
        }

        // POST: api/YFaqs
        [HttpPost]
        public async Task<ActionResult<YFaq>> PostYFaq(YFaq yFaq)
        {
            // 設定初始時間 (選用，根據需求)
            yFaq.CreatedAt = DateTime.Now;

            _context.YFaqs.Add(yFaq);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetYFaq), new { id = yFaq.FaqId }, yFaq);
        }

        // PUT: api/YFaqs/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutYFaq(int id, YFaq yFaq)
        {
            if (id != yFaq.FaqId)
            {
                return BadRequest();
            }

            // 更新修改時間
            yFaq.UpdatedAt = DateTime.Now;
            _context.Entry(yFaq).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!YFaqExists(id))
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

        // DELETE: api/YFaqs/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteYFaq(int id)
        {
            var yFaq = await _context.YFaqs.FindAsync(id);
            if (yFaq == null)
            {
                return NotFound();
            }

            _context.YFaqs.Remove(yFaq);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool YFaqExists(int id)
        {
            return _context.YFaqs.Any(e => e.FaqId == id);
        }
    }
}