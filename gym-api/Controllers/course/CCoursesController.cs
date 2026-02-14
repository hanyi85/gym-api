using gym_api.Models;
using gym_api.Models.CourseDTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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


        [HttpGet("search")]
        public async Task<IActionResult> SearchCourses(
      [FromQuery] string city,
      [FromQuery] string venue)
        {
            //  city
            var cityEntity = await _context.CCities
                .FirstOrDefaultAsync(c => c.CityName == city);

            if (cityEntity == null)
                return NotFound("City not found");

            // 2venue
            var venueEntity = await _context.CVenues
                .FirstOrDefaultAsync(v =>
                    v.CityId == cityEntity.CityId &&
                    v.VenueName == venue
                );

            if (venueEntity == null)
                return NotFound("Venue not found");

           
            var courses = await _context.CCourses
                .Where(c =>
                    c.VenueId == venueEntity.VenueId &&
                    !c.IsDeleted
                )
                .Select(c => new
                {
                    id = c.CourseId,
                    name = c.CourseName,
                    courseLevel = c.Courselevel,
                    price = c.Price,
                    duration = c.Duration,
                    imageUrl = c.FImageUrl,
                    categoryId = c.CategoryId,
                    categoryName = c.Category.CategoryName
                })
                .ToListAsync();

            return Ok(new
            {
                city = new
                {
                    id = cityEntity.CityId,
                    name = cityEntity.CityName
                },
                venue = new
                {
                    id = venueEntity.VenueId,
                    name = venueEntity.VenueName
                },
                courses
            });
        }

        [HttpGet("{id}/detail")]
        public async Task<IActionResult> GetCourseDetail(int id)
        {
            var course = await _context.CCourses
                .Where(c => c.CourseId == id)
                .Select(c => new CourseDetailDto
                {
                    Id = c.CourseId,
                    Title = c.CourseName,
                    Category = c.Category.CategoryName,
                    Duration = c.Duration,
                    Price = c.Price,
                    Description = c.Description,

                    CoachName = (
                        from cs in _context.CCourseSchedules
                        join coach in _context.UCoaches
                            on cs.CoachId equals coach.CoachId
                        where cs.CourseId == c.CourseId && cs.IsDeleted == false
                        select coach.Name
                    ).FirstOrDefault() ?? ""
                })
                .FirstOrDefaultAsync();

            if (course == null)
                return NotFound();

            return Ok(course);
        }


    }

}
