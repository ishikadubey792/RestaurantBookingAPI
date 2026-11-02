namespace RestaurantBookingAPI.Models
{
    public class RestaurantImage
    {
        public int Id { get; set; }

        public int RestaurantId { get; set; }

        public string ImagePath { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public Restaurant? Restaurant { get; set; }
    }
}