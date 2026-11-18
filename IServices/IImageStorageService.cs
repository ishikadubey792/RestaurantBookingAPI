
using Microsoft.AspNetCore.Http;
using static System.Net.Mime.MediaTypeNames;

namespace RestaurantBookingAPI.IServices
{
    public interface IImageStorageService
    {
        //It will validate image and will save on a disk and return relative URL
        Task<string> SaveImageAsync(
            IFormFile image,
            string folderName
        );

        // It will delete image from disk based on relative URL
        Task DeleteImageAsync(string? imagePath);
    }
}
