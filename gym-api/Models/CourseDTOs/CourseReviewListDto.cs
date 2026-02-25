namespace gym_api.Models.CourseDTOs
{
    public class CourseReviewListDto
    {
    public double AvgRating { get; set; }

        public int Count { get; set; }

        public List<CourseReviewItemDto> Items { get; set; } = new();
    }
}