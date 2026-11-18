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