namespace gym_api.DTO
{
    public class SPaymentDTO
    {
        public string Payment { get; set; } = null!;
        public decimal HandlingFee { get; set; }
        public bool Activate { get; set; }
    }
}
