namespace gym_api.Models.CourseDTOs
{
    public class BookingHistoryItemDto
    {
        public int CourseBookingId { get; set; }
        public int ScheduleId { get; set; }

        public string CourseName { get; set; } = "";
        public string CoachName { get; set; } = "";

        public DateTime StartTime { get; set; }

        public string Status { get; set; } = "";          // 你的 CCourseBooking.Status
        public string PaymentStatus { get; set; } = "";   // 付款狀態

        public int FinalPrice { get; set; }
        public DateTime BookingTime { get; set; }

        public bool IsReviewed { get; set; }
    }
}
