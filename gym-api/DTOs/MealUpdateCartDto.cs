namespace gym_api.DTOs
{
    public class MealUpdateCartDto
    {
        public int OrderId { get; set; }
        public List<MealUpdateCartItemDto> Items { get; set; }
    }
}
