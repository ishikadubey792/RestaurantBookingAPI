
using System.ComponentModel.DataAnnotations;

namespace RestaurantBookingAPI.Models
{
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(128, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;
    }
}
