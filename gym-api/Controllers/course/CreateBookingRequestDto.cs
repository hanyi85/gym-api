namespace gym_api.Controllers.course
{
    public class CreateBookingRequestDto
    {
        public int ScheduleId { get; set; }
        public int UserId { get; set; }          // 先假登入用
        public string PaymentMethod { get; set; } = "信用卡";
        public int FinalPrice { get; set; }
        public int DiscountAmount { get; set; } = 0;
        public int? DiscountId { get; set; }
    }
}
