using gym_api.Models;
using gym_api.Models.CourseDTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace gym_api.Controllers.course
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("課程評論管理")]
    public class ReviewsController : ControllerBase
    {
        private readonly dbFitness2Context _context;
        public ReviewsController(dbFitness2Context context)
        {
            _context = context;
        }

        //  POST: /api/Reviews/course
        // 建立「課程評論」（綁 CourseBookingId）
        [HttpPost("course")]
        public async Task<IActionResult> CreateCourseReview([FromBody] CreateCourseReviewDto req)
        {
            if (req == null) return BadRequest("資料不完整");
            if (req.CourseBookingId <= 0) return BadRequest("CourseBookingId 不可為 0");
            if (req.UserId <= 0) return BadRequest("UserId 不可為 0");
            if (req.Rating < 1 || req.Rating > 5) return BadRequest("Rating 必須 1~5");

            var now = DateTime.Now;

            // 1) 找 booking（確保存在、未刪除）
            var booking = await _context.CCourseBookings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.CourseBookingId == req.CourseBookingId && !b.IsDeleted);

            if (booking == null) return NotFound("找不到此訂單（CourseBookingId）");

            // 2) 防止同一筆訂單重複評論
            var existed = await _context.CReviews.AnyAsync(r =>
                !r.IsDeleted &&
                r.CourseBookingId == req.CourseBookingId
            );
            if (existed) return BadRequest("此訂單已評論過");

            // 3) 建立 Review
            var review = new CReview
            {
                UserId = req.UserId,
                CourseBookingId = req.CourseBookingId,
                CoachBookingId = null,

                Rating = req.Rating,
                TeachingQuality = req.TeachingQuality,
                EnvironmentScore = req.EnvironmentScore,
                DifficultyScore = req.DifficultyScore,
                ValueScore = req.ValueScore,

                Comment = (req.Comment ?? "").Trim(),
                ReviewTime = now,

                IsDeleted = false,
                CreatedAt = now,
                UpdatedAt = null
            };

            _context.CReviews.Add(review);
            await _context.SaveChangesAsync(); // 先存，拿到 ReviewId

            // 4) 寫入 TagMap（可選）
            if (req.TagIds != null && req.TagIds.Count > 0)
            {
                // 過濾掉重複、<=0
                var tagIds = req.TagIds.Where(x => x > 0).Distinct().ToList();

                // 只允許存在且未刪除的 tags
                var validTagIds = await _context.CReviewTags
                    .Where(t => !t.IsDeleted && tagIds.Contains(t.TagId))
                    .Select(t => t.TagId)
                    .ToListAsync();

                foreach (var tagId in validTagIds)
                {
                    _context.CReviewTagMaps.Add(new CReviewTagMap
                    {
                        ReviewId = review.ReviewId,
                        TagId = tagId,
                        IsDeleted = false,
                        CreatedAt = now
                    });
                }

                await _context.SaveChangesAsync();
            }

            return Ok(new { ok = true, ReviewId = review.ReviewId });
        }

        // GET: /api/Reviews/course/{courseId}
        // 讓你「課程詳情頁」底下顯示評價用
        [HttpGet("course/{courseId}")]
        public async Task<ActionResult<List<CourseReviewItemDto>>> GetReviewsByCourse(int courseId)
        {
            if (courseId <= 0) return BadRequest("courseId 不可為 0");

            // 用 join 方式拿到 course / coach / startTime
            var data = await (
                from r in _context.CReviews.AsNoTracking()
                join b in _context.CCourseBookings.AsNoTracking()
                    on r.CourseBookingId equals b.CourseBookingId
                join s in _context.CCourseSchedules.AsNoTracking()
                    on b.ScheduleId equals s.ScheduleId
                join c in _context.CCourses.AsNoTracking()
                    on s.CourseId equals c.CourseId
                join coach in _context.UCoaches.AsNoTracking()
                    on s.CoachId equals coach.CoachId
                where !r.IsDeleted
                      && r.CourseBookingId != null
                      && !b.IsDeleted
                      && !s.IsDeleted
                      && !c.IsDeleted
                      && c.CourseId == courseId
                orderby r.CreatedAt descending
                select new CourseReviewItemDto
                {
                    ReviewId = r.ReviewId,
                    UserId = r.UserId,
                    CourseBookingId = r.CourseBookingId!.Value,

                    Rating = r.Rating,
                    TeachingQuality = r.TeachingQuality,
                    EnvironmentScore = r.EnvironmentScore,
                    DifficultyScore = r.DifficultyScore,
                    ValueScore = r.ValueScore,
                    Comment = r.Comment,
                    ReviewTime = r.ReviewTime,

                    CourseName = c.CourseName,
                    CoachName = coach.Name,
                    StartTime = s.StartTime,
                    BookingNo = "BK" + b.CourseBookingId
                }
            ).ToListAsync();

            // 補 tags（避免 EF N+1：這邊用一次查回來再組）
            var reviewIds = data.Select(x => x.ReviewId).ToList();
            if (reviewIds.Count > 0)
            {
                var tagMap = await (
                    from m in _context.CReviewTagMaps.AsNoTracking()
                    join t in _context.CReviewTags.AsNoTracking()
                        on m.TagId equals t.TagId
                    where !m.IsDeleted && !t.IsDeleted && reviewIds.Contains(m.ReviewId)
                    select new { m.ReviewId, t.TagName }
                ).ToListAsync();

                var dict = tagMap
                    .GroupBy(x => x.ReviewId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.TagName).Distinct().ToList());

                foreach (var item in data)
                {
                    if (dict.TryGetValue(item.ReviewId, out var tags))
                        item.Tags = tags;
                }
            }

            return Ok(data);
        }

        //  GET: /api/Reviews/booking/{courseBookingId}
        // 讓你「評論頁」載入時判斷：已評論就直接顯示或禁用送出
        [HttpGet("booking/{courseBookingId}")]
        public async Task<ActionResult<CourseReviewItemDto?>> GetReviewByBooking(int courseBookingId)
        {
            if (courseBookingId <= 0) return BadRequest("courseBookingId 不可為 0");

            var review = await _context.CReviews
                .AsNoTracking()
                .FirstOrDefaultAsync(r => !r.IsDeleted && r.CourseBookingId == courseBookingId);

            if (review == null) return Ok(null);

            // 這邊簡化回傳（你要完整也能 join）
            return Ok(new
            {
                review.ReviewId,
                review.UserId,
                CourseBookingId = review.CourseBookingId,
                review.Rating,
                review.TeachingQuality,
                review.EnvironmentScore,
                review.DifficultyScore,
                review.ValueScore,
                review.Comment,
                review.ReviewTime
            });
        }
    }
}