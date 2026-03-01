using gym_api.Models;
using gym_api.Models.CourseDTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using gym_api.Models.CourseDTOs;

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

        [HttpGet("by-name/{name}")]
        public async Task<IActionResult> GetByName(string name)
        {
            var course = await _context.CCourses
                .Where(c => c.CourseName == name && !c.IsDeleted)
                .Select(c => new CourseDetailDto
                {
                    Id = c.CourseId,
                    Title = c.CourseName,
                    CourseLevel = c.Courselevel,
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
        [HttpGet("{courseId}/schedules")]
        public async Task<IActionResult> GetAvailableSchedules(int courseId)
        {
            var now = DateTime.Now;

        
            var schedules = await _context.CCourseSchedules
                .Where(s =>
                    s.CourseId == courseId &&
                    !s.IsDeleted &&
                    s.Status == "Open"
                )
                .ToListAsync();

          
            var result = schedules
                
                .Where(s => s.StartTime >= now.AddDays(-1))
                .GroupBy(s => s.StartTime.Date)
                .Select(g => new
                {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    slots = g.Select(s => new
                    {
                        scheduleId = s.ScheduleId,
                        time = s.StartTime.ToString("HH:mm"),
                        full = s.CurrentCapacity >= s.MaxCapacity,
                        canEnroll = s.EnrollDeadline == null || s.EnrollDeadline >= now
                    })
                    .OrderBy(x => x.time)
                    .ToList()
                })
                .OrderBy(x => x.date)
                .ToList();

            return Ok(result);
        }


        /// GET: api/CCourses/schedule-detail/5
        [HttpGet("schedule-detail/{scheduleId}")]
        public async Task<IActionResult> GetScheduleDetail(int scheduleId)
        {
            var now = DateTime.Now;

            var data = await (
                from s in _context.CCourseSchedules
                join c in _context.CCourses on s.CourseId equals c.CourseId
                where s.ScheduleId == scheduleId && !s.IsDeleted && !c.IsDeleted
                select new
                {
                    scheduleId = s.ScheduleId,
                    courseId = c.CourseId,
                    courseName = c.CourseName,
                    price = c.Price,
                    date = s.StartTime.ToString("yyyy-MM-dd"),
                    time = s.StartTime.ToString("HH:mm"),
                    full = s.CurrentCapacity >= s.MaxCapacity,
                    canEnroll = s.EnrollDeadline == null || s.EnrollDeadline >= now,
                    coachName = _context.UCoaches
                        .Where(x => x.CoachId == s.CoachId)
                        .Select(x => x.Name)
                        .FirstOrDefault() ?? ""
                }
            ).FirstOrDefaultAsync();

            if (data == null)
                return NotFound("Schedule not found");

            return Ok(data);
        }

        [HttpPost("apply-discount")]
        public async Task<IActionResult> ApplyDiscount([FromBody] ApplyDiscountRequestDto req)
        {
            if (req == null || req.ScheduleId <= 0)
                return BadRequest("資料不完整");

            var now = DateTime.Now;

            // 1) 用 scheduleId 取得原價（永遠以後端為準）
            var schedule = await (
                from s in _context.CCourseSchedules
                join c in _context.CCourses on s.CourseId equals c.CourseId
                where s.ScheduleId == req.ScheduleId && !s.IsDeleted && !c.IsDeleted
                select new
                {
                    basePrice = c.Price
                }
            ).FirstOrDefaultAsync();

            if (schedule == null)
                return NotFound("找不到時段");

            var basePrice = schedule.basePrice;

            // 2) 折扣碼整理
            var code = (req.Code ?? "").Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                return Ok(new
                {
                    ok = true,
                    basePrice,
                    discountCode = "",
                    discountAmount = 0,
                    finalPrice = basePrice
                });
            }

            // 3) 查折扣碼（請確認你的 DbSet 名稱）
            var discount = await _context.CDiscounts
                .Where(d => d.Code == code && d.IsActive && !d.IsDeleted)
                .Select(d => new
                {
                    d.Code,
                    d.DiscountType,
                    d.DiscountValue,
                    d.MinSpend
                })
                .FirstOrDefaultAsync();

            if (discount == null)
                return BadRequest("折扣碼無效或未啟用");

          
            if (basePrice < discount.MinSpend)
                return BadRequest($"未達低消 NT$ {discount.MinSpend}");

            
            int discountAmount = 0;

            if (discount.DiscountType == "百分比")
            {
                discountAmount = (int)Math.Floor(basePrice * (discount.DiscountValue / 100m));
            }
            else if (discount.DiscountType == "金額折抵")
            {
                discountAmount = discount.DiscountValue;
            }
            else
            {
                return BadRequest("折扣類型不支援");
            }

            if (discountAmount > basePrice) discountAmount = basePrice;

            var finalPrice = basePrice - discountAmount;

            return Ok(new
            {
                ok = true,
                basePrice,
                discountCode = discount.Code,
                discountAmount,
                finalPrice
            });
        }
        [HttpGet("payment-summary/{scheduleId}")]
        public async Task<IActionResult> GetPaymentSummary(int scheduleId, [FromQuery] string? code)
        {
            var now = DateTime.Now;

            // 1) schedule + course + coach
            var booking = await (
                from s in _context.CCourseSchedules
                join c in _context.CCourses on s.CourseId equals c.CourseId
                where s.ScheduleId == scheduleId && !s.IsDeleted && !c.IsDeleted
                select new
                {
                    scheduleId = s.ScheduleId,
                    courseId = c.CourseId,
                    courseName = c.CourseName,
                    price = c.Price,
                    date = s.StartTime.ToString("yyyy-MM-dd"),
                    time = s.StartTime.ToString("HH:mm"),
                    full = s.CurrentCapacity >= s.MaxCapacity,
                    canEnroll = s.EnrollDeadline == null || s.EnrollDeadline >= now,
                    coachName = _context.UCoaches
                        .Where(co => co.CoachId == s.CoachId)
                        .Select(co => co.Name)
                        .FirstOrDefault() ?? ""
                }
            ).FirstOrDefaultAsync();

            if (booking == null) return NotFound("找不到時段");
            if (booking.full) return BadRequest("此時段已額滿");
            if (!booking.canEnroll) return BadRequest("已超過報名截止時間");

            // 2) 套用折扣（可選）
            var basePrice = booking.price;
            var discountAmount = 0;
            var discountCode = "";

            var inputCode = (code ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(inputCode))
            {
                var d = await _context.CDiscounts
                    .Where(x => x.Code == inputCode && x.IsActive && !x.IsDeleted)
                    .Select(x => new { x.Code, x.DiscountType, x.DiscountValue, x.MinSpend })
                    .FirstOrDefaultAsync();

                if (d == null) return BadRequest("折扣碼無效或未啟用");
                if (basePrice < d.MinSpend) return BadRequest($"未達低消 NT$ {d.MinSpend}");

                discountCode = d.Code;

                if (d.DiscountType == "百分比")
                    discountAmount = (int)Math.Floor(basePrice * (d.DiscountValue / 100m));
                else
                    discountAmount = d.DiscountValue;
            }

            var finalPrice = Math.Max(0, basePrice - discountAmount);

            return Ok(new
            {
                booking.scheduleId,
                booking.courseId,
                booking.courseName,
                booking.date,
                booking.time,
                booking.coachName,
                originPrice = basePrice,
                discountCode,
                discountAmount,
                finalPrice
            });
        }
    }
}
