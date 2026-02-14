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
    [Tags("場館管理")]
    public class CVenuesController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public CVenuesController(dbFitness2Context context)
        {
            _context = context;
        }

        // GET: api/CVenues
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CVenue>>> GetCVenues([FromQuery] int? cityId)
        {
            var query = _context.CVenues
                .Include(v => v.City)
                .AsQueryable();

            if (cityId.HasValue)
            {
                query = query.Where(v => v.CityId == cityId.Value);
            }

            return await query.ToListAsync();
        }

        // GET: api/CVenues/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CVenue>> GetCVenue(int id)
        {
            var venue = await _context.CVenues
                .Include(v => v.City)
                .FirstOrDefaultAsync(v => v.VenueId == id);

            if (venue == null)
                return NotFound();

            return venue;
        }

        // POST: api/CVenues
        [HttpPost]
        public async Task<ActionResult<CVenue>> PostCVenue(CVenue venue)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.CVenues.Add(venue);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCVenue), new { id = venue.VenueId }, venue);
        }

        // PUT: api/CVenues/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCVenue(int id, CVenue venue)
        {
            if (id != venue.VenueId)
                return BadRequest("VenueId 不一致");

            _context.Entry(venue).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.CVenues.Any(v => v.VenueId == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // DELETE: api/CVenues/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCVenue(int id)
        {
            var venue = await _context.CVenues.FindAsync(id);

            if (venue == null)
                return NotFound();

            _context.CVenues.Remove(venue);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
