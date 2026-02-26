namespace gym_api.Models.CourseDTOs
{
    public class CoachListItemDto
    {
        public int CoachId { get; set; }
        public string Name { get; set; } = "";
        public int VenueId { get; set; }
        public string? VenueName { get; set; }
        public string? Description { get; set; }
        public decimal HourlyRate { get; set; }
        public List<string> Skills { get; set; } = new();
        public string? ImageUrl { get; set; } // 先預留
    }
}
