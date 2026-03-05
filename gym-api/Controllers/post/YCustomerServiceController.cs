using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gym_api.Models;
using Newtonsoft.Json;
using System.Net.Http;

namespace gym_api.Controllers.post
{
    [Route("api/[controller]")]
    [ApiController]
    public class YCustomerServiceController : ControllerBase
    {
        private readonly dbFitness2Context _context;
        // Google reCAPTCHA Secret Key
        private readonly string _reCaptchaSecret = "6LcNAHcsAAAAAAj_BM62lS9Ab6Kplzo0B8wPq1X_";

        public YCustomerServiceController(dbFitness2Context context)
        {
            _context = context;
        }

        /// <summary>
        /// 獲取所有啟用的問題類別
        /// </summary>
        [HttpGet("Categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.YQuestionCategories
                .Where(c => c.IsActive == true)
                .Select(c => new
                {
                    id = c.QuestionCategoryId,
                    name = c.Name
                })
                .ToListAsync();

            return Ok(categories);
        }

        /// <summary>
        /// 接收客戶回報表單 (含 reCAPTCHA 驗證)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Post([FromForm] CustomerServiceUploadDto uploadData)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("資料驗證失敗");
            }

            // 1. Google reCAPTCHA 驗證
            if (string.IsNullOrEmpty(uploadData.CaptchaToken))
            {
                return BadRequest(new { success = false, message = "請完成機器人驗證" });
            }

            using var client = new HttpClient();
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", _reCaptchaSecret),
                new KeyValuePair<string, string>("response", uploadData.CaptchaToken)
            });

            var response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
            var resultJson = await response.Content.ReadAsStringAsync();
            var captchaResult = JsonConvert.DeserializeObject<ReCaptchaInternalResult>(resultJson);

            if (captchaResult == null || !captchaResult.Success)
            {
                return BadRequest(new { success = false, message = "機器人驗證失敗，請重試" });
            }

            // 2. 執行資料存檔
            try
            {
                var newRecord = new YCustomerService
                {
                    Name = uploadData.Name,
                    Phone = uploadData.Phone,
                    Email = uploadData.Email,
                    QuestionCategoryId = uploadData.QuestionCategoryId,
                    Detail = uploadData.Detail,
                    Status = 0,
                    CreatedAt = DateTime.Now,
                    IsDeleted = false,
                    Sex = 0
                };

                if (uploadData.ImageFile != null && uploadData.ImageFile.Length > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        await uploadData.ImageFile.CopyToAsync(ms);
                        newRecord.Image = ms.ToArray();
                    }
                }

                _context.YCustomerServices.Add(newRecord);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "回報成功送出！" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"伺服器發生錯誤: {ex.Message}");
            }
        }

        // --- 巢狀類別：避免 CS0101 命名衝突 ---

        public class CustomerServiceUploadDto
        {
            public string Name { get; set; }
            public string Phone { get; set; }
            public string Email { get; set; }
            public int QuestionCategoryId { get; set; }
            public string Detail { get; set; }
            public string? CaptchaToken { get; set; } // 新增：接收 reCAPTCHA Token
            public IFormFile? ImageFile { get; set; }
        }

        private class ReCaptchaInternalResult
        {
            [JsonProperty("success")]
            public bool Success { get; set; }
            [JsonProperty("error-codes")]
            public List<string> ErrorCodes { get; set; }
        }
    }
}