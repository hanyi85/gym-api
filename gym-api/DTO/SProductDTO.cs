namespace gym_api.DTO
{
    public class SProductDTO
    {
        public int PId { get; set; }
        public string PName { get; set; }
        public decimal Price { get; set; }
        public string SpecName { get; set; }
        public string ImagePath { get; set; }

        public string CategoryName { get; set; }
        public string Description { get; set; }

        public string FullName => string.IsNullOrEmpty(SpecName)
            ? PName
            : $"{PName} ({SpecName})";

        public decimal? DiscountPrice { get; set; }

        public List<SCommentDTO> Comments { get; set; } = new List<SCommentDTO>();
        public double AverageStar { get; set; } 
        public int TotalComments { get; set; }
    }
}