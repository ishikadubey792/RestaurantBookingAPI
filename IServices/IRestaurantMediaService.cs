using RestaurantBookingAPI.Models;
using Microsoft.AspNetCore.Http;
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.IServices
{
    public interface IRestaurantMediaService
    {
        // Retrieves all gallery images for a restaurant.
        Task<List<RestaurantImage>> GetRestaurantImagesAsync(int restaurantId);

        // Uploads a gallery image after validating restaurant ownership.
        Task<RestaurantImage> AddRestaurantImageAsync(
            int restaurantId,
            int ownerUserId,
            IFormFile image);

        // Deletes a gallery image after validating restaurant ownership.
        Task<bool> DeleteRestaurantImageAsync(
            int restaurantId,
            int imageId,
            int ownerUserId);

        // Sets an existing gallery image as the restaurant's primary image.
        Task<bool> SetPrimaryImageAsync(
            int restaurantId,
            int imageId,
            int ownerUserId);

        // Retrieves all menu images for a restaurant.
        Task<List<MenuImage>> GetMenuImagesAsync(int restaurantId);

        // Uploads a menu image after validating restaurant ownership.
        Task<MenuImage> AddMenuImageAsync(
            int restaurantId,
            int ownerUserId,
            IFormFile image);

        // Deletes a menu image after validating restaurant ownership.
        Task<bool> DeleteMenuImageAsync(
            int restaurantId,
            int imageId,
            int ownerUserId);
    }
}