namespace RestaurantBookingAPI.Models
{
    public class OwnerDashboardMetrics
    {
        public int TotalRestaurants { get; set; }
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int UpcomingBookings { get; set; }

        public List<RestaurantBookingSummary> RestaurantSummaries { get; set; } = new();
        public List<PaymentStatusSummary> PaymentSummaries { get; set; } = new();

        public decimal TotalSuccessfulPaymentAmount { get; set; }
        public string PaymentNote { get; set; } = "Payments are recorded in demo mode and do not represent actual settled bank funds.";
    }

    public class RestaurantBookingSummary
    {
        public int RestaurantId { get; set; }
        public string RestaurantName { get; set; } = string.Empty;
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public int CompletedBookings { get; set; }
    }

    public class PaymentStatusSummary
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
    }
}

