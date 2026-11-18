
using Microsoft.EntityFrameworkCore;
using RestaurantBookingAPI.Data;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Repositories
{
    public class RestaurantMediaRepository : IRestaurantMediaRepository
    {
        private readonly AppDbContext _context;

        public RestaurantMediaRepository(AppDbContext context)
        {
            // Inject AppDbContext to perform database operations.
            _context = context;
        }

        // Retrieves all gallery images for a restaurant in upload order.
        public async Task<List<RestaurantImage>> GetRestaurantImagesAsync(
            int restaurantId)
        {
            return await _context.RestaurantImages
                .AsNoTracking()
                .Where(i => i.RestaurantId == restaurantId)
                .OrderBy(i => i.CreatedAt)
                .ToListAsync();
        }

        // Retrieves a specific gallery image only if it belongs to the given restaurant.
        public async Task<RestaurantImage?> GetRestaurantImageByIdAsync(
            int restaurantId,
            int imageId)
        {
            return await _context.RestaurantImages
                .AsNoTracking()
                .FirstOrDefaultAsync(i =>
                    i.Id == imageId &&
                    i.RestaurantId == restaurantId);
        }

        // Inserts a new gallery image record into the database.
        public async Task<RestaurantImage> AddRestaurantImageAsync(
            RestaurantImage image)
        {
            await _context.RestaurantImages.AddAsync(image);

            await _context.SaveChangesAsync();

            return image;
        }

        // Deletes an existing gallery image record from the database.
        public async Task<bool> DeleteRestaurantImageAsync(
            RestaurantImage image)
        {
            _context.RestaurantImages.Remove(image);

            int affectedRows = await _context.SaveChangesAsync();

            return affectedRows > 0;
        }

        // Updates the restaurant's primary cover image path.
        public async Task<bool> UpdateRestaurantCoverImageAsync(
            int restaurantId,
            string imagePath)
        {
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.Id == restaurantId);

            if (restaurant == null)
            {
                return false;
            }

            // Update the existing cover image path.
            restaurant.CoverImagePath = imagePath;

            int affectedRows = await _context.SaveChangesAsync();

            return affectedRows > 0;
        }


        // Retrieves all menu images in their configured display order.
        public async Task<List<MenuImage>> GetMenuImagesAsync(
            int restaurantId)
        {
            return await _context.MenuImages
                .AsNoTracking()
                .Where(m => m.RestaurantId == restaurantId)
                .OrderBy(m => m.DisplayOrder)
                .ThenBy(m => m.Id)
                .ToListAsync();
        }

        // Retrieves a specific menu image only if it belongs to the given restaurant.
        public async Task<MenuImage?> GetMenuImageByIdAsync(
            int restaurantId,
            int imageId)
        {
            return await _context.MenuImages
                .AsNoTracking()
                .FirstOrDefaultAsync(m =>
                    m.Id == imageId &&
                    m.RestaurantId == restaurantId);
        }

        // Inserts a new menu image record into the database.
        public async Task<MenuImage> AddMenuImageAsync(
            MenuImage image)
        {
            await _context.MenuImages.AddAsync(image);

            await _context.SaveChangesAsync();

            return image;
        }

        // Deletes an existing menu image record from the database.
        public async Task<bool> DeleteMenuImageAsync(
            MenuImage image)
        {
            _context.MenuImages.Remove(image);

            int affectedRows = await _context.SaveChangesAsync();

            return affectedRows > 0;
        }
    }
}
