using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gym_api.Models;
using gym_api.Models.CourseDTOs;

namespace gym_api.Controllers.course
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("教練查詢")]
    public class CoachesController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public CoachesController(dbFitness2Context context)
        {
            _context = context;
        }

        // GET /api/coaches?venueId=1
        [HttpGet]
        public async Task<ActionResult<List<CoachListItemDto>>> Get([FromQuery] int? venueId)
        {
            var q = _context.UCoaches
                .AsNoTracking()
                .Where(c => c.Status == 1); // 先假設 1=啟用

            if (venueId.HasValue && venueId.Value > 0)
                q = q.Where(c => c.VenueId == venueId.Value);

            var data = await q
                .Select(c => new CoachListItemDto
                {
                    CoachId = c.CoachId,
                    Name = c.Name,
                    VenueId = c.VenueId,
                    Description = c.Descrition,
                    HourlyRate = c.HourlyRate,
                    Skills = c.UCoachSkills.Select(s => s.SkillName).ToList(),
                    ImageUrl = null
                })
                .ToListAsync();

            return Ok(data);
        }
    }
}