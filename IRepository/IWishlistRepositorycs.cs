using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.IRepository
{
    public interface IWishlistRepository
    {
        Task<Wishlist> AddAsync(Wishlist wishlist);

        Task<List<Wishlist>> GetByUserIdAsync(int userId);

        Task<Wishlist?> GetByUserAndRestaurantAsync(
            int userId,
            int restaurantId);

        Task<bool> RemoveAsync(
            int userId,
            int restaurantId);
    }
}
