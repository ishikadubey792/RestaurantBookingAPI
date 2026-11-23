using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantBookingAPI.Models
{
    public class Restaurant
    {
        public int Id { get; set; }

        public int? OwnerUserId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string? City { get; set; }

        public string? Description { get; set; }

        public string Phone { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? CoverImagePath { get; set; }

        public bool IsActive { get; set; } = true;

        // Restaurant geographical coordinates
        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        // Explicit flag to indicate intentional coordinate update or clearing
        [NotMapped]
        public bool? UpdateCoordinates { get; set; }

        // Navigation properties
        public User? Owner { get; set; }

        // Stores all gallery images associated with this restaurant.
        public ICollection<RestaurantImage> Images { get; set; }
            = new List<RestaurantImage>();


        // Stores all menu images associated with this restaurant.
        public ICollection<MenuImage> MenuImages { get; set; }
            = new List<MenuImage>();

        // Stores all cuisine mappings associated with this restaurant.
        public ICollection<RestaurantCuisine> RestaurantCuisines { get; set; }
            = new List<RestaurantCuisine>();
        
    }
}