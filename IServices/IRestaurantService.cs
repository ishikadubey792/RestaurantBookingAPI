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


        Task<List<Restaurant>> GetMyRestaurantsAsync(int ownerUserId);

        Task<Restaurant?> GetMyRestaurantByIdAsync(
            int restaurantId,
            int ownerUserId
        );

        Task<bool> UpdateMyRestaurantAsync(
            int restaurantId,
            int ownerUserId,
            Restaurant restaurant
        );

        Task<bool> DeleteMyRestaurantAsync(
            int restaurantId,
            int ownerUserId
        );
    }
}