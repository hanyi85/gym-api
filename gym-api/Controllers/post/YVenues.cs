using gym_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gym_api.Models;
namespace gym_api.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class VenuesController : ControllerBase
    {
        private readonly dbFitness2Context _context; // 請更換成你的 DbContext 名稱

        public VenuesController(dbFitness2Context context)
        {
            _context = context;
        }

        // GET: api/Venues
        [HttpGet]
        public async Task<IActionResult> GetVenues()
        {
            // JOIN cVenues 和 cCity
            var venues = await _context.CVenues
                .Where(v => v.IsDeleted == false)
                .Join(_context.CCities,
                    v => v.CityId,
                    c => c.CityId,
                    (v, c) => new {
                        id = v.VenueId,
                        name = v.VenueName,
                        city = c.CityName,
                        address = v.Address,
                        description = v.Description,
                        // 以下為資料庫目前缺少的欄位，先給予預設值
                        phone = "02-2345-6789",
                        hours = "06:00 - 24:00",
                        parking = "會員免費停車",
                        nearbyParking = "鄰近特約停車場",
                        image = "https://images.unsplash.com/photo-1534438327276-14e5300c3a48?q=80&w=800",
                        mapUrl = $"https://www.google.com/maps?q={v.Address}&output=embed"
                    })
                .ToListAsync();

            return Ok(venues);
        }
    }
}