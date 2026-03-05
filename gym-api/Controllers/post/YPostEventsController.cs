using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gym_api.Models;

namespace gym_api.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController] // 讓 Swagger 抓取並自動處理參數驗證
    [Tags("活動管理")]

    public class YPostEventsController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public YPostEventsController(dbFitness2Context context)
        {
            _context = context;
        }

        // GET: api/YPostEvents
        [HttpGet]
        public async Task<ActionResult<IEnumerable<YPostEvent>>> GetYPostEvents()
        {
            return await _context.YPostEvents.Include(y => y.Post).ToListAsync();
        }

        // 這是你最需要的 API：根據貼文 ID 取得活動詳細資訊
        // GET: api/YPostEvents/ByPost/5
        [HttpGet("ByPost/{postId}")]
        public async Task<ActionResult<object>> GetEventByPostId(int postId)
        {
            var yPostEvent = await _context.YPostEvents
                .Include(y => y.Post)
                .FirstOrDefaultAsync(m => m.PostId == postId);

            if (yPostEvent == null)
            {
                return NotFound(new { message = "該貼文目前沒有關連的活動資訊" });
            }

            // 挑選前端報名表需要的欄位回傳
            return Ok(new
            {
                yPostEvent.EventId,
                yPostEvent.PostId,
                yPostEvent.Fee,
                yPostEvent.Venue,
                yPostEvent.MaxPeople,
                yPostEvent.StartDate,
                yPostEvent.EndDate,
                yPostEvent.Registrationdeadline,
                PostTitle = yPostEvent.Post?.Title
            });
        }

        // GET: api/YPostEvents/5
        [HttpGet("{id}")]
        public async Task<ActionResult<YPostEvent>> GetYPostEvent(int id)
        {
            var yPostEvent = await _context.YPostEvents.FindAsync(id);
            if (yPostEvent == null) return NotFound();
            return yPostEvent;
        }

        // POST: api/YPostEvents
        [HttpPost]
        public async Task<ActionResult<YPostEvent>> PostYPostEvent(YPostEvent yPostEvent)
        {
            _context.YPostEvents.Add(yPostEvent);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetYPostEvent", new { id = yPostEvent.EventId }, yPostEvent);
        }

        // PUT: api/YPostEvents/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutYPostEvent(int id, YPostEvent yPostEvent)
        {
            if (id != yPostEvent.EventId) return BadRequest();

            _context.Entry(yPostEvent).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!YPostEventExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // DELETE: api/YPostEvents/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteYPostEvent(int id)
        {
            var yPostEvent = await _context.YPostEvents.FindAsync(id);
            if (yPostEvent == null) return NotFound();

            _context.YPostEvents.Remove(yPostEvent);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private bool YPostEventExists(int id)
        {
            return _context.YPostEvents.Any(e => e.EventId == id);
        }
    }
}