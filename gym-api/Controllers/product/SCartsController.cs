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
            .ThenInclude(s => s.PIdNavigation)
            .ToListAsync();

        var itemList = dbItems.Select(c => {
            var bestImage = c.Spec.SImages
                .OrderBy(img => img.ImageType == "Spec" ? 1 : (img.ImageType == "Main" ? 2 : 3))
                .ThenBy(img => img.Sort)
                .Select(img => img.Picture) 
                .FirstOrDefault();

            return new SCartItemListDTO
            {
                CartId = c.CartId,
                SpecId = c.SpecId,
                Name = c.Spec.PIdNavigation.PName + (string.IsNullOrEmpty(c.Spec.SpecName) ? "" : $" - {c.Spec.SpecName}"),
                Price = c.Spec.DiscountPrice ?? c.Spec.Price, 
                OriginPrice = c.Spec.Price,
                Quantity = c.Quantity,
                Image = _imageBaseUrl + (bestImage ?? "images/default.png").Replace("\\", "/")
            };
        }).ToList();

        // 計算小計
        decimal subtotal = itemList.Sum(i => i.Price * i.Quantity);
        // 運費邏輯：滿 899 免運，否則 80 (購物車為空時為 0)
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

        var existingItem = await _context.SCarts
            .FirstOrDefaultAsync(c => c.UserId == dto.UserId && c.SpecId == dto.SpecId);

        if (existingItem != null)
        {
            existingItem.Quantity += dto.Quantity;
        }
        else
        {
            var newItem = new SCart
            {
                UserId = dto.UserId,
                SpecId = dto.SpecId,
                Quantity = dto.Quantity,
                Price = spec.DiscountPrice ?? spec.Price 
            };
            _context.SCarts.Add(newItem);
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "已加入購物車" });
    }

    [HttpGet("Recommendations")]
    public async Task<ActionResult<IEnumerable<ProductAddonDTO>>> GetRecommendations()
    {
        var products = await _context.SSpecifications
            .Include(s => s.PIdNavigation)
            .Include(s => s.SImages) 
            .OrderBy(x => Guid.NewGuid()) 
            .Take(6)
            .ToListAsync();

        var result = products.Select(s => {
            var bestImage = s.SImages
                .OrderBy(img => img.ImageType == "Spec" ? 1 : (img.ImageType == "Main" ? 2 : 3))
                .ThenBy(img => img.Sort)
                .Select(img => img.Picture) 
                .FirstOrDefault();

            return new ProductAddonDTO
            {
                SpecId = s.SpecId,
                Name = s.PIdNavigation.PName,
                Price = s.DiscountPrice ?? s.Price,
                OriginPrice = s.Price,
                Image = _imageBaseUrl + (bestImage ?? "images/default.png").Replace("\\", "/")
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