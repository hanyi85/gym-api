namespace gym_api.Models.UDTO
{
    public class UChangePasswordDto
    {
        public string? OldPassword { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmNewPassword { get; set; }
        // Additional validation can be added here if needed
        // For example, you might want to ensure that NewPassword and ConfirmNewPassword match
    }
}
