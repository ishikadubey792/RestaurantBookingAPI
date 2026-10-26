namespace RestaurantBookingAPI.Models
{
    public class RestaurantTable
    {
        public int Id { get; set; }

        public int RestaurantId { get; set; }

        public int TableNumber { get; set; }

        public int Capacity { get; set; }

        public string Status { get; set; }
    }
}