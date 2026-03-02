namespace gym_api.Models.CourseDTOs
{
    public class BookingPaymentSummaryDto
    {
        public int BookingId { get; set; }
        public int ScheduleId { get; set; }
        public int CourseId { get; set; }

        public string CourseName { get; set; } = "";
        public DateTime StartTime { get; set; }
        public string Date { get; set; } = "";
        public string Time { get; set; } = "";

        public string CoachName { get; set; } = "";

        public decimal OriginPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalPrice { get; set; }

        public string PaymentStatus { get; set; } = "";
    }
}
