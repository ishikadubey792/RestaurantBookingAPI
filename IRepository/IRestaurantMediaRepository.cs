
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.IRepository
{
    public interface IRestaurantMediaRepository
    {
        // Retrieves all gallery images associated with a restaurant.
        Task<List<RestaurantImage>> GetRestaurantImagesAsync(
            int restaurantId);

        // Retrieves a specific gallery image belonging to a restaurant.
        Task<RestaurantImage?> GetRestaurantImageByIdAsync(
            int restaurantId,
            int imageId);

        // Saves a new restaurant gallery image.
        Task<RestaurantImage> AddRestaurantImageAsync(
            RestaurantImage image);

        // Deletes an existing restaurant gallery image record.
        Task<bool> DeleteRestaurantImageAsync(
            RestaurantImage image);

        // Updates the restaurant's primary cover image path.
        Task<bool> UpdateRestaurantCoverImageAsync(
            int restaurantId,
            string imagePath);


        // Retrieves all menu images associated with a restaurant.
        Task<List<MenuImage>> GetMenuImagesAsync(
            int restaurantId);

        // Retrieves a specific menu image belonging to a restaurant.
        Task<MenuImage?> GetMenuImageByIdAsync(
            int restaurantId,
            int imageId);

        // Saves a new restaurant menu image.
        Task<MenuImage> AddMenuImageAsync(
            MenuImage image);

        // Deletes an existing restaurant menu image record.
        Task<bool> DeleteMenuImageAsync(
            MenuImage image);
    }
}
