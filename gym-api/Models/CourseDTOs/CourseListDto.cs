namespace gym_api.Models.CourseDTOs
{
    public class CourseListDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Level { get; set; }
        public int Price { get; set; }
        public int Duration { get; set; }
        public string ImageUrl { get; set; }

        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
    }
}
