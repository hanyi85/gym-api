using gym_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace gym_api.Controllers.user
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("會員管理")]
    public class UUsersController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public UUsersController(dbFitness2Context context)
        {
            _context = context;
        }

        // GET: api/UUsers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UUser>>> GetUUsers()
        {
            return await _context.UUsers.ToListAsync();
        }

        // GET: api/UUsers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UUser>> GetUUser(int id)
        {
            var uUser = await _context.UUsers.FindAsync(id);

            if (uUser == null)
            {
                return NotFound();
            }

            return uUser;
        }

        // POST: api/UUsers
        [HttpPost]
        public async Task<ActionResult<UUser>> PostUUser(UUser uUser)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.UUsers.Add(uUser);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUUser), new { id = uUser.UserId }, uUser);
        }

        // PUT: api/UUsers/5
        [HttpPut("{id}")]
        public IActionResult UpdateUser(int id, [FromBody] UUpdateUserDto dto)
        {
            var user = _context.UUsers.FirstOrDefault(u => u.UserId == id);

            if (user == null)
            {
                return NotFound("找不到使用者");
            }

            user.Name = dto.Name ?? user.Name;
            user.Phone = dto.Phone ?? user.Phone;
            user.Address = dto.Address ?? user.Address;

            if (dto.BirthDate.HasValue)
            {
                user.BirthDate = dto.BirthDate.Value;
            }

            _context.SaveChanges();

            return Ok("修改成功");
        }

        //修改密碼
        [HttpPut("{id}/change-password")]
        public async Task<IActionResult> ChangePassword(int id, UChangePasswordDto dto)
        {
            var user = await _context.UUsers.FindAsync(id);

            if (user == null)
                return NotFound("找不到使用者");
            //用資料庫的 salt 建立 HMAC
            using var hmac = new HMACSHA512(user.PasswordSalt);

            // 計算舊密碼 hash
            var oldHash = Convert.ToBase64String(
                hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.OldPassword))
            );

            if (oldHash != user.Password)
                return BadRequest("舊密碼錯誤");

            // 產生新 salt + 新密碼
            using var newHmac = new HMACSHA512();

            user.PasswordSalt = newHmac.Key;
            user.Password = Convert.ToBase64String(
                newHmac.ComputeHash(Encoding.UTF8.GetBytes(dto.NewPassword))
            );

            await _context.SaveChangesAsync();

            return Ok("密碼修改成功");
        }

        // DELETE: api/UUsers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUUser(int id)
        {
            var uUser = await _context.UUsers.FindAsync(id);
            if (uUser == null)
            {
                return NotFound();
            }

            _context.UUsers.Remove(uUser);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UUserExists(int id)
        {
            return _context.UUsers.Any(e => e.UserId == id);
        }
    }
}