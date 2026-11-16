using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.IServices
{
    public interface IOwnerDashboardService
    {
        Task<OwnerDashboardMetrics> GetDashboardMetricsAsync(
            int ownerUserId,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? restaurantId = null);
    }
}

