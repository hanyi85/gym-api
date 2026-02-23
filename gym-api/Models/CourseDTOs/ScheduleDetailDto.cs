namespace gym_api.Models.CourseDTOs
{
    public class ScheduleDetailDto
    {
        public int ScheduleId { get; set; }
        public int CourseId { get; set; }
        public string CourseName { get; set; } = "";
        public int Price { get; set; }
        public string Date { get; set; } = "";
        public string Time { get; set; } = "";
        public string CoachName { get; set; } = "";
        public bool Full { get; set; }
        public bool CanEnroll { get; set; }
    }
}
