using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using gym_api.Models;
using gym_api.Models.CourseDTOs;


namespace gym_api.Controllers.course
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("城市管理")]
    public class CCitiesController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public CCitiesController(dbFitness2Context context)
        {
            _context = context;
        }

        // GET: api/CCities
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CCity>>> GetCCities()
        {
            return await _context.CCities
        .Include(c => c.CVenues)
        .ToListAsync();
        }

        // GET: api/CCities/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CCity>> GetCCity(int id)
        {
            var cCity = await _context.CCities
                .FirstOrDefaultAsync(c => c.CityId == id);

            if (cCity == null)
            {
                return NotFound();
            }

            return cCity;
        }

        // POST: api/CCities
        [HttpPost]
        public async Task<ActionResult<CCity>> PostCCity(CCity cCity)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.CCities.Add(cCity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetCCity),
                new { id = cCity.CityId },
                cCity
            );
        }

        // PUT: api/CCities/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCCity(int id, CCity cCity)
        {
            if (id != cCity.CityId)
            {
                return BadRequest("CityId 不一致");
            }

            _context.Entry(cCity).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CCityExists(id))
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

        // DELETE: api/CCities/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCCity(int id)
        {
            var cCity = await _context.CCities.FindAsync(id);

            if (cCity == null)
            {
                return NotFound();
            }

            _context.CCities.Remove(cCity);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CCityExists(int id)
        {
            return _context.CCities.Any(e => e.CityId == id);
        }
    }
}