using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gym_api.Models;

namespace gym_api.Controllers.course
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("課程管理")] 
    public class CCoursesController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public CCoursesController(dbFitness2Context context)
        {
            _context = context;
        }

        // GET: api/CCourses
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CCourse>>> GetCCourses()
        {
            return await _context.CCourses
                .Include(c => c.Category)
                .Include(c => c.Venue)
                .ToListAsync();
        }

        // GET: api/CCourses/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CCourse>> GetCCourse(int id)
        {
            var cCourse = await _context.CCourses
                .Include(c => c.Category)
                .Include(c => c.Venue)
                .FirstOrDefaultAsync(m => m.CourseId == id);

            if (cCourse == null)
            {
                return NotFound();
            }

            return cCourse;
        }

        // POST: api/CCourses
        [HttpPost]
        public async Task<ActionResult<CCourse>> PostCCourse(CCourse cCourse)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.CCourses.Add(cCourse);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCCourse), new { id = cCourse.CourseId }, cCourse);
        }

        // PUT: api/CCourses/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCCourse(int id, CCourse cCourse)
        {
            if (id != cCourse.CourseId)
            {
                return BadRequest();
            }

            _context.Entry(cCourse).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CCourseExists(id))
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

        // DELETE: api/CCourses/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCCourse(int id)
        {
            var cCourse = await _context.CCourses.FindAsync(id);
            if (cCourse == null)
            {
                return NotFound();
            }

            _context.CCourses.Remove(cCourse);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CCourseExists(int id)
        {
            return _context.CCourses.Any(e => e.CourseId == id);
        }
    }
}