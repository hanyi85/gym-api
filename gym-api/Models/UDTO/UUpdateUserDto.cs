namespace gym_api.Models.UDTO
{
    public class UUpdateUserDto
    {
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Sex { get; set; }
        public string? Address { get; set; }
        public DateOnly? BirthDate { get; set; }

        //public IFormFile? Image { get; set; }
    }
}
