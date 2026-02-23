using System.ComponentModel.DataAnnotations;

namespace gym_api.Models
{
    public class UCompleteProfileDto
    {
        [Required]
        public string Name { get; set; }
        public string Sex { get; set; }
        public DateOnly BirthDate { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
    }
}
