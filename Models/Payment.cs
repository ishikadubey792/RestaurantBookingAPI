
namespace RestaurantBookingAPI.Models
{
    public class Payment
    {
        public int Id { get; set; }

        public int BookingId { get; set; }

        public decimal Amount { get; set; }

        public string PaymentMethod { get; set; } = "Demo";

        public string Status { get; set; } = "Pending";

        public string? TransactionId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? PaidAt { get; set; }

        // Navigation property
        public Booking Booking { get; set; } = null!;
    }
}
