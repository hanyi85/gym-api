namespace gym_api.DTO
{
    public class SProductDTO
    {
        public int PId { get; set; }
        public string PName { get; set; }
        public decimal Price { get; set; }
        public string SpecName { get; set; }

        // 新增這個屬性：組合後的完整名稱
        public string FullName => string.IsNullOrEmpty(SpecName)
            ? PName
            : $"{PName} ({SpecName})";

        // 如果你想在後台算好原價，也可以加上這個
        public string OriginalPrice => (Price * 1.2m).ToString("0");
    }
}