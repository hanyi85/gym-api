namespace gym_api.DTOs
{
    public class MealAddToCartDto
    {
        public int FUserId { get; set; }
        public int FMealId { get; set; }
        public DateOnly FPickDate { get; set; }
        public int FPickTimeId { get; set; }
        public int FQty { get; set; }
    }
}
