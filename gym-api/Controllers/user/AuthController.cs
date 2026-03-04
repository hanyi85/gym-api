using Google.Apis.Auth;
using gym_api.Models;
using gym_api.Models.UDTO;
using gym_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using NuGet.Common;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace gym_api.Controllers.user
{
    [ApiController]
    [Route("api/[controller]")]
    [Tags("會員登入")]
    public class AuthController : ControllerBase
    {
        private readonly dbFitness2Context _context;
        private readonly JwtService _jwtService;
        private readonly EmailService _emailService;
        private readonly IConfiguration _config;
        public AuthController(dbFitness2Context context, JwtService jwtService, IConfiguration config,
    EmailService emailService)
        {
            _context = context;
            _jwtService = jwtService;
            _config = config;
            _emailService = emailService;
        }

        //登入
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] ULoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) ||
    string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest("請輸入帳號密碼");
            }

            var email = dto.Email.ToLower();
            var user = await _context.UUsers
    .FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower());

            if (user == null)
                return Unauthorized("帳號或密碼錯誤");

            //判斷是否為舊明文帳號
            if (user.PasswordSalt == null
    || user.PasswordSalt.Length == 0
    || user.PasswordSalt.All(b => b == 0))
            {
                // 舊明文驗證
                if (dto.Password != user.Password)
                    return Unauthorized("帳號或密碼錯誤");

                // 升級成加密版本
                using var newHmac = new HMACSHA512();

                user.PasswordSalt = newHmac.Key;
                user.Password = Convert.ToBase64String(
                    newHmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password))
                );

                // 直接視為已驗證（因為舊系統本來沒這概念）
                user.IsEmailVerified = true;
                user.EmailVerifiedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
            else
            {
                // 正常加密驗證流程
                using var hmac = new HMACSHA512(user.PasswordSalt);

                var computedHash = Convert.ToBase64String(
                    hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password))
                );

                if (!CryptographicOperations.FixedTimeEquals(
        Convert.FromBase64String(computedHash),
        Convert.FromBase64String(user.Password)))
                    return Unauthorized("帳號或密碼錯誤");
            }



            // 檢查是否完成 Email 驗證 bool
            if (!user.IsEmailVerified)
            {
                return Ok(new
                {
                    needVerify = true,
                    message = "請先完成電子郵件驗證"
                });
            }
            if (user.Password == null)
            {
                return BadRequest("此帳號請使用 LINE 登入，或先設定密碼");
            }
            var token = _jwtService.GenerateAccessToken(user);

            return Ok(new
            {
                token,
                userId = user.UserId,
                name = user.Name,
                isEmailVerified = user.IsEmailVerified,
                showWelcomeMessage = false
            });
        }
        //忘記密碼
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(UForgotPasswordDto dto)
        {
            var user = await _context.UUsers
                .FirstOrDefaultAsync(x => x.Email == dto.Email.ToLower());

            if (user == null)
                return Ok(); // 不暴露帳號存在與否

            var token = _jwtService.GenerateResetPasswordToken(user);
            var encodedToken = WebUtility.UrlEncode(token);

            var link = $"http://localhost:5173/reset-password?token={encodedToken}";

            await _emailService.SendResetPasswordEmail(user.Email, link);

            return Ok(new { message = "若帳號存在，已寄出重設信" });
        }


        //google登入
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] UGoogleLoginDto dto)
        {
            Console.WriteLine(dto == null ? "DTO 是 null" : "DTO 不為 null");
            Console.WriteLine("IdToken 是否為 null: " + (dto?.IdToken == null));
            GoogleJsonWebSignature.Payload payload;

            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new[]
     {
        _config["GoogleAuth:ClientId"]
    }
                };

                payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken, settings);
            }
            catch
            {
                return Unauthorized("Invalid Google token");
            }

            var userEmail = payload.Email.ToLower();
            var googleId = payload.Subject;

            var user = await _context.UUsers
                .FirstOrDefaultAsync(u => u.GoogleId == googleId || u.Email == userEmail);
            if (user != null && user.GoogleId == null)
            {
                user.GoogleId = googleId;
              
                user.IsEmailVerified = true;
                user.EmailVerifiedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
            if (user == null)
            {
                user = new UUser
                {
                    Name = payload.Name,
                    Email = userEmail,
                    GoogleId = googleId,
                    Account = userEmail,

                    IsEmailVerified = true,
                    EmailVerifiedAt = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow,

                    Address = "",
                    Phone = "",
                    Sex = "",
                    BirthDate = DateOnly.FromDateTime(DateTime.Today),
                    Status = 1
                };

                _context.UUsers.Add(user);
                await _context.SaveChangesAsync();
            }

            var token = _jwtService.GenerateAccessToken(user);

            return Ok(new
            {
                token,
                userId = user.UserId,
                name = user.Name,
                isEmailVerified = true,
                provider = "Google",
                showWelcomeMessage = true
            });
        }
        
        //line登入
        [HttpPost("line-login")]
        public async Task<IActionResult> LineLogin([FromBody] ULineLoginDto dto)
        {
            var client = new HttpClient();

            var values = new Dictionary<string, string>
    {
        { "grant_type", "authorization_code" },
        { "code", dto.Code },
        { "redirect_uri", _config["LineAuth:RedirectUri"] },
        { "client_id", _config["LineAuth:ClientId"] },
        { "client_secret", _config["LineAuth:ClientSecret"] }
    };

            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync("https://api.line.me/oauth2/v2.1/token", content);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return BadRequest($"LINE Token 交換失敗: {json}");

            var tokenData = JsonDocument.Parse(json);

            if (!tokenData.RootElement.TryGetProperty("id_token", out var idTokenElement))
                return BadRequest("沒有取得 id_token");

            var idToken = idTokenElement.GetString();

            if (string.IsNullOrEmpty(idToken))
            {
                return BadRequest("LINE id_token 為空");
            }

            JwtSecurityToken jwtToken;

            try
            {
            // 解析 id_token
                var handler = new JwtSecurityTokenHandler();
                jwtToken = handler.ReadJwtToken(idToken);
            }
            catch (Exception ex)
            {
                return BadRequest("LINE id_token 解析失敗: " + ex.Message);
            }

            

            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            var lineUserId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            var name = jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
            var picture = jwtToken.Claims.FirstOrDefault(c => c.Type == "picture")?.Value;
            if (string.IsNullOrEmpty(lineUserId))
                return Unauthorized("LINE 使用者資訊錯誤");

            // 1️ 先用 LineId 查
            var user = await _context.UUsers
                .FirstOrDefaultAsync(u => u.LineId == lineUserId);

            if (user != null)
            {
                if (user.LineId == null)
                {
                    user.LineId = lineUserId;
                    await _context.SaveChangesAsync();
                }
                var token = _jwtService.GenerateAccessToken(user);
                return Ok(new
                {
                    token,
                    userId = user.UserId,
                    name = user.Name,
                    provider = "LINE"
                });
            }

            // 2️ 如果沒有 Email → 要求補填
            if (string.IsNullOrEmpty(email))
            {
                email = $"line_{lineUserId}@temp.local";

                user = new UUser
                {
                    Account = $"line_{lineUserId}",
                    Name = name ?? "",
                    Email = email,
                    LineId = lineUserId,
                    IsEmailVerified = false,
                    CreatedDate = DateTime.UtcNow,
                    Status = 1,

                    Address = "",
                    Phone = "",
                    Sex = "",
                    BirthDate = DateOnly.FromDateTime(DateTime.Today)
                };

                _context.UUsers.Add(user);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    needEmail = true,
                    lineUserId = lineUserId,
                    name = name,
                    picture = picture
                });
            }

            if (!string.IsNullOrEmpty(email))
            {
                email = email.ToLower();
            }

            // 3️ 有 Email → 用 Email 查
            user = await _context.UUsers
    .FirstOrDefaultAsync(u => u.Email == email);



            if (user == null)
            {
                user = new UUser
                {
                    Account = $"line_{lineUserId}",
                    Name = name ?? "",
                    Email = email,
                    LineId = lineUserId,

                    IsEmailVerified = true,
                    EmailVerifiedAt = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow,
                    Status = 1,

                    Address = "",
                    Phone = "",
                    Sex = "",
                    BirthDate = DateOnly.FromDateTime(DateTime.Today)
                };

                _context.UUsers.Add(user);
                await _context.SaveChangesAsync();
            }

            var accessToken = _jwtService.GenerateAccessToken(user);

            return Ok(new
            {
                token = accessToken,
                userId = user.UserId,
                name = user.Name,
                provider = "LINE"
            });
        }

        //完成line會員註冊
        [HttpPost("complete-line-register")]
        public async Task<IActionResult> CompleteLineRegister(
     [FromBody] UCompleteLineRegisterRequest request)
        {
            if (string.IsNullOrEmpty(request.LineUserId) ||
                string.IsNullOrEmpty(request.Email))
            {
                return BadRequest("資料不完整");
            }

            var email = request.Email.ToLower();

            var user = await _context.UUsers
                .FirstOrDefaultAsync(x => x.LineId == request.LineUserId);

            if (user == null)
                return BadRequest("找不到 LINE 使用者");

            // 已完成註冊
            if (!user.Email.EndsWith("@temp.local"))
                return BadRequest("此 LINE 帳號已完成註冊");

            var emailExist = await _context.UUsers
                .AnyAsync(x => x.Email == email);

            if (emailExist)
                return BadRequest("Email 已被使用");

            user.Email = email;
            user.IsEmailVerified = true;
            user.EmailVerifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var token = _jwtService.GenerateAccessToken(user);

            return Ok(new
            {
                message = "註冊成功",
                token,
                userId = user.UserId,
                name = user.Name,
                provider = "LINE"
            });
        }
        //重設密碼 API
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(UResetPasswordDto dto)
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);

            try
            {
                var claims = handler.ValidateToken(dto.Token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = _config["Jwt:Issuer"],
                    ValidAudience = _config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                }, out _);

                if (claims.FindFirst("purpose")?.Value != "reset_password")
                    return BadRequest("Token 無效");

                var userId = int.Parse(claims.FindFirst("userId").Value);

                var user = await _context.UUsers.FindAsync(userId);

                if (user == null)
                    return BadRequest("使用者不存在");

                using var newHmac = new HMACSHA512();

                user.PasswordSalt = newHmac.Key;
                user.Password = Convert.ToBase64String(
                    newHmac.ComputeHash(Encoding.UTF8.GetBytes(dto.NewPassword))
                );

                await _context.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (SecurityTokenExpiredException)
            {
                return BadRequest("重設連結已過期");
            }
            catch
            {
                return BadRequest("重設連結無效");
            }
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

            var user = new UUser
            {
                // 帳號資料
                Account = dto.Email.ToLower(),
                Email = dto.Email.ToLower(),

                // 密碼
                Password = Convert.ToBase64String(
                    hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password))
                ),
                PasswordSalt = hmac.Key,

                // 預設值
                Name = "",                     // 先給空值，之後填寫
                Sex = "",
                BirthDate = DateOnly.FromDateTime(DateTime.Today),
                Phone = "",
                Address = "",
                Status = 0, // 未驗證
                CreatedDate = DateTime.UtcNow,
                IsEmailVerified = false,
                EmailVerifiedAt = null
            };

            _context.UUsers.Add(user);
            await _context.SaveChangesAsync();

            // 產生驗證 token
            var verifyToken = GenerateEmailVerifyToken(user);

            // 一定要 UrlEncode
            var encodedToken = WebUtility.UrlEncode(verifyToken);

            // 組驗證連結（用 encodedToken）
            var verifyLink = $"http://localhost:5173/users/verify-email?token={encodedToken}";

            // 寄信
            _ = Task.Run(() => _emailService.SendVerifyEmail(dto.Email, verifyLink));

            // 暫時改成回傳連結（測試用）
            return Ok(new
            {
                message = "註冊成功",
                verifyLink = verifyLink
            });


        }


        private string GenerateEmailVerifyToken(UUser user)
        {
            
            var claims = new[]
            {
        new Claim("userId", user.UserId.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim("purpose", "email_verify"),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"])
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        //登入驗證 token
        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            Console.WriteLine("====== 收到的 token ======");
            Console.WriteLine(token);
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);

            try
            {
                var claims = handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = _config["Jwt:Issuer"],
                    ValidAudience = _config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                }, out _);

                //  檢查用途
                var purpose = claims.FindFirst("purpose")?.Value;
                if (purpose != "email_verify")
                    return BadRequest("Token 類型錯誤");

                //  用 UserId 查詢
                var userId = claims.FindFirst("userId")?.Value;

                if (!int.TryParse(userId, out int id))
                    return BadRequest("Token 資料錯誤");

                var user = await _context.UUsers
                    .FirstOrDefaultAsync(x => x.UserId == id);

                if (user == null)
                    return BadRequest("使用者不存在");

                // 已驗證直接成功 + 發 token
                if (!user.IsEmailVerified)
                {
                    user.EmailVerifiedAt = DateTime.UtcNow;
                    user.Status = 1;
                    user.IsEmailVerified = true;

                    await _context.SaveChangesAsync();
                }

                var accessToken = _jwtService.GenerateAccessToken(user);

                return Ok(new
                {
                    success = true,
                    token = accessToken
                });

            }
            catch (SecurityTokenExpiredException)
            {
                return BadRequest("驗證連結已過期");
            }
            catch
            {
                return BadRequest("驗證連結無效");
            }
        }

        //取得並解析會員資料
        //[Authorize]
        //[HttpGet("me")]
        //public async Task<IActionResult> GetMe()
        //{
        //    var userId = int.Parse(User.FindFirst("userId").Value);

        //    var user = await _context.UUsers
        //        .Where(u => u.UserId == userId)
        //        .Select(u => new
        //        {
        //            u.UserId,
        //            u.Name,
        //            u.Email,
        //            u.Phone,
        //            u.Address
        //        })
        //        .FirstOrDefaultAsync();

        //    if (user == null)
        //        return NotFound();

        //    return Ok(user);
        //}
        
        //重發驗證信
                [HttpPost("resend-verify-email")]
        public async Task<IActionResult> ResendVerifyEmail([FromBody] UResendVerifyEmailDto dto)
        {
            var user = await _context.UUsers
                .FirstOrDefaultAsync(x => x.Email == dto.Email);

            // 不要告訴對方帳號是否存在（防止帳號枚舉）
            if (user == null)
                return Ok(new { success = true });

            if (user.IsEmailVerified)
            {
                return Ok(new
                {
                    success = true,
                    alreadyVerified = true,
                    message = "帳號已完成驗證"
                });
            }

            var verifyToken = GenerateEmailVerifyToken(user);
            var encodedToken = WebUtility.UrlEncode(verifyToken);

            var verifyLink = $"http://localhost:5173/users/verify-email?token={encodedToken}";

            await _emailService.SendVerifyEmail(user.Email, verifyLink);

            return Ok(new
            {
                success = true,
                message = "驗證信已重新寄出"
            });
        }
    }
}
