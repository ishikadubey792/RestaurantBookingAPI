using Microsoft.EntityFrameworkCore;
using RestaurantBookingAPI.Data;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Repositories
{
    public class OwnerDashboardRepository : IOwnerDashboardRepository
    {
        private readonly AppDbContext _context;

        public OwnerDashboardRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<OwnerDashboardMetrics> GetDashboardMetricsAsync(
            int ownerUserId,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? restaurantId = null)
        {
            // Retrieve all active/registered restaurants owned by this user
            var ownerRestaurants = await _context.Restaurants
                .AsNoTracking()
                .Where(r => r.OwnerUserId == ownerUserId)
                .ToListAsync();

            var totalOwnedRestaurants = ownerRestaurants.Count;

            var filteredRestaurants = restaurantId.HasValue
                ? ownerRestaurants.Where(r => r.Id == restaurantId.Value).ToList()
                : ownerRestaurants;

            var restaurantIds = filteredRestaurants.Select(r => r.Id).ToList();

            if (!restaurantIds.Any())
            {
                return new OwnerDashboardMetrics
                {
                    TotalRestaurants = totalOwnedRestaurants,
                    TotalBookings = 0,
                    PendingBookings = 0,
                    ConfirmedBookings = 0,
                    CancelledBookings = 0,
                    CompletedBookings = 0,
                    UpcomingBookings = 0,
                    RestaurantSummaries = new List<RestaurantBookingSummary>(),
                    PaymentSummaries = new List<PaymentStatusSummary>(),
                    TotalSuccessfulPaymentAmount = 0m
                };
            }

            // Query bookings linked to the owner's restaurants
            var bookingsQuery = from b in _context.Bookings.AsNoTracking()
                                join t in _context.RestaurantTables.AsNoTracking()
                                    on b.RestaurantTableId equals t.Id
                                where restaurantIds.Contains(t.RestaurantId)
                                select new
                                {
                                    b.Id,
                                    t.RestaurantId,
                                    b.BookingDate,
                                    b.Status
                                };

            if (fromDate.HasValue)
            {
                var fDate = fromDate.Value.Date;
                bookingsQuery = bookingsQuery.Where(x => x.BookingDate >= fDate);
            }

            if (toDate.HasValue)
            {
                var tDate = toDate.Value.Date;
                bookingsQuery = bookingsQuery.Where(x => x.BookingDate <= tDate);
            }

            var bookings = await bookingsQuery.ToListAsync();

            var today = DateTime.Today;

            var totalBookings = bookings.Count;
            var pendingBookings = bookings.Count(b => string.Equals(b.Status, "Pending", StringComparison.OrdinalIgnoreCase));
            var confirmedBookings = bookings.Count(b => string.Equals(b.Status, "Confirmed", StringComparison.OrdinalIgnoreCase));
            var cancelledBookings = bookings.Count(b => string.Equals(b.Status, "Cancelled", StringComparison.OrdinalIgnoreCase));
            var completedBookings = bookings.Count(b => string.Equals(b.Status, "Completed", StringComparison.OrdinalIgnoreCase));
            var upcomingBookings = bookings.Count(b => b.BookingDate.Date >= today && !string.Equals(b.Status, "Cancelled", StringComparison.OrdinalIgnoreCase));

            // Breakdown per restaurant
            var restaurantSummaries = filteredRestaurants.Select(r =>
            {
                var rBookings = bookings.Where(b => b.RestaurantId == r.Id).ToList();
                return new RestaurantBookingSummary
                {
                    RestaurantId = r.Id,
                    RestaurantName = r.Name,
                    TotalBookings = rBookings.Count,
                    PendingBookings = rBookings.Count(b => string.Equals(b.Status, "Pending", StringComparison.OrdinalIgnoreCase)),
                    ConfirmedBookings = rBookings.Count(b => string.Equals(b.Status, "Confirmed", StringComparison.OrdinalIgnoreCase)),
                    CancelledBookings = rBookings.Count(b => string.Equals(b.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)),
                    CompletedBookings = rBookings.Count(b => string.Equals(b.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                };
            }).OrderBy(s => s.RestaurantName).ToList();

            // Query payments for the matched bookings
            var bookingIds = bookings.Select(b => b.Id).ToList();
            var payments = bookingIds.Any()
                ? await _context.Payments
                    .AsNoTracking()
                    .Where(p => bookingIds.Contains(p.BookingId))
                    .Select(p => new { p.Status, p.Amount })
                    .ToListAsync()
                : new();

            var paymentSummaries = payments
                .GroupBy(p => p.Status ?? "Unknown")
                .Select(g => new PaymentStatusSummary
                {
                    Status = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .OrderBy(p => p.Status)
                .ToList();

            var totalSuccessfulPaymentAmount = payments
                .Where(p => string.Equals(p.Status, "Success", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(p.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                .Sum(p => p.Amount);

            return new OwnerDashboardMetrics
            {
                TotalRestaurants = totalOwnedRestaurants,
                TotalBookings = totalBookings,
                PendingBookings = pendingBookings,
                ConfirmedBookings = confirmedBookings,
                CancelledBookings = cancelledBookings,
                CompletedBookings = completedBookings,
                UpcomingBookings = upcomingBookings,
                RestaurantSummaries = restaurantSummaries,
                PaymentSummaries = paymentSummaries,
                TotalSuccessfulPaymentAmount = totalSuccessfulPaymentAmount
            };
        }
    }
}

