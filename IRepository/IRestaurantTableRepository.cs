using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.IRepository
{
    public interface IRestaurantTableRepository
    {
        Task<List<RestaurantTable>> GetAllAsync();

        Task<RestaurantTable?> GetByIdAsync(int id);

        Task<List<RestaurantTable>> GetByRestaurantIdAsync(int restaurantId);

        Task<RestaurantTable> AddAsync(RestaurantTable table);

        Task<bool> UpdateAsync(RestaurantTable table);

        Task<bool> DeleteAsync(int id);

        Task<List<RestaurantTable>> GetAvailableTablesAsync(
        DateTime bookingDate,
        TimeSpan startTime,
        TimeSpan endTime,
        int numberOfGuests);
    }
}