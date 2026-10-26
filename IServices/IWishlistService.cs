
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.IServices
{
    public interface IWishlistService
    {
        Task AddToWishlistAsync(int userId, int restaurantId);

        Task<List<Wishlist>> GetMyWishlistAsync(int userId);

        Task RemoveFromWishlistAsync(int userId, int restaurantId);
    }
}
