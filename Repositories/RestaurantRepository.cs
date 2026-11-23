using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RestaurantBookingAPI.Data;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.Models;
using System.Data;

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
                .FromSqlInterpolated(
                    $"EXEC GetRestaurantById @Id = {id}")
                .ToListAsync();

            return restaurants.FirstOrDefault();
        }


        public async Task<Restaurant> AddAsync(Restaurant restaurant)
        {
            var latitudeParameter = new SqlParameter("@Latitude", SqlDbType.Decimal)
            {
                Precision = 9,
                Scale = 6,
                Value = (object?)restaurant.Latitude ?? DBNull.Value
            };

            var longitudeParameter = new SqlParameter("@Longitude", SqlDbType.Decimal)
            {
                Precision = 9,
                Scale = 6,
                Value = (object?)restaurant.Longitude ?? DBNull.Value
            };

            var restaurants = await _context.Restaurants
                .FromSqlInterpolated($"""
            EXEC CreateRestaurant
                @Name={restaurant.Name},
                @Address={restaurant.Address},
                @Phone={restaurant.Phone},
                @Email={restaurant.Email},
                @IsActive={restaurant.IsActive},
                @OwnerUserId={restaurant.OwnerUserId},
                @City={restaurant.City},
                @Description={restaurant.Description},
                @CoverImagePath={restaurant.CoverImagePath},
                @Latitude={latitudeParameter},
                @Longitude={longitudeParameter}
            """)
                .ToListAsync();

            return restaurants.First();
        }



        public async Task<bool> UpdateAsync(
            Restaurant restaurant,
            bool updateCoordinates = false)
        {
            var latitudeParameter = new SqlParameter("@Latitude", SqlDbType.Decimal)
            {
                Precision = 9,
                Scale = 6,
                Value = (object?)restaurant.Latitude ?? DBNull.Value
            };

            var longitudeParameter = new SqlParameter("@Longitude", SqlDbType.Decimal)
            {
                Precision = 9,
                Scale = 6,
                Value = (object?)restaurant.Longitude ?? DBNull.Value
            };

            var restaurants = await _context.Restaurants
                .FromSqlInterpolated($"""
            EXEC UpdateRestaurant
                @Id={restaurant.Id},
                @Name={restaurant.Name},
                @Address={restaurant.Address},
                @Phone={restaurant.Phone},
                @Email={restaurant.Email},
                @IsActive={restaurant.IsActive},
                @OwnerUserId={restaurant.OwnerUserId},
                @City={restaurant.City},
                @Description={restaurant.Description},
                @CoverImagePath={restaurant.CoverImagePath},
                @Latitude={latitudeParameter},
                @Longitude={longitudeParameter},
                @UpdateCoordinates={updateCoordinates}
            """)
                .ToListAsync();

            return restaurants.Any();
        }


        // Marks the owner's restaurant as inactive without deleting related records.
        public async Task<bool> SoftDeleteAsync(int id, int ownerUserId)
        {
            var affectedRows = await _context.Restaurants
                .Where(r =>
                    r.Id == id &&
                    r.OwnerUserId == ownerUserId &&
                    r.IsActive)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(r => r.IsActive, false));

            return affectedRows > 0;
        }

        // Returns only active restaurants belonging to the specified owner.
        // Returns all restaurants owned by the user, including inactive ones.
        public async Task<List<Restaurant>> GetByOwnerIdAsync(int ownerUserId)
        {
            return await _context.Restaurants
                .AsNoTracking()
                .Where(r => r.OwnerUserId == ownerUserId)
                .ToListAsync();
        }

        public async Task<Restaurant?> GetByIdAndOwnerIdAsync(
                int restaurantId,
                int ownerUserId)
        {
            return await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(r =>
                    r.Id == restaurantId &&
                    r.OwnerUserId == ownerUserId);
        }


        // Soft-deletes a restaurant when requested by an authorized admin.
        public async Task<bool> SoftDeleteAsync(int id)
        {
            var affectedRows = await _context.Restaurants
                .Where(r => r.Id == id && r.IsActive)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(r => r.IsActive, false));

            return affectedRows > 0;
        }
    }
}