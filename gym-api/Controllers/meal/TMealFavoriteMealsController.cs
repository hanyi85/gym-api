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
    [Tags("餐點我的最愛管理")]
    public class TMealFavoriteMealsController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public TMealFavoriteMealsController(dbFitness2Context context)
        {
            _context = context;
        }

        // GET: api/TMealFavoriteMeals
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TMealFavoriteMeal>>> GetFavoriteMeals()
        {
            return await _context.TMealFavoriteMeals
                .Include(t => t.FMeal)
                .Include(t => t.FUser)
                .ToListAsync();
        }

        // GET: api/TMealFavoriteMeals/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TMealFavoriteMeal>> GetFavoriteMeal(int id)
        {
            var favorite = await _context.TMealFavoriteMeals
                .Include(t => t.FMeal)
                .Include(t => t.FUser)
                .FirstOrDefaultAsync(m => m.FFavoriteMealId == id);

            if (favorite == null)
                return NotFound();

            return favorite;
        }

        // 🔥 依照使用者查詢（給 Vue 用很重要）
        // GET: api/TMealFavoriteMeals/user/3
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<TMealFavoriteMeal>>> GetByUser(int userId)
        {
            return await _context.TMealFavoriteMeals
                .Include(t => t.FMeal)
                .Where(x => x.FUserId == userId)
                .ToListAsync();
        }

        // POST: api/TMealFavoriteMeals
        [HttpPost]
        public async Task<ActionResult<TMealFavoriteMeal>> CreateFavorite(TMealFavoriteMeal favorite)
        {
            favorite.FCreatedAt = DateTime.Now;

            _context.TMealFavoriteMeals.Add(favorite);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetFavoriteMeal),
                new { id = favorite.FFavoriteMealId },
                favorite);
        }

        // PUT: api/TMealFavoriteMeals/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFavorite(int id, TMealFavoriteMeal favorite)
        {
            if (id != favorite.FFavoriteMealId)
                return BadRequest();

            _context.Entry(favorite).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.TMealFavoriteMeals.Any(e => e.FFavoriteMealId == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // DELETE: api/TMealFavoriteMeals/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFavorite(int id)
        {
            var favorite = await _context.TMealFavoriteMeals.FindAsync(id);

            if (favorite == null)
                return NotFound();

            _context.TMealFavoriteMeals.Remove(favorite);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
