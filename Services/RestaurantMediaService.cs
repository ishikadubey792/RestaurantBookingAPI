
using Microsoft.AspNetCore.Http;
using RestaurantBookingAPI.Exceptions;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.IServices;
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Services
{
    public class RestaurantMediaService : IRestaurantMediaService
    {
        private readonly IRestaurantRepository _restaurantRepository;
        private readonly IRestaurantMediaRepository _mediaRepository;
        private readonly IImageStorageService _imageStorageService;

        public RestaurantMediaService(
            IRestaurantRepository restaurantRepository,
            IRestaurantMediaRepository mediaRepository,
            IImageStorageService imageStorageService)
        {
            // Inject repositories and storage service required for media operations.
            _restaurantRepository = restaurantRepository;
            _mediaRepository = mediaRepository;
            _imageStorageService = imageStorageService;
        }

        // Verifies that the restaurant belongs to the authenticated owner.
        private async Task<Restaurant> GetOwnedRestaurantAsync(
            int restaurantId,
            int ownerUserId)
        {
            var restaurant = await _restaurantRepository
                .GetByIdAndOwnerIdAsync(restaurantId, ownerUserId);

            if (restaurant == null)
                throw new UnauthorizedAccessException(
                    "You are not authorized to manage this restaurant.");

            return restaurant;
        }

        // Retrieves gallery images for customer-facing display.
        // Retrieves gallery images only when the restaurant exists and is active.
        public async Task<List<RestaurantImage>> GetRestaurantImagesAsync(
            int restaurantId)
        {
            await GetActiveRestaurantAsync(restaurantId);

            return await _mediaRepository.GetRestaurantImagesAsync(restaurantId);
        }

        // Saves a gallery image and creates its database record.
        public async Task<RestaurantImage> AddRestaurantImageAsync(
            int restaurantId,
            int ownerUserId,
            IFormFile image)
        {
            await GetOwnedRestaurantAsync(restaurantId, ownerUserId);

            string savedPath = await _imageStorageService
                .SaveImageAsync(image, "restaurants");

            try
            {
                var restaurantImage = new RestaurantImage
                {
                    RestaurantId = restaurantId,
                    ImagePath = savedPath,
                    CreatedAt = DateTime.UtcNow
                };

                var addedImage = await _mediaRepository
                    .AddRestaurantImageAsync(restaurantImage);

                // Use the first gallery image as the cover if no cover exists.
                var restaurant = await GetOwnedRestaurantAsync(
                    restaurantId, ownerUserId);

                if (string.IsNullOrWhiteSpace(restaurant.CoverImagePath))
                {
                    await _mediaRepository.UpdateRestaurantCoverImageAsync(
                        restaurantId, savedPath);
                }

                return addedImage;
            }
            catch
            {
                // Remove the uploaded file if database processing fails.
                await _imageStorageService.DeleteImageAsync(savedPath);
                throw;
            }
        }

        // Deletes a gallery image and updates the cover if that image was primary.
        public async Task<bool> DeleteRestaurantImageAsync(
            int restaurantId,
            int imageId,
            int ownerUserId)
        {
            var restaurant = await GetOwnedRestaurantAsync(
                restaurantId, ownerUserId);

            var image = await _mediaRepository.GetRestaurantImageByIdAsync(
                restaurantId, imageId);

            if (image == null)
                return false;

            bool wasCoverImage = string.Equals(
                restaurant.CoverImagePath,
                image.ImagePath,
                StringComparison.OrdinalIgnoreCase);

            bool deleted = await _mediaRepository.DeleteRestaurantImageAsync(image);

            if (!deleted)
                return false;

            // Replace the cover with another gallery image if the cover was deleted.
            if (wasCoverImage)
            {
                var remainingImages = await _mediaRepository
                    .GetRestaurantImagesAsync(restaurantId);

                string replacementPath =
                    remainingImages.FirstOrDefault()?.ImagePath ?? string.Empty;

                await _mediaRepository.UpdateRestaurantCoverImageAsync(
                    restaurantId, replacementPath);
            }

            // Delete the physical file only after its database record is removed.
            await _imageStorageService.DeleteImageAsync(image.ImagePath);

            return true;
        }

        // Sets an existing gallery image as the restaurant's cover image.
        public async Task<bool> SetPrimaryImageAsync(
            int restaurantId,
            int imageId,
            int ownerUserId)
        {
            await GetOwnedRestaurantAsync(restaurantId, ownerUserId);

            var image = await _mediaRepository.GetRestaurantImageByIdAsync(
                restaurantId, imageId);

            if (image == null)
                return false;

            return await _mediaRepository.UpdateRestaurantCoverImageAsync(
                restaurantId, image.ImagePath);
        }

        // Retrieves menu images only when the restaurant exists and is active.
        public async Task<List<MenuImage>> GetMenuImagesAsync(int restaurantId)
        {
            await GetActiveRestaurantAsync(restaurantId);

            return await _mediaRepository.GetMenuImagesAsync(restaurantId);
        }

        // Saves a menu image and assigns its display order.
        public async Task<MenuImage> AddMenuImageAsync(
            int restaurantId,
            int ownerUserId,
            IFormFile image)
        {
            await GetOwnedRestaurantAsync(restaurantId, ownerUserId);

            string savedPath = await _imageStorageService
                .SaveImageAsync(image, "menus");

            try
            {
                var existingImages = await _mediaRepository
                    .GetMenuImagesAsync(restaurantId);

                int nextDisplayOrder = existingImages.Count == 0
                    ? 1
                    : existingImages.Max(m => m.DisplayOrder) + 1;

                var menuImage = new MenuImage
                {
                    RestaurantId = restaurantId,
                    ImagePath = savedPath,
                    DisplayOrder = nextDisplayOrder,
                    CreatedAt = DateTime.UtcNow
                };

                return await _mediaRepository.AddMenuImageAsync(menuImage);
            }
            catch
            {
                // Remove the uploaded file if database processing fails.
                await _imageStorageService.DeleteImageAsync(savedPath);
                throw;
            }
        }

        // Deletes a menu image after verifying restaurant ownership.
        public async Task<bool> DeleteMenuImageAsync(
            int restaurantId,
            int imageId,
            int ownerUserId)
        {
            await GetOwnedRestaurantAsync(restaurantId, ownerUserId);

            var image = await _mediaRepository.GetMenuImageByIdAsync(
                restaurantId, imageId);

            if (image == null)
                return false;

            bool deleted = await _mediaRepository.DeleteMenuImageAsync(image);

            if (!deleted)
                return false;

            // Remove the physical menu image after deleting its database record.
            await _imageStorageService.DeleteImageAsync(image.ImagePath);

            return true;
        }


        // Verifies that the restaurant exists and is active before public media access.
        private async Task<Restaurant> GetActiveRestaurantAsync(int restaurantId)
        {
            var restaurant = await _restaurantRepository.GetByIdAsync(restaurantId);

            if (restaurant == null || !restaurant.IsActive)
            {
                throw new NotFoundException("Restaurant not found.");
            }

            return restaurant;
        }
    }
}
