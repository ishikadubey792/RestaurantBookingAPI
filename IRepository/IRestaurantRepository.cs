using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.IRepository
{
    public interface IRestaurantRepository
    {
        Task<List<Restaurant>> GetAllAsync();

        Task<Restaurant?> GetByIdAsync(int id);

        Task<Restaurant> AddAsync(Restaurant restaurant);

        Task<bool> UpdateAsync(Restaurant restaurant);

        Task<bool> DeleteAsync(int id);
    }
}