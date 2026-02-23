using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gym_api.Models;
using Microsoft.AspNetCore.Http;
using gym_api.DTO; // 用於生產狀態碼說明

namespace gym_api.Controllers.product
{
    [Route("api/[controller]")]
    [ApiController] // 啟用 API 行為（如自動模型驗證）
    [Tags("類別管理")]

    public class SCategoriesController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public SCategoriesController(dbFitness2Context context)
        {
            _context = context;
        }

        // GET: api/SCategories
        [HttpGet]
        public async Task<IEnumerable<SCategoryDTO>> GetSCategories()
        {
            return await _context.SCategories
                .Select(c => new SCategoryDTO
                {
                    Name = c.CName,
                    SubCategories = c.SProducts
                                     .Select(p => p.PName)
                                     .Distinct() 
                                     .ToList()
                })
                .ToListAsync();
        }

        // GET: api/SCategories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SCategory>> GetSCategory(int id)
        {
            var sCategory = await _context.SCategories.FindAsync(id);

            if (sCategory == null)
            {
                return NotFound();
            }

            return sCategory;
        }

        // POST: api/SCategories
        [HttpPost]
        public async Task<ActionResult<SCategory>> PostSCategory(SCategory sCategory)
        {
            _context.SCategories.Add(sCategory);
            await _context.SaveChangesAsync();

            // 成功後傳回 201 Created，並附上查詢該資料的 URL
            return CreatedAtAction(nameof(GetSCategory), new { id = sCategory.CId }, sCategory);
        }

        // PUT: api/SCategories/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSCategory(int id, SCategory sCategory)
        {
            if (id != sCategory.CId)
            {
                return BadRequest("ID 不符");
            }

            _context.Entry(sCategory).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SCategoryExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent(); // 成功更新通常回傳 204
        }

        // DELETE: api/SCategories/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSCategory(int id)
        {
            var sCategory = await _context.SCategories.FindAsync(id);
            if (sCategory == null)
            {
                return NotFound();
            }

            _context.SCategories.Remove(sCategory);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SCategoryExists(int id)
        {
            return _context.SCategories.Any(e => e.CId == id);
        }
    }
}