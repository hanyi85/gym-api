namespace gym_api.Models.CourseDTOs
{
    public class CourseDetailDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string CoachName { get; set; } = string.Empty;

        public int Duration { get; set; }

        public decimal Price { get; set; }

        public string Description { get; set; } = string.Empty;
    }
}
