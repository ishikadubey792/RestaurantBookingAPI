
using Microsoft.EntityFrameworkCore;
using RestaurantBookingAPI.Data;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Repositories
{
    public class WishlistRepository : IWishlistRepository
    {
        private readonly AppDbContext _context;

        public WishlistRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Wishlist> AddAsync(Wishlist wishlist)
        {
            var results = await _context.Wishlists
                .FromSqlInterpolated($"""
                    EXEC dbo.AddToWishlist
                        @UserId = {wishlist.UserId},
                        @RestaurantId = {wishlist.RestaurantId}
                    """)
                .AsNoTracking()
                .ToListAsync();

            var result = results.FirstOrDefault();

            if (result == null)
            {
                throw new InvalidOperationException(
                    "Wishlist item could not be created or retrieved.");
            }

            return result;
        }

        public async Task<List<Wishlist>> GetByUserIdAsync(int userId)
        {
            return await _context.Wishlists
                .FromSqlInterpolated($"""
                    EXEC dbo.GetWishlistByUserId
                        @UserId = {userId}
                    """)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Wishlist?> GetByUserAndRestaurantAsync(
            int userId,
            int restaurantId)
        {
            var results = await _context.Wishlists
                .FromSqlInterpolated($"""
                    EXEC dbo.GetWishlistByUserAndRestaurant
                        @UserId = {userId},
                        @RestaurantId = {restaurantId}
                    """)
                .AsNoTracking()
                .ToListAsync();

            return results.FirstOrDefault();
        }

        public async Task<bool> RemoveAsync(
            int userId,
            int restaurantId)
        {
            var results = await _context.Database
                .SqlQueryRaw<int>(
                    "EXEC dbo.RemoveFromWishlist @UserId = {0}, @RestaurantId = {1}",
                    userId,
                    restaurantId)
                .ToListAsync();

            return results.FirstOrDefault() > 0;
        }
    }
}
