using Microsoft.EntityFrameworkCore;
using RestaurantBookingAPI.Data;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Repositories
{
    public class RestaurantRepository : IRestaurantRepository
    {
        private readonly AppDbContext _context;

        public RestaurantRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Restaurant>> GetAllAsync()
        {
            return await _context.Restaurants
            .FromSqlRaw("EXEC GetAllRestaurants")
            .ToListAsync();
        }

        public async Task<Restaurant?> GetByIdAsync(int id)
        {
            var restaurants = await _context.Restaurants
                .FromSqlInterpolated($"EXEC GetRestaurantById @Id = {id}")
                .ToListAsync();

            return restaurants.FirstOrDefault();
        }

        public async Task<Restaurant> AddAsync(Restaurant restaurant)
        {
            var restaurants = await _context.Restaurants
                .FromSqlInterpolated($"""
            EXEC CreateRestaurant
                @Name = {restaurant.Name},
                @Address = {restaurant.Address},
                @Phone = {restaurant.Phone},
                @Email = {restaurant.Email},
                @IsActive = {restaurant.IsActive}
            """)
                .ToListAsync();

            return restaurants.First();
        }

        public async Task<bool> UpdateAsync(Restaurant restaurant)
        {
            var restaurants = await _context.Restaurants
                .FromSqlInterpolated($"""
            EXEC UpdateRestaurant
                @Id = {restaurant.Id},
                @Name = {restaurant.Name},
                @Address = {restaurant.Address},
                @Phone = {restaurant.Phone},
                @Email = {restaurant.Email},
                @IsActive = {restaurant.IsActive}
            """)
                .ToListAsync();

            return restaurants.Any();
        }



        public async Task<bool> DeleteAsync(int id)
        {
            var result = await _context.Database
                .SqlQueryRaw<int>(
                    "EXEC dbo.DeleteRestaurant @Id = {0}", id)
                .ToListAsync();

            return result.FirstOrDefault() > 0;
        }


    }
}