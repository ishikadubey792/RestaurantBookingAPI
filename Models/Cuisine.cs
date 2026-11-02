namespace RestaurantBookingAPI.Models
{
    public class Cuisine
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public ICollection<RestaurantCuisine> RestaurantCuisines { get; set; }
            = new List<RestaurantCuisine>();
    }
}