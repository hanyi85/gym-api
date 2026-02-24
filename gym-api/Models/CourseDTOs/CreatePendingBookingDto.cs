namespace gym_api.Models.CourseDTOs
{
    public class CreatePendingBookingDto
    {
        public int ScheduleId { get; set; }
        public int UserId { get; set; }
        public int FinalPrice { get; set; }
        public int DiscountAmount { get; set; } = 0;
        public int? DiscountId { get; set; }
    }
}
