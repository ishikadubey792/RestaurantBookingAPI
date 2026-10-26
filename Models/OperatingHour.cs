namespace RestaurantBookingAPI.Models
{
    public class OperatingHour
    {
        public int Id { get; set; }

        public int RestaurantId { get; set; }

        public string DayOfWeek { get; set; }

        public TimeSpan? OpenTime { get; set; }

        public TimeSpan? CloseTime { get; set; }

        public bool IsClosed { get; set; }
    }
}