using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.IServices
{
    public interface IRestaurantTableService
    {
        Task<List<RestaurantTable>> GetAllAsync();

        Task<RestaurantTable?> GetByIdAsync(int id);

        Task<List<RestaurantTable>> GetByRestaurantIdAsync(int restaurantId);

        Task<RestaurantTable> CreateAsync(RestaurantTable table);

        Task<bool> UpdateAsync(int id, RestaurantTable table);

        Task<bool> DeleteAsync(int id);
    }
}