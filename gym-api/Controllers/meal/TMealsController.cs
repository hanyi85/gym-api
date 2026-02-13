using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gym_api.Models;

namespace gym_api.Controllers.meal
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("餐點管理")]
    public class TMealsController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public TMealsController(dbFitness2Context context)
        {
            _context = context;
        }

        // GET: api/TMeals
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TMeal>>> GetTMeals()
        {
            return await _context.TMeals
                .Include(t => t.FCategory)
                .ToListAsync();
        }

        // GET: api/TMeals/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TMeal>> GetTMeal(int id)
        {
            var tMeal = await _context.TMeals
                .Include(t => t.FCategory)
                .FirstOrDefaultAsync(m => m.FMealId == id);

            if (tMeal == null)
            {
                return NotFound();
            }

            return tMeal;
        }

        // POST: api/TMeals
        [HttpPost]
        public async Task<ActionResult<TMeal>> PostTMeal(TMeal tMeal)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.TMeals.Add(tMeal);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTMeal), new { id = tMeal.FMealId }, tMeal);
        }

        // PUT: api/TMeals/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTMeal(int id, TMeal tMeal)
        {
            if (id != tMeal.FMealId)
            {
                return BadRequest("餐點 ID 不符");
            }

            _context.Entry(tMeal).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TMealExists(id))
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

        // DELETE: api/TMeals/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTMeal(int id)
        {
            var tMeal = await _context.TMeals.FindAsync(id);
            if (tMeal == null)
            {
                return NotFound();
            }

            _context.TMeals.Remove(tMeal);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TMealExists(int id)
        {
            return _context.TMeals.Any(e => e.FMealId == id);
        }
    }
}