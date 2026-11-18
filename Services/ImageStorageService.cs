
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using RestaurantBookingAPI.Exceptions;
using RestaurantBookingAPI.IServices;
using SixLabors.ImageSharp;

namespace RestaurantBookingAPI.Services
{
    public class ImageStorageService : IImageStorageService
    {
        private readonly IWebHostEnvironment _environment;

        // Maximum allowed size for a single image (5 MB).
        private const long MaxFileSize = 5 * 1024 * 1024;

        // Only these folders are allowed for storing uploaded images.
        private static readonly HashSet<string> AllowedFolders =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "restaurants",
                "menus"
            };

        public ImageStorageService(
            IWebHostEnvironment environment)
        {
            // Inject hosting environment to access the wwwroot directory.
            _environment = environment;
        }

        // Validates an image, saves it to the specified folder and returns its relative URL.
        public async Task<string> SaveImageAsync(
            IFormFile image,
            string folderName)
        {
            // Validate folder name to prevent uploads into unauthorized directories.
            if (string.IsNullOrWhiteSpace(folderName) ||
                !AllowedFolders.Contains(folderName))
            {
                throw new BadRequestException(
                    "Invalid image storage folder.");
            }

            // Ensure that an image file has been provided.
            if (image == null || image.Length == 0)
            {
                throw new BadRequestException(
                    "Please select a valid image.");
            }

            // Reject images larger than the maximum allowed file size.
            if (image.Length > MaxFileSize)
            {
                throw new BadRequestException(
                    "Image size must not exceed 5 MB.");
            }

            // Validate the file extension and actual image content using magic bytes.
            var extension = await GetValidImageExtensionAsync(image);

            if (extension == null)
            {
                throw new BadRequestException(
                    "Only valid JPG, PNG and WEBP images are allowed.");
            }

            // Get the wwwroot path or create its expected path if unavailable.
            var webRoot = _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");

            // Build the upload directory path: wwwroot/uploads/{folderName}.
            var uploadFolder = Path.Combine(
                webRoot,
                "uploads",
                folderName);

            // Create the upload directory if it does not already exist.
            Directory.CreateDirectory(uploadFolder);

            // Generate a unique filename to prevent filename conflicts.
            var fileName = $"{Guid.NewGuid()}{extension}";

            // Combine the directory path and filename to get the complete file path.
            var filePath = Path.Combine(
                uploadFolder,
                fileName);

            try
            {
                // Create a new file and asynchronously copy the uploaded image into it.
                await using var stream = new FileStream(
                    filePath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    useAsync: true);

                await image.CopyToAsync(stream);

                // Return a relative URL that can be stored in the database.
                return $"/uploads/{folderName}/{fileName}";
            }
            catch
            {
                // Remove any partially uploaded file if the save operation fails.
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // Re-throw the original exception for centralized error handling.
                throw;
            }
        }

        // Deletes an uploaded image after validating that its path belongs to the uploads directory.
        public Task DeleteImageAsync(string? imagePath)
        {
            // Nothing to delete when the image path is null or empty.
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return Task.CompletedTask;
            }

            // Get the wwwroot directory path.
            var webRoot = _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");

            // Resolve the absolute path of the uploads directory.
            var uploadsRoot = Path.GetFullPath(
                Path.Combine(webRoot, "uploads"));

            // Normalize the path to prevent directory traversal attempts.
            var normalizedPath = imagePath.Trim().Replace('\\', '/');

            // Remove the leading slash from relative URLs.
            if (normalizedPath.StartsWith('/'))
            {
                normalizedPath = normalizedPath.TrimStart('/');
            }

            // Reject paths containing traversal sequences or rooted paths.
            if (normalizedPath.Contains("..") ||
                Path.IsPathRooted(normalizedPath))
            {
                throw new BadRequestException(
                    "Invalid image path.");
            }

            // Convert the relative image URL into an absolute server file path.
            var fullPath = Path.GetFullPath(
                Path.Combine(
                    webRoot,
                    normalizedPath.Replace(
                        '/',
                        Path.DirectorySeparatorChar)));

            // Build the allowed directory prefix for path validation.
            var allowedPrefix = uploadsRoot
                + Path.DirectorySeparatorChar;

            // Ensure the image file is located inside the uploads directory.
            if (!fullPath.StartsWith(
                allowedPrefix,
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
            {
                throw new BadRequestException(
                    "Image path is outside the upload directory.");
            }



            // Restrict deletion to the approved restaurant and menu image folders.
            var allowedRestaurantPrefix = Path.GetFullPath(
                Path.Combine(webRoot, "uploads", "restaurants"))
                + Path.DirectorySeparatorChar;

            var allowedMenuPrefix = Path.GetFullPath(
                Path.Combine(webRoot, "uploads", "menus"))
                + Path.DirectorySeparatorChar;

            var comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            bool isAllowedFolder =
                fullPath.StartsWith(allowedRestaurantPrefix, comparison) ||
                fullPath.StartsWith(allowedMenuPrefix, comparison);

            if (!isAllowedFolder)
            {
                throw new BadRequestException(
                    "Image deletion is allowed only inside approved folders.");
            }

            Console.WriteLine($"Image Path from DB: {imagePath}");
            Console.WriteLine($"WebRootPath: {webRoot}");
            Console.WriteLine($"Resolved Full Path: {fullPath}");
            Console.WriteLine($"File Exists Before Delete: {File.Exists(fullPath)}");


            // Delete the physical image file only if it exists.
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }

        // Checks whether the file extension matches the actual image file signature.

        // Validates the actual image format, dimensions and image content.
        private static async Task<string?> GetValidImageExtensionAsync(
            IFormFile image)
        {
            // Extract and normalize the uploaded file extension.
            var extension = Path.GetExtension(image.FileName)
                .ToLowerInvariant();

            // Allow only supported image extensions.
            if (extension is not ".jpg" and not ".jpeg"
                and not ".png" and not ".webp")
            {
                return null;
            }

            try
            {
                await using var stream = image.OpenReadStream();

                // Detect the actual image format instead of trusting the extension.
                var format = await Image.DetectFormatAsync(stream);

                if (format == null)
                    return null;

                // Verify that the detected image format matches the extension.
                bool validFormat =
                    (format.Name.Equals("JPEG", StringComparison.OrdinalIgnoreCase)
                        && extension is ".jpg" or ".jpeg") ||
                    (format.Name.Equals("PNG", StringComparison.OrdinalIgnoreCase)
                        && extension == ".png") ||
                    (format.Name.Equals("WEBP", StringComparison.OrdinalIgnoreCase)
                        && extension == ".webp");

                if (!validFormat)
                    return null;

                // Reset the stream before reading image dimensions.
                stream.Position = 0;

                // Identify image dimensions before fully decoding the image.
                var imageInfo = await Image.IdentifyAsync(stream);

                if (imageInfo == null)
                    return null;

                // Reject images with excessive width or height.
                const int maxDimension = 8000;

                if (imageInfo.Width > maxDimension ||
                    imageInfo.Height > maxDimension)
                {
                    return null;
                }

                // Reject images containing more than 40 million pixels.
                long totalPixels =
                    (long)imageInfo.Width * imageInfo.Height;

                const long maxPixels = 40_000_000;

                if (totalPixels > maxPixels)
                    return null;

                // Reset the stream before performing full image decoding.
                stream.Position = 0;

                // Fully decode the image to reject malformed or corrupted files.
                using var decodedImage = await Image.LoadAsync(stream);

                // Return the validated extension.
                return extension;
            }
            catch (UnknownImageFormatException)
            {
                return null;
            }
            catch (InvalidImageContentException)
            {
                return null;
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }

    }
}
