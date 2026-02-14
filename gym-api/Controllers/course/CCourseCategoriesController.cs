using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using gym_api.Models;

namespace gym_api.Controllers.course
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("課程類別")]
    public class CCourseCategoriesController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public CCourseCategoriesController(dbFitness2Context context)
        {
            _context = context;
        }

        // GET: api/CCourseCategories
        // 課程列表 / 下拉選單用
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.CCourseCategories
     .Where(c => c.IsActive == true && c.IsDeleted == false)
     .Select(c => new
     {
         id = c.CategoryId,
         name = c.CategoryName
     })
     .ToListAsync();


            return Ok(categories);
        }

        // GET: api/CCourseCategories/5
        // 單筆
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _context.CCourseCategories
                .Where(c => c.CategoryId == id && !c.IsDeleted)
                .Select(c => new
                {
                    id = c.CategoryId,
                    name = c.CategoryName,
                    description = c.Description,
                    isActive = c.IsActive
                })
                .FirstOrDefaultAsync();

            if (category == null)
                return NotFound("找不到課程類別");

            return Ok(category);
        }
    }
}
