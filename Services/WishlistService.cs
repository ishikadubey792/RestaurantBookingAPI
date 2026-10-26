using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.IServices;
using RestaurantBookingAPI.Models;
using RestaurantBookingAPI.Repositories;

namespace RestaurantBookingAPI.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly IWishlistRepository _wishlistRepository;
        private readonly IRestaurantRepository _restaurantRepository;

        public WishlistService()
        {
        }

        public WishlistService(
            IWishlistRepository wishlistRepository,
            IRestaurantRepository restaurantRepository)
        {
            _wishlistRepository = wishlistRepository;
            _restaurantRepository = restaurantRepository;
        }

        public async Task AddToWishlistAsync(
            int userId,
            int restaurantId)
        {
            // Check whether the restaurant exists
            var restaurant = await _restaurantRepository
                .GetByIdAsync(restaurantId);

            if (restaurant == null)
            {
                throw new KeyNotFoundException(
                    "Restaurant not found.");
            }

            // Check whether restaurant is already in wishlist
            var existingWishlist =
                await _wishlistRepository.GetByUserAndRestaurantAsync(
                    userId, restaurantId);

            if (existingWishlist != null)
            {
                throw new InvalidOperationException(
                    "Restaurant is already in your wishlist.");
            }

            // Create wishlist entry
            var wishlist = new Wishlist
            {
                UserId = userId,
                RestaurantId = restaurantId,
                CreatedAt = DateTime.UtcNow.AddHours(5).AddMinutes(30)
            };

            await _wishlistRepository.AddAsync(wishlist);
        }

        public async Task<List<Wishlist>> GetMyWishlistAsync(int userId)
        {
            return await _wishlistRepository.GetByUserIdAsync(userId);
        }

        public async Task RemoveFromWishlistAsync(
            int userId,
            int restaurantId)
        {
            var removed = await _wishlistRepository.RemoveAsync(
                userId, restaurantId);

            if (!removed)
            {
                throw new KeyNotFoundException(
                    "Restaurant was not found in your wishlist.");
            }
        }
    }
}
