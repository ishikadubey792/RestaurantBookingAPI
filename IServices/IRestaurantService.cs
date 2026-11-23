using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.IServices
{
    public interface IRestaurantService
    {
        Task<List<Restaurant>> GetAllAsync();

        Task<Restaurant?> GetByIdAsync(int id);

        Task<Restaurant> CreateAsync(Restaurant restaurant);

        Task<bool> UpdateAsync(int id, Restaurant restaurant, bool? updateCoordinates = null);

        Task<bool> DeleteAsync(int id);


        Task<Restaurant> CreateMyRestaurantAsync(
            Restaurant restaurant,
            int ownerUserId
        );

        Task<List<Restaurant>> GetMyRestaurantsAsync(int ownerUserId);

        Task<Restaurant?> GetMyRestaurantByIdAsync(
            int restaurantId,
            int ownerUserId
        );

        Task<bool> UpdateMyRestaurantAsync(
            int restaurantId,
            int ownerUserId,
            Restaurant restaurant,
            bool? updateCoordinates = null
        );

        Task<bool> DeleteMyRestaurantAsync(
            int restaurantId,
            int ownerUserId
        );
    }
}