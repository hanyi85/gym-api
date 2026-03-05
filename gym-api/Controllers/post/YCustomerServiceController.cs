using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gym_api.Models; // 請確保指向你的 DbContext 所在的命名空間

namespace gym_api.Controllers.post
{
    [Route("api/[controller]")]
    [ApiController] // 標註為 Web API 模式
    public class YCustomerServiceController : ControllerBase
    {
        private readonly dbFitness2Context _context; // 請將 YourDbContext 改成你實際的 DbContext 名稱

        public YCustomerServiceController(dbFitness2Context context)
        {
            _context = context;
        }

        /// <summary>
        /// 獲取所有啟用的問題類別 (用於前端下拉選單)
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
        /// 接收前端傳回的客戶回報表單
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Post([FromForm] CustomerServiceUploadDto uploadData)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("資料驗證失敗");
            }

            try
            {
                var newRecord = new YCustomerService
                {
                    Name = uploadData.Name,
                    Phone = uploadData.Phone,
                    Email = uploadData.Email,
                    QuestionCategoryId = uploadData.QuestionCategoryId,
                    Detail = uploadData.Detail,
                    Status = 0,               // 預設 0: 待處理
                    CreatedAt = DateTime.Now, // 對應 SQL getdate()
                    IsDeleted = false,        // 預設未刪除
                    Sex = 0                   // 如果前端沒傳，預設給 0 或可加欄位接收
                };

                // 處理圖片轉換：將前端上傳的檔案轉為 byte[] 存入資料庫 image 欄位
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
                // 這裡可以記錄錯誤日誌 (Logger)
                return StatusCode(500, $"伺服器發生錯誤: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 定義接收資料的 DTO (Data Transfer Object)
    /// </summary>
    public class CustomerServiceUploadDto
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public int QuestionCategoryId { get; set; }
        public string Detail { get; set; }
        public IFormFile? ImageFile { get; set; } // 接收圖片檔案
    }
}
