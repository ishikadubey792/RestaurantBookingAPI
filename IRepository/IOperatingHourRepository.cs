using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.IRepository
{
    public interface IOperatingHourRepository
    {
        Task<List<OperatingHour>> GetAllAsync();

        Task<OperatingHour?> GetByIdAsync(int id);

        Task<List<OperatingHour>> GetByRestaurantIdAsync(int restaurantId);

        Task<OperatingHour> AddAsync(OperatingHour operatingHour);

        Task<bool> UpdateAsync(OperatingHour operatingHour);

        Task<bool> DeleteAsync(int id);
    }
}