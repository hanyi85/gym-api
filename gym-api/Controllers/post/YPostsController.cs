using gym_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace gym_api.Controllers.post
{
    // --- 輔助類別：定義接收前端報名的資料結構 ---
    public class RegistrationDto
    {
        public int PostId { get; set; }
        public int? UserId { get; set; }
        public string Name { get; set; }
        public string Sex { get; set; } // 接收 "1", "0", "2"
        public string Email { get; set; }
        public string Phone { get; set; }
        public string PaymentMethod { get; set; } // "LINEPAY", "ATM"
        public string CaptchaToken { get; set; }
    }

    // --- Google reCAPTCHA 回傳格式 ---
    public class ReCaptchaResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
        [JsonProperty("error-codes")]
        public List<string> ErrorCodes { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    [Tags("貼文管理")]
    public class YPostsController : ControllerBase
    {
        private readonly dbFitness2Context _context;
        // 注意：請在此處填入你的 Secret Key
        private readonly string _reCaptchaSecret = "6LcNAHcsAAAAAAj_BM62lS9Ab6Kplzo0B8wPq1X_";

        public YPostsController(dbFitness2Context context)
        {
            _context = context;
        }

        // --- 核心新增功能：報名並驗證 reCAPTCHA ---
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegistrationDto dto)
        {
            // 1. 檢查 DTO 是否為空
            if (dto == null)
                return BadRequest(new { message = "接收不到表單資料，請檢查格式" });

            if (string.IsNullOrEmpty(dto.CaptchaToken))
                return BadRequest(new { message = "驗證碼無效，請重新嘗試" });

            // 2. Google reCAPTCHA 驗證 (保持不變)
            using var client = new HttpClient();
            var content = new FormUrlEncodedContent(new[]
            {
        new KeyValuePair<string, string>("secret", _reCaptchaSecret),
        new KeyValuePair<string, string>("response", dto.CaptchaToken)
    });

            var response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
            var resultJson = await response.Content.ReadAsStringAsync();
            var captchaResult = JsonConvert.DeserializeObject<ReCaptchaResponse>(resultJson);

            if (captchaResult == null || !captchaResult.Success)
                return BadRequest(new { message = "機器人驗證失敗，請重試" });

            // 3. 取得活動資訊
            var eventInfo = await _context.YPostEvents.FirstOrDefaultAsync(e => e.PostId == dto.PostId);
            if (eventInfo == null)
                return NotFound(new { message = "找不到相關活動資訊，請確認 PostId 是否正確" });

            // 4. 執行存檔
            try
            {
                // 安全轉換 Sex：解析失敗預設為 2 (其他)
                if (!int.TryParse(dto.Sex, out int sexValue))
                {
                    sexValue = 2;
                }

                var newJoin = new YJoinForm
                {
                    EventId = eventInfo.EventId,
                    UserId = dto.UserId,
                    Name = dto.Name ?? "未知用戶",
                    Sex = sexValue,
                    Phone = dto.Phone ?? "",
                    Email = dto.Email ?? "",
                    Status = 1,
                    PayStatus = (eventInfo.Fee > 0) ? 0 : 1,
                    PayMethod = dto.PaymentMethod == "LINEPAY" ? 1 : 2,
                    Fee = eventInfo.Fee ?? 0,
                    CheckStatus = 0,
                    CreatedAt = DateTime.Now,
                    IsDeleted = false
                };

                _context.YJoinForms.Add(newJoin);
                await _context.SaveChangesAsync();

                return Ok(new { message = "報名成功！", id = newJoin.JoinFormId });
            }
            catch (Exception ex)
            {
                // 這裡如果是 400，代表資料庫欄位長度不夠或是限制 (如：Name 太長)
                return BadRequest(new { message = "報名存檔失敗", detail = ex.Message });
            }
        }

        // GET: api/YPosts
        [HttpGet]
        public async Task<IActionResult> GetYPosts()
        {
            try
            {
                var posts = await _context.YPosts
                    .AsNoTracking()
                    .Include(y => y.PostCategory)
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new {
                        Id = p.PostId,
                        Title = p.Title,
                        Detail = p.Detail,
                        TagName = p.PostCategory != null ? p.PostCategory.Name : "其他",
                        IsPinned = p.IsPinned,
                        Date = p.CreatedAt.ToString("yyyy/MM/dd")
                    })
                    .ToListAsync();

                return Ok(posts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"抓取失敗: {ex.Message}");
            }
        }

        // GET: api/YPosts/5
        [HttpGet("{id}")]
        public async Task<ActionResult> GetYPost(int id)
        {
            var yPost = await _context.YPosts
                .Include(y => y.Coach)
                .Include(y => y.PostCategory)
                .Include(y => y.YPostEvents)
                .FirstOrDefaultAsync(m => m.PostId == id);

            if (yPost == null) return NotFound(new { message = "找不到貼文" });

            try
            {
                yPost.ViewCount = (yPost.ViewCount) + 1;
                _context.Entry(yPost).Property(x => x.ViewCount).IsModified = true;
                await _context.SaveChangesAsync();
            }
            catch (Exception) { }

            var evt = yPost.YPostEvents.FirstOrDefault();

            return Ok(new
            {
                Id = yPost.PostId,
                Title = yPost.Title,
                Detail = yPost.Detail,
                ImageUrl = yPost.ImageRelationId,
                CreatedAt = yPost.CreatedAt,
                ViewCount = yPost.ViewCount,
                CoachName = yPost.Coach?.Name,
                CategoryName = yPost.PostCategory?.Name ?? "其他",
                PostCategoryId = yPost.PostCategoryId,
                EventInfo = evt != null ? new
                {
                    EventId = evt.EventId,
                    Fee = evt.Fee,
                    Venue = evt.Venue,
                    StartDate = evt.StartDate,
                    EndDate = evt.EndDate,
                    RegistrationDeadline = evt.Registrationdeadline,
                    EventDetail = evt.Detail,
                    Status = evt.Status
                } : null
            });
        }

        // POST: api/YPosts
        [HttpPost]
        public async Task<ActionResult<YPost>> PostYPost(YPost yPost)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            yPost.CreatedAt = DateTime.Now;
            yPost.UpdatedAt = DateTime.Now;

            _context.YPosts.Add(yPost);
            await _context.SaveChangesAsync();

            var result = await _context.YPosts
                .Include(y => y.Coach)
                .Include(y => y.PostCategory)
                .FirstOrDefaultAsync(x => x.PostId == yPost.PostId);

            return CreatedAtAction(nameof(GetYPost), new { id = yPost.PostId }, result);
        }

        // PUT: api/YPosts/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutYPost(int id, YPost yPost)
        {
            if (id != yPost.PostId) return BadRequest("ID 不符");

            yPost.UpdatedAt = DateTime.Now;
            _context.Entry(yPost).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!YPostExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // DELETE: api/YPosts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteYPost(int id)
        {
            var yPost = await _context.YPosts.FindAsync(id);
            if (yPost == null) return NotFound();

            _context.YPosts.Remove(yPost);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool YPostExists(int id)
        {
            return _context.YPosts.Any(e => e.PostId == id);
        }
    }
}