using Microsoft.AspNetCore.Mvc;

namespace gym_api.Controllers.product
{
    [ApiController]
    [Route("api/[controller]")]
    [Tags("地址管理")]
    public class SAddressController : ControllerBase
    {
        // 暫時硬編碼的資料字典
        private static readonly Dictionary<string, List<string>> TaiwanData = new()
    {
        { "臺北市", new List<string> { "中正區", "大安區", "信義區", "中山區", "松山區" } },
        { "新北市", new List<string> { "板橋區", "新莊區", "中和區", "永和區", "淡水區" } },
        { "桃園市", new List<string> { "桃園區", "中壢區", "平鎮區", "八德區" } },
        { "臺中市", new List<string> { "西屯區", "北屯區", "南屯區", "一中商圈" } }
    };

        // GET: api/Address/cities
        [HttpGet("cities")]
        public IActionResult GetCities()
        {
            return Ok(TaiwanData.Keys.ToList());
        }

        // GET: api/Address/districts?cityName=臺北市
        [HttpGet("districts")]
        public IActionResult GetDistricts([FromQuery] string cityName)
        {
            if (TaiwanData.ContainsKey(cityName))
            {
                return Ok(TaiwanData[cityName]);
            }
            return NotFound("找不到該縣市");
        }
    }
}
