namespace gym_api.Models.CourseDTOs
{
    public class CourseScheduleSlotDto
    {
        public int ScheduleId { get; set; }
        public string Time { get; set; }          
        public bool Full { get; set; }
        public bool CanEnroll { get; set; }
    }
}
