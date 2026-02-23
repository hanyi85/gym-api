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

        private string GetUserEmail()
        {
            return User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        }

        // PUT: api/UUsers/5
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateUser([FromBody] UUpdateUserDto dto)
        {
            var email = GetUserEmail();

            var user = await _context.UUsers
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return NotFound("找不到使用者");

            user.Name = dto.Name ?? user.Name;
            user.Phone = dto.Phone ?? user.Phone;
            user.Address = dto.Address ?? user.Address;

            if (dto.BirthDate.HasValue)
                user.BirthDate = dto.BirthDate.Value;

            await _context.SaveChangesAsync();

            return Ok("修改成功");
        }

        //修改密碼
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword(UChangePasswordDto dto)
        {
            var email = GetUserEmail();

            var user = await _context.UUsers
                .FirstOrDefaultAsync(u => u.Email == email);

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


        private bool UUserExists(int id)
        {
            return _context.UUsers.Any(e => e.UserId == id);
        }
    }
}