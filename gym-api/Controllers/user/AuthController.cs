using gym_api.Models;
using gym_api.Services;
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
    [Tags("會員登入")]
    public class AuthController : ControllerBase
    {
        private readonly dbFitness2Context _context;
        private readonly JwtService _jwtService;
        public AuthController(dbFitness2Context context,JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        //登入
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] ULoginDto dto)
        {
            var user = await _context.UUsers
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
                return Unauthorized("帳號不存在");

            
            using var hmac = new HMACSHA512(user.PasswordSalt);

            var computedHash = Convert.ToBase64String(
                hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password)));

            if (dto.Password != user.Password)
                return Unauthorized("密碼錯誤");

            var token = _jwtService.GenerateAccessToken(user);

            return Ok(new
            {
                token,
                userId = user.UserId,
                Name = user.Name
            });
        }

        //登出
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok("前端刪除 Token 即可");
        }

        //註冊
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (await _context.UUsers.AnyAsync(x => x.Email == dto.Email))
                return BadRequest("信箱已被註冊");

            using var hmac = new HMACSHA512();

            var verifyToken = Guid.NewGuid().ToString();

            var user = new UUser
            {
                Email = dto.Email,
                Account = dto.Email,
                LoginProvider = "local",

                Password = Convert.ToBase64String(
                    hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password))
                ),
                PasswordSalt = hmac.Key,

                Status = 0,
                email_verified_at = null,
                EmailVerifyToken = verifyToken,
                EmailVerifyExpire = DateTime.Now.AddMinutes(30),

                CreatedDate = DateTime.Now
            };

            _context.UUsers.Add(user);
            await _context.SaveChangesAsync();

            var verifyLink = $"https://你的前端網址/users/verify?token={verifyToken}";
            await _emailService.SendVerifyEmail(dto.Email, verifyLink);

            return Ok("請至信箱完成驗證");
        }

        //驗證 Email
        [HttpGet("verify-email")]
        //GET  /api/Auth/verify-email
    }
}
