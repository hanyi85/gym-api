using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gym_api.Models;
using gym_api.DTO;

namespace gym_api.Controllers.product
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("商品管理")]
    public class SProductsController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public SProductsController(dbFitness2Context context)
        {
            _context = context;
        }

        // GET: api/SProducts
        [HttpGet]
        public async Task<IEnumerable<SProductDTO>> GetSProducts()
        {
            return await _context.SProducts
                .SelectMany(p => p.SSpecifications.Select(s => new SProductDTO
                {
                    PId = s.SpecId,
                    PName = p.PName,
                    Price = s.Price,
                    DiscountPrice = s.DiscountPrice,
                    SpecName = s.SpecName,
                    ImagePath = p.SImages
                    .OrderByDescending(img => img.SpecId == s.SpecId)
                    .ThenByDescending(img => img.MainPicture && img.SpecId == null)
                    .ThenByDescending(img => img.PicId)
                    .Select(img => img.Picture)
                    .FirstOrDefault() ?? "/images/products/default.jpg"
                }))
                .ToListAsync();
        }

        // GET: api/SProducts/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSProduct(int id)
        {
            var spec = await _context.SSpecifications
                .Include(s => s.PIdNavigation)
                    .ThenInclude(p => p.SImages)
                .Include(s => s.PIdNavigation)
                    .ThenInclude(p => p.CIdNavigation)
                .FirstOrDefaultAsync(s => s.SpecId == id);

            if (spec == null) return NotFound();

            var product = spec.PIdNavigation;

            var commentList = await _context.SComments
                .Include(c => c.Od)
                .Where(c => c.Od.SpecId == id)
                .ToListAsync();

            var result = new SProductDTO
            {
                PId = spec.SpecId,
                PName = product.PName,
                Price = spec.Price,
                DiscountPrice = spec.DiscountPrice,
                SpecName = spec.SpecName,
                CategoryName = product.CIdNavigation?.CName ?? "未分類",
                Description = product.Description,

                AverageStar = commentList.Any() ? Math.Round(commentList.Average(c => (double)c.CommentStar), 1) : 0,
                TotalComments = commentList.Count,

                Comments = commentList.Select(c => new SCommentDTO
                {
                    ComId = c.ComId,
                    UserId = c.UserId,
                    CommentStar = (int)c.CommentStar,
                    ProductComment = c.Productcomment, 
                    CommentTime = c.CommentTime.ToString("yyyy-MM-dd HH:mm")
                }).ToList(),

                ImageList = product.SImages
             .OrderByDescending(img => img.SpecId == spec.SpecId) 
             .ThenByDescending(img => img.MainPicture)          
             .Select(img => img.Picture)
             .ToList(),

                ImagePath = product.SImages
                    .OrderByDescending(img => img.SpecId == spec.SpecId)
                    .ThenByDescending(img => img.MainPicture)
                    .Select(img => img.Picture)
                    .FirstOrDefault() ?? "/images/products/default.jpg"
            };

            return Ok(result);
        }

        // POST: api/SProducts
        [HttpPost]
        public async Task<ActionResult<SProduct>> PostSProduct(SProduct sProduct)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.SProducts.Add(sProduct);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSProduct), new { id = sProduct.PId }, sProduct);
        }

        // PUT: api/SProducts/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSProduct(int id, SProduct sProduct)
        {
            if (id != sProduct.PId)
            {
                return BadRequest();
            }

            _context.Entry(sProduct).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SProductExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/SProducts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSProduct(int id)
        {
            var sProduct = await _context.SProducts.FindAsync(id);
            if (sProduct == null)
            {
                return NotFound();
            }

            _context.SProducts.Remove(sProduct);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SProductExists(int id)
        {
            return _context.SProducts.Any(e => e.PId == id);
        }
    }
}