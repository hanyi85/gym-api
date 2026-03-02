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
    [Tags("餐點取餐時間管理")]
    public class TMealPickUpTimesController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public TMealPickUpTimesController(dbFitness2Context context)
        {
            _context = context;
        }

        // GET: api/TMealPickUpTimes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TMealPickUpTime>>> GetPickUpTimes()
        {
            return await _context.TMealPickUpTimes
                .OrderBy(t => t.FStartTime)
                .ToListAsync();
        }

        // 只取啟用時段（給前端下拉選單用）
        // GET: api/TMealPickUpTimes/active
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<TMealPickUpTime>>> GetActivePickUpTimes()
        {
            return await _context.TMealPickUpTimes
                .Where(t => t.FIsActive)
                .OrderBy(t => t.FStartTime)
                .ToListAsync();
        }

        // GET: api/TMealPickUpTimes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TMealPickUpTime>> GetPickUpTime(int id)
        {
            var time = await _context.TMealPickUpTimes
                .FirstOrDefaultAsync(m => m.FPickUpTimeId == id);

            if (time == null)
                return NotFound();

            return time;
        }

        // POST: api/TMealPickUpTimes
        [HttpPost]
        public async Task<ActionResult<TMealPickUpTime>> CreatePickUpTime(TMealPickUpTime time)
        {
            _context.TMealPickUpTimes.Add(time);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetPickUpTime),
                new { id = time.FPickUpTimeId },
                time);
        }

        // PUT: api/TMealPickUpTimes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePickUpTime(int id, TMealPickUpTime time)
        {
            if (id != time.FPickUpTimeId)
                return BadRequest();

            _context.Entry(time).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.TMealPickUpTimes.Any(e => e.FPickUpTimeId == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // DELETE: api/TMealPickUpTimes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePickUpTime(int id)
        {
            var time = await _context.TMealPickUpTimes.FindAsync(id);

            if (time == null)
                return NotFound();

            _context.TMealPickUpTimes.Remove(time);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

