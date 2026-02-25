using gym_api.DTO; // 確保引用了你的 DTO 命名空間
using gym_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class SCartsController : ControllerBase
{
    private readonly dbFitness2Context _context;
    private readonly string _imageBaseUrl = "https://localhost:7218/";

    public SCartsController(dbFitness2Context context)
    {
        _context = context;
    }

    [HttpGet("User/{userId}")]
    public async Task<ActionResult<SCartPageResponseDTO>> GetCart(int userId)
    {
        var dbItems = await _context.SCarts
     .Where(c => c.UserId == userId)
     .Include(c => c.Spec)
         .ThenInclude(s => s.SImages)          
     .Include(c => c.Spec)
         .ThenInclude(s => s.PIdNavigation)
             .ThenInclude(p => p.SImages)       
     .ToListAsync();

        var itemList = dbItems.Select(c => {
            string? bestImage = null;

            bestImage = c.Spec.SImages
                .Where(img => img.ImageType == "Spec")
                .OrderBy(img => img.Sort)
                .Select(img => img.Picture)
                .FirstOrDefault();

            if (bestImage == null)
            {
                bestImage = c.Spec.PIdNavigation.SImages
                    .Where(img => img.ImageType == "Main")
                    .OrderBy(img => img.Sort)
                    .Select(img => img.Picture)
                    .FirstOrDefault();
            }

            bestImage ??= "images/default.png";

            bestImage = bestImage.Replace("\\", "/");

            return new SCartItemListDTO
            {
                CartId = c.CartId,
                SpecId = c.SpecId,
                Name = c.Spec.PIdNavigation.PName + (string.IsNullOrEmpty(c.Spec.SpecName) ? "" : $" - {c.Spec.SpecName}"),
                Price = c.Spec.DiscountPrice ?? c.Spec.Price, 
                OriginPrice = c.Spec.Price,
                Quantity = c.Quantity,
                Image = (bestImage ?? "images/default.png").Replace("\\", "/")
            };
        }).ToList();

        decimal subtotal = itemList.Sum(i => i.Price * i.Quantity);
        int shipping = (subtotal >= 899 || subtotal == 0) ? 0 : 80;

        return Ok(new SCartPageResponseDTO
        {
            Items = itemList,
            Subtotal = subtotal,
            ShippingFee = shipping,
            TotalAmount = subtotal + shipping
        });
    }

    [HttpPost("AddToCart")]
    public async Task<ActionResult> PostCart([FromBody] SCartsDTO dto)
    {
        var spec = await _context.SSpecifications.FindAsync(dto.SpecId);
        if (spec == null) return NotFound("找不到該商品規格");

        decimal finalPrice = spec.DiscountPrice ?? spec.Price;

        if (dto.IsAddon) dto.Quantity = 1;

        var existingItem = await _context.SCarts
            .FirstOrDefaultAsync(c => c.UserId == dto.UserId && c.SpecId == dto.SpecId);

        if (existingItem != null)
        {
            existingItem.Quantity += dto.Quantity;
            existingItem.TimeStamp = DateTime.Now;
        }
        else
        {
            var newItem = new SCart
            {
                UserId = dto.UserId,
                SpecId = dto.SpecId,
                Quantity = dto.Quantity,
                Price = finalPrice, 
                TimeStamp = DateTime.Now
            };
            _context.SCarts.Add(newItem);
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "已加入購物車" });
    }

    [HttpGet("Recommendations/{userId}")]
    public async Task<ActionResult<IEnumerable<ProductAddonDTO>>> GetRecommendations(int userId)
    {
        var cartSpecIds = await _context.SCarts
            .Where(c => c.UserId == userId)
            .Select(c => c.SpecId)
            .Distinct()
            .ToListAsync();

        var specs = await _context.SSpecifications
            .Where(s => !cartSpecIds.Contains(s.SpecId)) 
            .Include(s => s.PIdNavigation)
                .ThenInclude(p => p.SImages)
            .Include(s => s.SImages)
            .OrderBy(x => Guid.NewGuid())
            .Take(6)
            .ToListAsync();

        var result = specs.Select(s =>
        {
            var bestImage =
                s.SImages.OrderBy(i => i.Sort).Select(i => i.Picture).FirstOrDefault()
                ?? s.PIdNavigation.SImages.OrderBy(i => i.Sort).Select(i => i.Picture).FirstOrDefault()
                ?? "images/default.png";

            var addonPrice = s.DiscountPrice ?? s.Price;

            return new ProductAddonDTO
            {
                SpecId = s.SpecId,
                Name = s.PIdNavigation.PName +
                       (string.IsNullOrEmpty(s.SpecName) ? "" : $" - {s.SpecName}"),

                Price = s.Price,
                OriginPrice = s.Price,
                AddonPrice = addonPrice,

                Image = bestImage.Replace("\\", "/")
            };
        }).ToList();

        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateQuantity(int id, [FromBody] int quantity)
    {
        var cart = await _context.SCarts.FindAsync(id);
        if (cart == null) return NotFound();

        if (quantity <= 0) _context.SCarts.Remove(cart);
        else cart.Quantity = quantity;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var cart = await _context.SCarts.FindAsync(id);
        if (cart == null) return NotFound();

        _context.SCarts.Remove(cart);
        await _context.SaveChangesAsync();
        return Ok();
    }
}