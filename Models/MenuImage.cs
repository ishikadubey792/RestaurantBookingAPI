
namespace RestaurantBookingAPI.Models
{
    public class MenuImage
    {
        // Unique identifier for each menu image.
        public int Id { get; set; }

        // Identifies the restaurant to which this menu image belongs.
        public int RestaurantId { get; set; }

        // Stores the relative URL of the uploaded menu image.
        public string ImagePath { get; set; } = string.Empty;

        // Defines the display sequence of menu images for customers.
        public int DisplayOrder { get; set; }

        // Records when the menu image was uploaded.
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property representing the associated restaurant.
        public Restaurant? Restaurant { get; set; }
    }
}
