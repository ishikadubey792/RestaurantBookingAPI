using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.IRepository
{
    public interface IRestaurantRepository
    {
        Task<List<Restaurant>> GetAllAsync();

        Task<Restaurant?> GetByIdAsync(int id);

        Task<Restaurant> AddAsync(Restaurant restaurant);

        Task<bool> UpdateAsync(Restaurant restaurant);

        // Marks a restaurant as inactive without deleting its related records.
        Task<bool> SoftDeleteAsync(int id, int ownerUserId);


        // Marks a restaurant as inactive without owner-specific filtering.
        Task<bool> SoftDeleteAsync(int id);

        Task<List<Restaurant>> GetByOwnerIdAsync(int ownerUserId);

        Task<Restaurant?> GetByIdAndOwnerIdAsync(
            int restaurantId,
            int ownerUserId
        );
    }
}