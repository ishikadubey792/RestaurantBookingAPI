using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.IServices
{
    public interface IRestaurantService
    {
        Task<List<Restaurant>> GetAllAsync();

        Task<Restaurant?> GetByIdAsync(int id);

        Task<Restaurant> CreateAsync(Restaurant restaurant);

        Task<bool> UpdateAsync(int id, Restaurant restaurant);

        Task<bool> DeleteAsync(int id);
    }
}