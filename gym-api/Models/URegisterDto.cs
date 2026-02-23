using System.ComponentModel.DataAnnotations;

namespace gym_api.Models
{
    public class RegisterDto
    {

        public string Email { get; set; }


        public string Password { get; set; }
    }
}
