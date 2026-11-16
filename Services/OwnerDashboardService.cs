using RestaurantBookingAPI.Exceptions;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.IServices;
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Services
{
    public class OwnerDashboardService : IOwnerDashboardService
    {
        private readonly IOwnerDashboardRepository _dashboardRepository;
        private readonly IRestaurantRepository _restaurantRepository;

        public OwnerDashboardService(
            IOwnerDashboardRepository dashboardRepository,
            IRestaurantRepository restaurantRepository)
        {
            _dashboardRepository = dashboardRepository;
            _restaurantRepository = restaurantRepository;
        }

        public async Task<OwnerDashboardMetrics> GetDashboardMetricsAsync(
            int ownerUserId,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? restaurantId = null)
        {
            if (restaurantId.HasValue)
            {
                var restaurant = await _restaurantRepository
                    .GetByIdAndOwnerIdAsync(restaurantId.Value, ownerUserId);

                if (restaurant == null)
                {
                    throw new BadRequestException(
                        "Restaurant not found or you do not have permission to access its analytics.");
                }
            }

            return await _dashboardRepository.GetDashboardMetricsAsync(
                ownerUserId,
                fromDate,
                toDate,
                restaurantId);
        }
    }
}

