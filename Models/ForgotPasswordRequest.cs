using System.ComponentModel.DataAnnotations;

namespace RestaurantBookingAPI.Models
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;
    }
}