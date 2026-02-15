namespace gym_api.Models.CourseDTOs
{
    public class CourseScheduleDateDto
    {
        public string Date { get; set; }  
        public List<CourseScheduleSlotDto> Slots { get; set; }
    }
}
