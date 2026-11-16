namespace RestaurantBookingAPI.Models
{
    public class OwnerRestaurantBooking
    {
        public int BookingId { get; set; }

        public int RestaurantId { get; set; }
        public string RestaurantName { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;

        public DateTime BookingDate { get; set; }

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public int NumberOfGuests { get; set; }

        public string Status { get; set; } = string.Empty;
        public string? SpecialRequest { get; set; }

        public DateTime CreatedAt { get; set; }

        // Associated Table details
        public int? RestaurantTableId { get; set; }
        public int? TableNumber { get; set; }

        // Associated Payment details (demo mode)
        public int? PaymentId { get; set; }
        public decimal? PaymentAmount { get; set; }
        public string? PaymentStatus { get; set; }
        public string? PaymentMethod { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}
