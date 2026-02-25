namespace gym_api.Models.UDTO
{
    public class UResetPasswordDto
    {
        public string Token { get; set; }
        public string NewPassword { get; set; }
    }
}
