namespace RestaurantBookingAPI.Models
{
    public class Booking
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }

        public int RestaurantTableId { get; set; }

        public DateTime BookingDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public int NumberOfGuests { get; set; }

        public string Status { get; set; }

        public string? SpecialRequest { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}