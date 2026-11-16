using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.IRepository
{
    public interface IOwnerDashboardRepository
    {
        Task<OwnerDashboardMetrics> GetDashboardMetricsAsync(
            int ownerUserId,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? restaurantId = null);
    }
}

