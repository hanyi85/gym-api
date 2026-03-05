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
    [ApiController] // 這個屬性是讓 Swagger 抓到它的關鍵
    [Tags("報名表管理")]
    public class YJoinFormsController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public YJoinFormsController(dbFitness2Context context)
        {
            _context = context;
        }

        // GET: api/YJoinForms
        // 取得所有報名表 (通常是後台管理用)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetYJoinForms()
        {
            return await _context.YJoinForms
                .Include(y => y.Event)
                .Include(y => y.User)
                .Select(y => new {
                    y.JoinFormId,
                    y.EventId,
                    y.UserId,
                    y.Name,
                    y.Sex,
                    y.Phone,
                    y.Email,
                    y.Status,
                    y.PayStatus,
                    // 這裡可以根據前端需求挑選要回傳的欄位
                })
                .ToListAsync();
        }

        // GET: api/YJoinForms/5
        [HttpGet("{id}")]
        public async Task<ActionResult<YJoinForm>> GetYJoinForm(int id)
        {
            var yJoinForm = await _context.YJoinForms
                .Include(y => y.Event)
                .Include(y => y.User)
                .FirstOrDefaultAsync(m => m.JoinFormId == id);

            if (yJoinForm == null) return NotFound();

            return yJoinForm;
        }

        // POST: api/YJoinForms
        // 這是你的 Vue 前端「提交報名表」會呼叫的 API
        [HttpPost]
        public async Task<ActionResult<YJoinForm>> PostYJoinForm(YJoinForm yJoinForm)
        {
            // 設定初始值 (例如報名時間)
            yJoinForm.CreatedAt = DateTime.Now;
            yJoinForm.UpdatedAt = DateTime.Now;
            yJoinForm.Status = 0; // 假設 0 代表待審核

            _context.YJoinForms.Add(yJoinForm);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetYJoinForm", new { id = yJoinForm.JoinFormId }, yJoinForm);
        }

        // PUT: api/YJoinForms/5
        // 修改報名資料
        [HttpPut("{id}")]
        public async Task<IActionResult> PutYJoinForm(int id, YJoinForm yJoinForm)
        {
            if (id != yJoinForm.JoinFormId) return BadRequest();

            yJoinForm.UpdatedAt = DateTime.Now;
            _context.Entry(yJoinForm).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!YJoinFormExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // DELETE: api/YJoinForms/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteYJoinForm(int id)
        {
            var yJoinForm = await _context.YJoinForms.FindAsync(id);
            if (yJoinForm == null) return NotFound();

            _context.YJoinForms.Remove(yJoinForm);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool YJoinFormExists(int id)
        {
            return _context.YJoinForms.Any(e => e.JoinFormId == id);
        }
    }
}