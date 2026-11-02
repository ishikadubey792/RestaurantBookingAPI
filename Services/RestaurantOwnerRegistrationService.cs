using System.Text.RegularExpressions;
using RestaurantBookingAPI.Exceptions;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.Models;
using RestaurantBookingAPI.Repositories;
using RestaurantBookingAPI.Security;
using RestaurantBookingAPI.IServices;

namespace RestaurantBookingAPI.Services
{
    public class RestaurantOwnerRegistrationService
        : IRestaurantOwnerRegistrationService
    {
        private readonly IUserRepository _userRepository;
        private readonly ICuisineRepository _cuisineRepository;
        private readonly IRestaurantOwnerRegistrationRepository _registrationRepository;
        private readonly PasswordService _passwordService;
        private readonly IWebHostEnvironment _environment;

        private const long MaxFileSize = 5 * 1024 * 1024;

        public RestaurantOwnerRegistrationService(
            IUserRepository userRepository,
            ICuisineRepository cuisineRepository,
            IRestaurantOwnerRegistrationRepository registrationRepository,
            PasswordService passwordService,
            IWebHostEnvironment environment)
        {
            _userRepository = userRepository;
            _cuisineRepository = cuisineRepository;
            _registrationRepository = registrationRepository;
            _passwordService = passwordService;
            _environment = environment;
        }

        public async Task<(User User, Restaurant Restaurant)> RegisterAsync(
            RestaurantOwnerRegisterRequest request)
        {
            // 1. Validate password
            if (!IsPasswordValid(request.Password))
            {
                throw new BadRequestException(
                    "Password must be 8-128 characters long and contain uppercase, lowercase, number and special character. Spaces are not allowed."
                );
            }

            // 2. Check duplicate email
            var email = request.OwnerEmail.Trim();

            var existingUser =
                await _userRepository.GetByEmailAsync(email);

            if (existingUser != null)
            {
                throw new BadRequestException(
                    "Email is already registered."
                );
            }

            // 3. Validate selected cuisines
            var cuisineIds = request.CuisineIds.Distinct().ToList();

            if (cuisineIds.Count != request.CuisineIds.Count)
            {
                throw new BadRequestException(
                    "Duplicate cuisines are not allowed."
                );
            }

            var validCuisines =
                await _cuisineRepository.AreAllActiveAsync(cuisineIds);

            if (!validCuisines)
            {
                throw new BadRequestException(
                    "One or more selected cuisines are invalid."
                );
            }

            // 4. Validate cover image
            var image = request.CoverImage;

            if (image == null || image.Length == 0)
            {
                throw new BadRequestException(
                    "Restaurant cover image is required."
                );
            }

            if (image.Length > MaxFileSize)
            {
                throw new BadRequestException(
                    "Cover image size must not exceed 5 MB."
                );
            }

            // 5. Validate image content
            var extension = await GetValidImageExtensionAsync(image);

            if (extension == null)
            {
                throw new BadRequestException(
                    "Only valid JPG, PNG and WEBP images are allowed."
                );
            }

            // 6. Create owner account
            var user = new User
            {
                Name = request.OwnerName.Trim(),
                Email = email,
                Role = "RestaurantOwner",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash =
                _passwordService.HashPassword(user, request.Password);

            // 7. Prepare restaurant
            var restaurant = new Restaurant
            {
                Name = request.RestaurantName.Trim(),
                Address = request.Address.Trim(),
                City = request.City.Trim(),
                Description = request.Description.Trim(),
                Phone = request.Phone.Trim(),
                Email = request.RestaurantEmail?.Trim(),
                IsActive = true
            };

            // 8. Save image with a unique filename
            var webRoot = _environment.WebRootPath
                ?? Path.Combine(_environment.ContentRootPath, "wwwroot");

            var uploadFolder = Path.Combine(
                webRoot,
                "uploads",
                "restaurants"
            );

            Directory.CreateDirectory(uploadFolder);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadFolder, fileName);

            try
            {
                await using (var stream = new FileStream(
                    filePath,
                    FileMode.CreateNew,
                    FileAccess.Write))
                {
                    await image.CopyToAsync(stream);
                }

                restaurant.CoverImagePath =
                    $"/uploads/restaurants/{fileName}";

                // 9. Save User, Restaurant and Cuisine mappings
                var registeredRestaurant =
                    await _registrationRepository.RegisterAsync(
                        user,
                        restaurant,
                        cuisineIds
                    );

                return (user, registeredRestaurant);
            }
            catch
            {
                // Remove uploaded image if database registration fails
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                throw;
            }
        }

        private static bool IsPasswordValid(string password)
        {
            return !string.IsNullOrEmpty(password)
                && password.Length >= 8
                && password.Length <= 128
                && Regex.IsMatch(
                    password,
                    @"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[^A-Za-z0-9\s])\S{8,128}$"
                );
        }

        private static async Task<string?> GetValidImageExtensionAsync(
            IFormFile image)
        {
            var extension = Path.GetExtension(image.FileName)
                .ToLowerInvariant();

            if (extension is not ".jpg" and not ".jpeg"
                and not ".png" and not ".webp")
            {
                return null;
            }

            byte[] header = new byte[12];

            await using var stream = image.OpenReadStream();

            int bytesRead = await stream.ReadAsync(
                header.AsMemory(0, header.Length)
            );

            bool isJpeg =
                bytesRead >= 3 &&
                header[0] == 0xFF &&
                header[1] == 0xD8 &&
                header[2] == 0xFF;

            bool isPng =
                bytesRead >= 8 &&
                header[0] == 0x89 &&
                header[1] == 0x50 &&
                header[2] == 0x4E &&
                header[3] == 0x47 &&
                header[4] == 0x0D &&
                header[5] == 0x0A &&
                header[6] == 0x1A &&
                header[7] == 0x0A;

            bool isWebp =
                bytesRead >= 12 &&
                header[0] == 'R' &&
                header[1] == 'I' &&
                header[2] == 'F' &&
                header[3] == 'F' &&
                header[8] == 'W' &&
                header[9] == 'E' &&
                header[10] == 'B' &&
                header[11] == 'P';

            if ((isJpeg && extension is ".jpg" or ".jpeg") ||
                (isPng && extension == ".png") ||
                (isWebp && extension == ".webp"))
            {
                return extension;
            }

            return null;
        }
    }
}