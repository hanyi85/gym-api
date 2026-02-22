using gym_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace gym_api.Controllers.user
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public AuthController(dbFitness2Context context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(ULoginDto dto)
        {
            var user = await _context.UUsers
                .FirstOrDefaultAsync(u => u.Account == dto.Account);

            if (user == null)
                return Unauthorized("帳號不存在");

            using var hmac = new HMACSHA512(user.PasswordSalt);

            var computedHash = Convert.ToBase64String(
                hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password))
            );

            if (computedHash != user.Password)
                return Unauthorized("密碼錯誤");

            return Ok(new
            {
                message = "登入成功",
                userId = user.UserId,
                account = user.Account
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (await _context.UUsers.AnyAsync(x => x.Account == dto.Account))
                return BadRequest("帳號已存在");

            using var hmac = new HMACSHA512();

            var user = new UUser
            {
                Account = dto.Account,
                Name = dto.Name,

                // 補齊 NOT NULL 欄位
                Sex = "未填",                   // 或 "男"
                BirthDate = DateOnly.FromDateTime(DateTime.Now),    // 先給今天
                Phone = "",
                Address = "",
                Email = "",
                Status = 1,                     // 正常會員

                // 密碼相關
                Password = Convert.ToBase64String(
                    hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password))
                ),
                PasswordSalt = hmac.Key
            };

            _context.UUsers.Add(user);
            await _context.SaveChangesAsync();

            return Ok("註冊成功");
        }
    }
}
