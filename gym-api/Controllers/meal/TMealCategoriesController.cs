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
    [Tags("餐點類別管理")]
    public class TMealCategoriesController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public TMealCategoriesController(dbFitness2Context context)
        {
            _context = context;
        }

        // GET: api/TMealCategories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TMealCategory>>> GetCategories()
        {
            return await _context.TMealCategories.ToListAsync();
        }

        // GET: api/TMealCategories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TMealCategory>> GetCategory(int id)
        {
            var category = await _context.TMealCategories
                .FirstOrDefaultAsync(m => m.FCategoryId == id);

            if (category == null)
                return NotFound();

            return category;
        }

        // POST: api/TMealCategories
        [HttpPost]
        public async Task<ActionResult<TMealCategory>> CreateCategory(TMealCategory tMealCategory)
        {
            _context.TMealCategories.Add(tMealCategory);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetCategory),
                new { id = tMealCategory.FCategoryId },
                tMealCategory);
        }

        // PUT: api/TMealCategories/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, TMealCategory tMealCategory)
        {
            if (id != tMealCategory.FCategoryId)
                return BadRequest();

            _context.Entry(tMealCategory).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.TMealCategories.Any(e => e.FCategoryId == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // DELETE: api/TMealCategories/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.TMealCategories.FindAsync(id);
            if (category == null)
                return NotFound();

            _context.TMealCategories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}


