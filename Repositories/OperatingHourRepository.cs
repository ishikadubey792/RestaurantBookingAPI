
using Microsoft.EntityFrameworkCore;
using RestaurantBookingAPI.Data;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Repositories
{
    public class OperatingHourRepository : IOperatingHourRepository
    {
        private readonly AppDbContext _context;

        public OperatingHourRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<OperatingHour>> GetAllAsync()
        {
            return await _context.OperatingHours
                .FromSqlRaw("EXEC dbo.GetAllOperatingHours")
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<OperatingHour?> GetByIdAsync(int id)
        {
            var results = await _context.OperatingHours
                .FromSqlInterpolated($"""
                    EXEC dbo.GetOperatingHourById @Id = {id}
                    """)
                .AsNoTracking()
                .ToListAsync();

            return results.FirstOrDefault();
        }

        public async Task<List<OperatingHour>> GetByRestaurantIdAsync(
            int restaurantId)
        {
            return await _context.OperatingHours
                .FromSqlInterpolated($"""
                    EXEC dbo.GetOperatingHoursByRestaurantId
                        @RestaurantId = {restaurantId}
                    """)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<OperatingHour> AddAsync(
            OperatingHour operatingHour)
        {
            var results = await _context.OperatingHours
                .FromSqlInterpolated($"""
                    EXEC dbo.CreateOperatingHour
                        @RestaurantId = {operatingHour.RestaurantId},
                        @DayOfWeek = {operatingHour.DayOfWeek},
                        @OpenTime = {operatingHour.OpenTime},
                        @CloseTime = {operatingHour.CloseTime},
                        @IsClosed = {operatingHour.IsClosed}
                    """)
                .AsNoTracking()
                .ToListAsync();

            var created = results.FirstOrDefault();

            if (created == null)
            {
                throw new InvalidOperationException(
                    "Operating hour could not be created.");
            }

            return created;
        }

        public async Task<bool> UpdateAsync(
            OperatingHour operatingHour)
        {
            var results = await _context.OperatingHours
                .FromSqlInterpolated($"""
                    EXEC dbo.UpdateOperatingHour
                        @Id = {operatingHour.Id},
                        @RestaurantId = {operatingHour.RestaurantId},
                        @DayOfWeek = {operatingHour.DayOfWeek},
                        @OpenTime = {operatingHour.OpenTime},
                        @CloseTime = {operatingHour.CloseTime},
                        @IsClosed = {operatingHour.IsClosed}
                    """)
                .AsNoTracking()
                .ToListAsync();

            return results.Any();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var results = await _context.Database
                .SqlQueryRaw<int>(
                    "EXEC dbo.DeleteOperatingHour @Id = {0}", id)
                .ToListAsync();

            return results.FirstOrDefault() > 0;
        }
    }
}
