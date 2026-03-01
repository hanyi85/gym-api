using gym_api.Models;
using gym_api.Models.UDTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace gym_api.Controllers.user
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]   // 需要登入
    [Tags("會員健康管理")]
    public class HealthProfileController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public HealthProfileController(dbFitness2Context context)
        {
            _context = context;
        }


        // 取得自己的健康資料
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetCurrentUserId();

            var profile = await _context.UUserHealthProfiles
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (profile == null)
                return NotFound();

            decimal targetWeight = 0;

            if (!string.IsNullOrEmpty(profile.Goal))
                decimal.TryParse(profile.Goal, out targetWeight);

            return Ok(new
            {
                profile.Height,
                profile.Weight,
                profile.Bmi,
                TargetWeight = targetWeight
            });
        }

        // 新增或更新健康資料
        [HttpPost("profile")]
        public async Task<IActionResult> SaveProfile(UHealthProfileDto dto)
        {
            var userId = GetCurrentUserId(); // 你自己的方法

            var existing = await _context.UUserHealthProfiles
                .FirstOrDefaultAsync(x => x.UserId == userId);

            // 計算 BMI（統一用 decimal）
            var heightInMeter = dto.Height / 100m;
            var bmi = dto.Weight / (heightInMeter * heightInMeter);
            var roundedBmi = Math.Round(bmi, 1);

            if (existing == null)
            {
                var profile = new UUserHealthProfile
                {
                    UserId = userId,
                    Height = dto.Height,
                    Weight = dto.Weight,
                    Bmi = roundedBmi,
                    Goal = dto.TargetWeight.ToString(), // 存成字串
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.UUserHealthProfiles.Add(profile);
            }
            else
            {
                existing.Height = dto.Height;
                existing.Weight = dto.Weight;
                existing.Bmi = roundedBmi;
                existing.Goal = dto.TargetWeight.ToString();
                existing.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Profile saved successfully",
                Bmi = roundedBmi
            });
        }

        ////更新目標體重
        [HttpPut("target-weight")]
        public async Task<IActionResult> UpdateTargetWeight([FromBody]  UUpdateTargetWeightDto dto)
        {
            if (dto.TargetWeight < 30 || dto.TargetWeight > 200)
                return BadRequest("目標體重不合理");

            var userId = GetCurrentUserId();

            var profile = await _context.UUserHealthProfiles
    .FirstOrDefaultAsync(x => x.UserId == userId);

            if (profile == null)
            {
                profile = new UUserHealthProfile
                {
                    UserId = userId,
                    Goal = dto.TargetWeight.ToString(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.UUserHealthProfiles.Add(profile);
            }
            else
            {
                profile.Goal = dto.TargetWeight.ToString();
                profile.UpdatedAt = DateTime.UtcNow;
            }

            profile.Goal = dto.TargetWeight.ToString();
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "目標體重更新成功",
                TargetWeight = dto.TargetWeight
            });
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst("userId")
                 ?? User.FindFirst(ClaimTypes.NameIdentifier)
                 ?? User.FindFirst(JwtRegisteredClaimNames.Sub);

            if (claim == null)
                throw new UnauthorizedAccessException("UserId not found in token");

            return int.Parse(claim.Value);
        }

        private bool UUserHealthProfileExists(int id)
        {
            return _context.UUserHealthProfiles.Any(e => e.HealthProfileId == id);
        }
    }
}
