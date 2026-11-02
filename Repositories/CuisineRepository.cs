using Microsoft.EntityFrameworkCore;
using RestaurantBookingAPI.Data;
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Repositories
{
    public class CuisineRepository : ICuisineRepository
    {
        private readonly AppDbContext _context;

        public CuisineRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Cuisine>> GetAllActiveAsync()
        {
            return await _context.Cuisines
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<bool> AreAllActiveAsync(List<int> cuisineIds)
        {
            if (cuisineIds == null || cuisineIds.Count == 0)
                return false;

            var uniqueIds = cuisineIds.Distinct().ToList();

            var activeCount = await _context.Cuisines
                .CountAsync(c => uniqueIds.Contains(c.Id) && c.IsActive);

            return activeCount == uniqueIds.Count;
        }
    }
}