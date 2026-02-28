using gym_api.Models;
using gym_api.Models.UDTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace gym_api.Controllers.user
{

        // GET: WeightRecords
        [Authorize]
        [ApiController]
        [Route("api/[controller]")]
    [Tags("會員體重紀錄")]
    public class WeightRecordsController : ControllerBase
        {
            private readonly dbFitness2Context _context;

            public WeightRecordsController(dbFitness2Context context)
            {
                _context = context;
            }

            // 取得目前登入者的 UserId
            private int GetUserId()
            {
                return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            }

            // ===============================
            // 新增紀錄
            // POST: api/weightrecords
            // ===============================
            [HttpPost]
            public async Task<IActionResult> AddRecord([FromBody] UWeightRecordDto dto)
            {
                var userId = GetUserId();

                var record = new UUserWeightRecord
                {
                    UserId = userId,
                    RecordDate = DateOnly.FromDateTime(DateTime.Now),
                    Weight = dto.Weight,
                    BodyFat = dto.BodyFat,
                    MuscleMass = dto.MuscleMass,
                    CreatedAt = DateTime.Now
                };

                _context.UUserWeightRecords.Add(record);
                await _context.SaveChangesAsync();

                return Ok(new { message = "新增成功" });
            }

            // ===============================
            // 取得所有紀錄（最新在前）
            // GET: api/weightrecords
            // ===============================
            [HttpGet]
            public async Task<IActionResult> GetRecords()
            {
                var userId = GetUserId();

                var records = await _context.UUserWeightRecords
                    .Where(x => x.UserId == userId)
                    .OrderByDescending(x => x.RecordDate)
                    .Select(x => new
                    {
                        x.RecordId,
                        x.RecordDate,
                        x.Weight,
                        x.BodyFat,
                        x.MuscleMass
                    })
                    .ToListAsync();

                return Ok(records);
            }

            // ===============================
            // 取得最近一筆
            // GET: api/weightrecords/latest
            // ===============================
            [HttpGet("latest")]
            public async Task<IActionResult> GetLatest()
            {
                var userId = GetUserId();

                var latest = await _context.UUserWeightRecords
                    .Where(x => x.UserId == userId)
                    .OrderByDescending(x => x.RecordDate)
                    .FirstOrDefaultAsync();

                if (latest == null)
                    return NotFound();

                return Ok(latest);
            }
        // 更新一筆資料

        [HttpPut("{id}")]
        public async Task<IActionResult> PutWeightRecord(int id, UWeightRecordDto dto)
        {
            var userId = GetUserId();

            var existingRecord = await _context.UUserWeightRecords
                .FirstOrDefaultAsync(x => x.RecordId == id && x.UserId == userId);

            if (existingRecord == null)
                return NotFound();

            existingRecord.RecordDate = dto.RecordDate;
            existingRecord.Weight = dto.Weight;
            existingRecord.BodyFat = dto.BodyFat;
            existingRecord.MuscleMass = dto.MuscleMass;

            await _context.SaveChangesAsync();

            return Ok(new { message = "更新成功" });
        }
        // 刪除紀錄
        // DELETE: api/weightrecords/{id}
        // ===============================
        [HttpDelete("{id}")]
            public async Task<IActionResult> Delete(int id)
            {
                var userId = GetUserId();

                var record = await _context.UUserWeightRecords
                    .FirstOrDefaultAsync(x => x.RecordId == id && x.UserId == userId);

                if (record == null)
                    return NotFound();

                _context.UUserWeightRecords.Remove(record);
                await _context.SaveChangesAsync();

                return Ok(new { message = "刪除成功" });
            }
        
        private bool UExerciseRecordExists(int id)
        {
            return _context.UExerciseRecords.Any(e => e.ExerciseId == id);
        }
    }
}
