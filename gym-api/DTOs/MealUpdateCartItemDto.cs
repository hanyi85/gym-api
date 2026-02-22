namespace gym_api.DTOs
{
    public class MealUpdateCartItemDto
    {
        public int OrderItemId { get; set; }
        public DateOnly PickDate { get; set; }
        public int PickTimeId { get; set; }
        public int Qty { get; set; }
    }
}
