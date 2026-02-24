namespace gym_api.Models.CourseDTOs
{
    public class CreateCourseReviewDto
    {
        public int CourseBookingId { get; set; }
        public int UserId { get; set; } = 1; // 先假登入可不填也行（前端送）

        public int Rating { get; set; } // 1~5

        public int? TeachingQuality { get; set; }
        public int? EnvironmentScore { get; set; }
        public int? DifficultyScore { get; set; }
        public int? ValueScore { get; set; }

        public string? Comment { get; set; }

        // 快速標籤（TagId 清單）
        public List<int>? TagIds { get; set; }
    }
}
