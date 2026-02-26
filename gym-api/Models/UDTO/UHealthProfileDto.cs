using System.ComponentModel.DataAnnotations;

namespace gym_api.Models.UDTO
{
    public class UHealthProfileDto
    {
        [Required]
        [Range(100, 250)]
        public decimal Height { get; set; }   // cm

        [Required]
        [Range(30, 200)]
        public decimal Weight { get; set; }   // kg

        [Required]
        [Range(30, 200)]
        public decimal TargetWeight { get; set; }  // kg
    }
}
