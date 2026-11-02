using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RestaurantBookingAPI.Models
{
    public class RestaurantOwnerRegisterRequest
    {
        // Owner account details

        [Required]
        [StringLength(100)]
        public string OwnerName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string OwnerEmail { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;


        // Restaurant details

        [Required]
        [StringLength(100)]
        public string RestaurantName { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(150)]
        public string? RestaurantEmail { get; set; }


        // Multiple cuisines

        [Required]
        [MinLength(1, ErrorMessage = "Select at least one cuisine.")]
        public List<int> CuisineIds { get; set; } = new();


        // One cover image during registration

        [Required]
        public IFormFile? CoverImage { get; set; }
    }
}