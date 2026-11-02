using Microsoft.EntityFrameworkCore;
using RestaurantBookingAPI.Data;
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Repositories
{
    public class RestaurantOwnerRegistrationRepository
        : IRestaurantOwnerRegistrationRepository
    {
        private readonly AppDbContext _context;

        public RestaurantOwnerRegistrationRepository(
            AppDbContext context)
        {
            _context = context;
        }

        public async Task<Restaurant> RegisterAsync(
            User user,
            Restaurant restaurant,
            List<int> cuisineIds)
        {

            if (cuisineIds == null || cuisineIds.Count == 0)
            {
                throw new ArgumentException(
                    "At least one cuisine must be selected.");
            }

            if (cuisineIds.Distinct().Count() != cuisineIds.Count)
            {
                throw new ArgumentException(
                    "Duplicate cuisine IDs are not allowed.");
            }


            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Create owner account
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                // 2. Link restaurant with newly created owner
                restaurant.OwnerUserId = user.Id;

                await _context.Restaurants.AddAsync(restaurant);
                await _context.SaveChangesAsync();

                // 3. Create restaurant-cuisine mappings
                var mappings = cuisineIds.Select(cuisineId =>
                    new RestaurantCuisine
                    {
                        RestaurantId = restaurant.Id,
                        CuisineId = cuisineId
                    }).ToList();

                await _context.RestaurantCuisines.AddRangeAsync(mappings);
                await _context.SaveChangesAsync();

                // 4. Commit transaction
                await transaction.CommitAsync();

                return restaurant;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}