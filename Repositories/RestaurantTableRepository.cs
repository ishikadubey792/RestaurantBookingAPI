
using Microsoft.EntityFrameworkCore;
using RestaurantBookingAPI.Data;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Repositories
{
    public class RestaurantTableRepository : IRestaurantTableRepository
    {
        private readonly AppDbContext _context;

        public RestaurantTableRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<RestaurantTable>> GetAllAsync()
        {
            return await _context.RestaurantTables
                .FromSqlRaw("EXEC dbo.GetAllRestaurantTables")
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<RestaurantTable?> GetByIdAsync(int id)
        {
            var results = await _context.RestaurantTables
                .FromSqlInterpolated($"""
                    EXEC dbo.GetRestaurantTableById @Id = {id}
                    """)
                .AsNoTracking()
                .ToListAsync();

            return results.FirstOrDefault();
        }

        public async Task<List<RestaurantTable>> GetByRestaurantIdAsync(
            int restaurantId)
        {
            return await _context.RestaurantTables
                .FromSqlInterpolated($"""
                    EXEC dbo.GetRestaurantTablesByRestaurantId
                        @RestaurantId = {restaurantId}
                    """)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<RestaurantTable> AddAsync(RestaurantTable table)
        {
            var results = await _context.RestaurantTables
                .FromSqlInterpolated($"""
                    EXEC dbo.CreateRestaurantTable
                        @RestaurantId = {table.RestaurantId},
                        @TableNumber = {table.TableNumber},
                        @Capacity = {table.Capacity},
                        @Status = {table.Status}
                    """)
                .AsNoTracking()
                .ToListAsync();

            var created = results.FirstOrDefault();

            if (created == null)
            {
                throw new InvalidOperationException(
                    "Restaurant table could not be created.");
            }

            return created;
        }

        public async Task<bool> UpdateAsync(RestaurantTable table)
        {
            var results = await _context.RestaurantTables
                .FromSqlInterpolated($"""
                    EXEC dbo.UpdateRestaurantTable
                        @Id = {table.Id},
                        @RestaurantId = {table.RestaurantId},
                        @TableNumber = {table.TableNumber},
                        @Capacity = {table.Capacity},
                        @Status = {table.Status}
                    """)
                .AsNoTracking()
                .ToListAsync();

            return results.Any();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var results = await _context.Database
                .SqlQueryRaw<int>(
                    "EXEC dbo.DeleteRestaurantTable @Id = {0}", id)
                .ToListAsync();

            return results.FirstOrDefault() > 0;
        }

        public async Task<List<RestaurantTable>> GetAvailableTablesAsync(
            DateTime bookingDate,
            TimeSpan startTime,
            TimeSpan endTime,
            int numberOfGuests)
        {
            return await _context.RestaurantTables
                .FromSqlInterpolated($"""
                    EXEC dbo.GetAvailableRestaurantTables
                        @BookingDate = {bookingDate.Date},
                        @StartTime = {startTime},
                        @EndTime = {endTime},
                        @NumberOfGuests = {numberOfGuests}
                    """)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
