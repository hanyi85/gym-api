using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gym_api.Models;

namespace gym_api.Controllers.post
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("貼文管理")]
    public class YPostsController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public YPostsController(dbFitness2Context context)
        {
            _context = context;
        }

        // GET: api/YPosts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<YPost>>> GetYPosts()
        {
            // 使用 AsNoTracking() 可以減少 EF 的負擔
            var posts = await _context.YPosts
                .AsNoTracking()
                .Include(y => y.Coach)
                .Include(y => y.PostCategory)
                 .Include(y => y.ImageRelation)
                 .Include(y => y.TagMapping)
                .ToListAsync();

            return posts;
        }

        // GET: api/YPosts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<YPost>> GetYPost(int id)
        {
            var yPost = await _context.YPosts
                .Include(y => y.Coach)
                .Include(y => y.PostCategory)
                .Include(y => y.ImageRelation)
                .Include(y => y.TagMapping)
                .FirstOrDefaultAsync(m => m.PostId == id);

            if (yPost == null)
            {
                return NotFound(new { message = $"找不到識別碼為 {id} 的貼文" });
            }

            return yPost;
        }

        // POST: api/YPosts
        [HttpPost]
        public async Task<ActionResult<YPost>> PostYPost(YPost yPost)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 強制設定建立時間 (防止前端傳來錯誤的時間)
            yPost.CreatedAt = DateTime.Now;
            yPost.UpdatedAt = DateTime.Now;

            _context.YPosts.Add(yPost);
            await _context.SaveChangesAsync();

            // 重新抓取包含關聯資料的物件，回傳給前端比較完整
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
            if (id != yPost.PostId)
            {
                return BadRequest("URL 的 ID 與內容的 ID 不符");
            }

            // 更新時間
            yPost.UpdatedAt = DateTime.Now;
            _context.Entry(yPost).State = EntityState.Modified;

            // 如果某些欄位不希望被前端修改（例如建立時間），可以在此排除
            // _context.Entry(yPost).Property(x => x.CreatedAt).IsModified = false;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!YPostExists(id))
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

        // DELETE: api/YPosts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteYPost(int id)
        {
            var yPost = await _context.YPosts.FindAsync(id);
            if (yPost == null)
            {
                return NotFound();
            }

            // 建議：如果是正式專案，改用「軟刪除」IsDeleted = true，而不是真的從資料庫移除
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