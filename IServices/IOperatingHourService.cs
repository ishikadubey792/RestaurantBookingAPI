
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.IServices
{
    public interface IOperatingHourService
    {
        Task<List<OperatingHour>> GetAllAsync();

        Task<OperatingHour?> GetByIdAsync(int id);

        Task<List<OperatingHour>> GetByRestaurantIdAsync(
            int restaurantId);

        Task<OperatingHour> CreateAsync(
            OperatingHour operatingHour,
            int ownerUserId);

        Task<bool> UpdateAsync(
            int id,
            OperatingHour operatingHour,
            int ownerUserId);

        Task<bool> DeleteAsync(
            int id,
            int ownerUserId);
    }
}
