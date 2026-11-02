namespace RestaurantBookingAPI.Models
{
    public class RestaurantCuisine
    {
        public int Id { get; set; }

        public int RestaurantId { get; set; }

        public int CuisineId { get; set; }

        public Restaurant? Restaurant { get; set; }

        public Cuisine? Cuisine { get; set; }
    }
}