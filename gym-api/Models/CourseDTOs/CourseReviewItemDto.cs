namespace gym_api.Models.CourseDTOs
{
    public class CourseReviewItemDto
    {
        public int ReviewId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = "匿名學員";
        public int CourseBookingId { get; set; }

        public int Rating { get; set; }
        public int? TeachingQuality { get; set; }
        public int? EnvironmentScore { get; set; }
        public int? DifficultyScore { get; set; }
        public int? ValueScore { get; set; }

        public string? Comment { get; set; }
        public DateTime ReviewTime { get; set; }

        public List<string> Tags { get; set; } = new();

        // 展示用：課名/教練/時間（從 booking join 出來）
        public string CourseName { get; set; } = "";
        public string CoachName { get; set; } = "";
        public DateTime StartTime { get; set; }
        public string BookingNo { get; set; } = ""; // BKxx
    }
}
