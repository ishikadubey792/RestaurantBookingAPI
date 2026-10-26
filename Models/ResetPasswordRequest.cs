using System.ComponentModel.DataAnnotations;

namespace RestaurantBookingAPI.Models
{
    public class ResetPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must contain exactly 6 digits.")]
        public string Otp { get; set; } = string.Empty;

        [Required]
        [StringLength(128, MinimumLength = 8)]
        [RegularExpression(
            @"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[^A-Za-z0-9\s])\S{8,128}$",
            ErrorMessage = "Password must contain uppercase, lowercase, a number, and a special character. Spaces are not allowed."
        )]
        public string NewPassword { get; set; } = string.Empty;
    }
}