
namespace RestaurantBookingAPI.Models
{
    public class Wishlist
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int RestaurantId { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;

        public Restaurant Restaurant { get; set; } = null!;
    }
}
