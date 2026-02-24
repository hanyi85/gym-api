using gym_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using gym_api.Models.UDTO;

namespace gym_api.Controllers.user
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Tags("會員管理")]
    public class UUsersController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public UUsersController(dbFitness2Context context)
        {
            _context = context;
        }


        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var user = await _context.UUsers.FindAsync(userId);

            if (user == null) return NotFound();

            return Ok(new
            {
                user.Name,
                user.Email,
                user.Phone,
                user.Sex,
                user.BirthDate,
                user.Address,
                Image = user.Image != null ? Convert.ToBase64String(user.Image) : null
            });
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(UUpdateUserDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized();

            var userId = int.Parse(userIdClaim.Value);
            var user = await _context.UUsers.FindAsync(userId);

            if (user == null)
                return NotFound();

            if (!string.IsNullOrEmpty(dto.Name))
                user.Name = dto.Name;

            if (!string.IsNullOrEmpty(dto.Phone))
                user.Phone = dto.Phone;

            if (!string.IsNullOrEmpty(dto.Sex))
                user.Sex = dto.Sex;

            if (!string.IsNullOrEmpty(dto.Address))
                user.Address = dto.Address;

            user.BirthDate = dto.BirthDate ?? user.BirthDate;

            await _context.SaveChangesAsync();

            return Ok(new { message = "更新成功" });
        }
        //修改密碼
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword(UChangePasswordDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var user = await _context.UUsers.FindAsync(userId);

            if (user == null)
                return NotFound("找不到使用者");

            using var hmac = new HMACSHA512(user.PasswordSalt);

            var oldHash = Convert.ToBase64String(
                hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.OldPassword))
            );

            if (oldHash != user.Password)
                return BadRequest("舊密碼錯誤");

            using var newHmac = new HMACSHA512();

            user.PasswordSalt = newHmac.Key;
            user.Password = Convert.ToBase64String(
                newHmac.ComputeHash(Encoding.UTF8.GetBytes(dto.NewPassword))
            );

            await _context.SaveChangesAsync();

            return Ok("密碼修改成功");
        }

        [Authorize]
        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.UUsers.FindAsync(userId);

            if (user == null) return NotFound();

            if (file == null || file.Length == 0)
                return BadRequest("未選擇圖片");

            if (file.Length > 800 * 1024)
                return BadRequest("圖片不能超過 800KB");

            var allowedTypes = new[] { "image/jpeg", "image/png" };

            if (!allowedTypes.Contains(file.ContentType))
                return BadRequest("只允許 JPG 或 PNG");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            user.Image = ms.ToArray();

            await _context.SaveChangesAsync();

            return Ok("頭像上傳成功");
        }
        private bool UUserExists(int id)
        {
            return _context.UUsers.Any(e => e.UserId == id);
        }
    }
}