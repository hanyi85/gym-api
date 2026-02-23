namespace gym_api.Models.CourseDTOs
{
    public class ApplyDiscountRequestDto
    {
        public int ScheduleId { get; set; }
        public string Code { get; set; } = "";
    }
}
