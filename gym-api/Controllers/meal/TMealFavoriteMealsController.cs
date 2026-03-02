using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gym_api.Models;
using gym_api.DTOs;

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
        public async Task<ActionResult<IEnumerable<TMeal>>> GetByUser(int userId)
        {
            var meals = await _context.TMealFavoriteMeals
         .Include(x => x.FMeal)
         .Where(x => x.FUserId == userId)
         .Select(x => x.FMeal)
         .Where(m => m.FIsActive)
         .ToListAsync();

            return Ok(meals);
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

        [HttpPost("toggle")]
        public async Task<ActionResult> ToggleFavorite([FromBody] MealFavoriteDto dto)
        {
            var existing = await _context.TMealFavoriteMeals
                .FirstOrDefaultAsync(f => f.FUserId == dto.FUserId && f.FMealId == dto.FMealId);

            if (existing != null)
            {
                _context.TMealFavoriteMeals.Remove(existing);
                await _context.SaveChangesAsync();
                return Ok(new { isFavorite = false });
            }

            var newFavorite = new TMealFavoriteMeal
            {
                FUserId = dto.FUserId,
                FMealId = dto.FMealId,
                FCreatedAt = DateTime.Now
            };

            _context.TMealFavoriteMeals.Add(newFavorite);
            await _context.SaveChangesAsync();

            return Ok(new { isFavorite = true });
        }

        // 🔥 只回傳餐點 ID 給 Vue 用（給 Vue 用很重要）
        [HttpGet("user/{userId}/ids")]
        public async Task<ActionResult<IEnumerable<int>>> GetFavoriteIds(int userId)
        {
            var ids = await _context.TMealFavoriteMeals
                .Where(x => x.FUserId == userId)
                .Select(x => x.FMealId)
                .ToListAsync();

            return Ok(ids);
        }


    }
}
