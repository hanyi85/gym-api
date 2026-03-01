using gym_api.Models;
using gym_api.Models.CourseDTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace gym_api.Controllers.course
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("課程預約管理")]
    public class CourseBookingsController : ControllerBase
    {
        private readonly dbFitness2Context _context;
        public CourseBookingsController(dbFitness2Context context)
        {
            _context = context;
        }

        // GET: /api/coursebookings/history?userId=1
        [HttpGet("history")]
        public async Task<ActionResult<List<BookingHistoryItemDto>>> GetHistory([FromQuery] int userId)
        {
            var data = await (
                from b in _context.CCourseBookings.AsNoTracking()
                join s in _context.CCourseSchedules.AsNoTracking()
                    on b.ScheduleId equals s.ScheduleId
                join c in _context.CCourses.AsNoTracking()
                    on s.CourseId equals c.CourseId
                join coach in _context.UCoaches.AsNoTracking()
                    on s.CoachId equals coach.CoachId
                where !b.IsDeleted && b.UserId == userId
                   && !s.IsDeleted
                   && !c.IsDeleted
                orderby b.CreatedAt descending
                select new BookingHistoryItemDto
                {
                    CourseBookingId = b.CourseBookingId,
                    ScheduleId = b.ScheduleId,

                    BookingTime = b.BookingTime,
                    Status = b.Status,
                    PaymentStatus = b.PaymentStatus,
                    FinalPrice = b.FinalPrice,

                    StartTime = s.StartTime,
                    CourseName = c.CourseName,
                    CoachName = coach.Name,

                    // 是否已評論
                    IsReviewed = _context.CReviews.Any(r =>
                        !r.IsDeleted && r.CourseBookingId == b.CourseBookingId
                    )
                }
            ).ToListAsync();

            return Ok(data);
        }
        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequestDto req)
        {
            if (req == null || req.ScheduleId <= 0 || req.UserId <= 0)
                return BadRequest("資料不完整");

            var now = DateTime.Now;

            // 1) 檢查時段
            var schedule = await _context.CCourseSchedules
                .FirstOrDefaultAsync(s => s.ScheduleId == req.ScheduleId && !s.IsDeleted);

            if (schedule == null) return NotFound("找不到時段");
            if (schedule.Status != "Open") return BadRequest("此時段未開放");
            if (schedule.EnrollDeadline != null && schedule.EnrollDeadline < now)
                return BadRequest("已超過報名截止時間");
            if (schedule.CurrentCapacity >= schedule.MaxCapacity)
                return BadRequest("此時段已額滿");

            // 2) 防止重複預約（建議）
            var existed = await _context.CCourseBookings.AnyAsync(b =>
                !b.IsDeleted &&
                b.UserId == req.UserId &&
                b.ScheduleId == req.ScheduleId &&
                b.Status != "已取消"
            );
            if (existed) return BadRequest("你已經預約過此時段");

            // 3) 建立 booking
            var booking = new CCourseBooking
            {
                ScheduleId = req.ScheduleId,
                UserId = req.UserId,
                BookingTime = now,

                Status = "已報名",
                OriginalPrice = req.FinalPrice + req.DiscountAmount,
                DiscountId = req.DiscountId,
                DiscountAmount = req.DiscountAmount,
                FinalPrice = req.FinalPrice,

                PaymentMethod = req.PaymentMethod,
                PaymentStatus = "已付款",
                PaidAt = now,

                IsDeleted = false,
                CreatedAt = now,
                UpdatedAt = null
            };

            _context.CCourseBookings.Add(booking);

            // 4) 加人數（很重要）
            schedule.CurrentCapacity += 1;
            schedule.UpdatedAt = now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Ok = true,
                CourseBookingId = booking.CourseBookingId
            });
        }
        [HttpPost("pending")]
        public async Task<IActionResult> CreatePending([FromBody] CreatePendingBookingDto req)
        {
            if (req == null || req.ScheduleId <= 0 || req.UserId <= 0)
                return BadRequest("資料不完整");

            var now = DateTime.Now;

            var schedule = await _context.CCourseSchedules
                .FirstOrDefaultAsync(s => s.ScheduleId == req.ScheduleId && !s.IsDeleted);

            if (schedule == null) return NotFound("找不到時段");
            if (schedule.Status != "Open") return BadRequest("此時段未開放");
            if (schedule.EnrollDeadline != null && schedule.EnrollDeadline < now)
                return BadRequest("已超過報名截止時間");
            if (schedule.CurrentCapacity >= schedule.MaxCapacity)
                return BadRequest("此時段已額滿");

            var booking = new CCourseBooking
            {
                ScheduleId = req.ScheduleId,
                UserId = req.UserId,
                BookingTime = now,
                Status = "已報名",
                OriginalPrice = req.FinalPrice + req.DiscountAmount,
                DiscountId = req.DiscountId,
                DiscountAmount = req.DiscountAmount,
                FinalPrice = req.FinalPrice,
                PaymentMethod = "信用卡",
                PaymentStatus = "待付款",
                PaidAt = null,
                IsDeleted = false,
                CreatedAt = now
            };

            _context.CCourseBookings.Add(booking);
            await _context.SaveChangesAsync();

            return Ok(new { ok = true, CourseBookingId = booking.CourseBookingId });
        }

        [HttpPost("checkin/{bookingId}")]
        public async Task<IActionResult> CheckIn(int bookingId)
        {
            var booking = await _context.CCourseBookings
                .FirstOrDefaultAsync(b => b.CourseBookingId == bookingId && !b.IsDeleted);

            if (booking == null) return NotFound("找不到預約");

            if (booking.Status == "已報到")
                return BadRequest("已報到過");

            booking.Status = "已報到";
            booking.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { message = "報到成功", bookingId });
        }

        // DELETE: /api/CourseBookings/123
        [HttpDelete("{id}")]
        public async Task<IActionResult> Cancel(int id)
        {
            var booking = await _context.CCourseBookings
                .FirstOrDefaultAsync(b => b.CourseBookingId == id);

            if (booking == null) return NotFound("找不到此預約");

            // ❌ 已報到不可取消
            if (booking.Status.Contains("已報到"))
                return BadRequest("已報到不可取消");

            // ✅ 開課前 1 小時內不可取消
            var startTime = await _context.CCourseSchedules
                .Where(s => s.ScheduleId == booking.ScheduleId && !s.IsDeleted)
                .Select(s => s.StartTime)
                .FirstOrDefaultAsync();

            if (startTime == default)
                return BadRequest("找不到課程時段");

            var minutes = (startTime - DateTime.Now).TotalMinutes;
            if (minutes <= 60)
                return BadRequest("開課前 1 小時內不可取消");

            booking.IsDeleted = true;
            booking.Status = "Canceled";

            await _context.SaveChangesAsync();
            return Ok(new { message = "已取消預約" });
        }
    }
}