namespace gym_api.Models.CourseDTOs
{
    public class VenueDto
    {
        public int VenueId { get; set; }
        public string VenueName { get; set; }
        public string Address { get; set; }
        public int CityId { get; set; }
        public string CityName { get; set; }
    }
}
