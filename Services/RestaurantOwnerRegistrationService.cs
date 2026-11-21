
using RestaurantBookingAPI.Exceptions;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.IServices;
using RestaurantBookingAPI.Models;
using RestaurantBookingAPI.Repositories;
using RestaurantBookingAPI.Security;
using System.Text.RegularExpressions;

namespace RestaurantBookingAPI.Services
{
    public class RestaurantOwnerRegistrationService
        : IRestaurantOwnerRegistrationService
    {
        private readonly IUserRepository _userRepository;
        private readonly ICuisineRepository _cuisineRepository;
        private readonly IRestaurantOwnerRegistrationRepository _registrationRepository;
        private readonly PasswordService _passwordService;
        private readonly IImageStorageService _imageStorageService;

        private readonly ILegalDocumentRepository _legalDocumentRepository;
        private readonly IUserLegalAcceptanceRepository _userLegalAcceptanceRepository;

        public RestaurantOwnerRegistrationService(
            IUserRepository userRepository,
            ICuisineRepository cuisineRepository,
            IRestaurantOwnerRegistrationRepository registrationRepository,
            PasswordService passwordService,
            IImageStorageService imageStorageService,
            ILegalDocumentRepository legalDocumentRepository,
            IUserLegalAcceptanceRepository userLegalAcceptanceRepository)
        {
            // Inject repositories and services required for owner registration.
            _userRepository = userRepository;
            _cuisineRepository = cuisineRepository;
            _registrationRepository = registrationRepository;
            _passwordService = passwordService;
            _imageStorageService = imageStorageService;
            _legalDocumentRepository = legalDocumentRepository;
            _userLegalAcceptanceRepository = userLegalAcceptanceRepository;
        }

        public async Task<(User User, Restaurant Restaurant)> RegisterAsync(
            RestaurantOwnerRegisterRequest request)
        {
            // 1. Validate password before creating the owner account.
            if (!IsPasswordValid(request.Password))
            {
                throw new BadRequestException(
                    "Password must be 8-128 characters long and contain uppercase, lowercase, number and special character. Spaces are not allowed."
                );
            }


            // 2. Validate Terms and Conditions acceptance.
            if (!request.AcceptedTerms)
            {
                throw new BadRequestException(
                    "You must accept the Terms and Conditions."
                );
            }

            // Fetch the currently active Terms and Conditions document.
            var activeTerms =
                await _legalDocumentRepository.GetActiveDocumentAsync(
                    "TermsAndConditions"
                );

            if (activeTerms == null)
            {
                throw new BadRequestException(
                    "Terms and Conditions are currently unavailable."
                );
            }

            // Ensure that the owner accepted the currently active version.
            if (activeTerms.Id != request.LegalDocumentId)
            {
                throw new BadRequestException(
                    "Terms and Conditions have been updated. Please review and accept the latest version."
                );
            }

            // 2. Check whether the owner's email is already registered.
            var email = request.OwnerEmail.Trim();

            var existingUser =
                await _userRepository.GetByEmailAsync(email);

            if (existingUser != null)
            {
                throw new BadRequestException(
                    "Email is already registered."
                );
            }

            // 3. Validate selected cuisines and prevent duplicate selections.
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

            // 4. Ensure that the restaurant cover image is provided.
            var image = request.CoverImage;

            if (image == null || image.Length == 0)
            {
                throw new BadRequestException(
                    "Restaurant cover image is required."
                );
            }

            // 5. Create the owner account with a securely hashed password.
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

            // 6. Prepare the restaurant entity.
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

            string? savedImagePath = null;

            try
            {
                // 7. Validate and save the cover image using the common storage service.
                savedImagePath = await _imageStorageService.SaveImageAsync(
                    image,
                    "restaurants"
                );

                // Store the relative image URL in the restaurant entity.
                restaurant.CoverImagePath = savedImagePath;

                // 8. Save owner, restaurant and cuisine mappings in the database.
                var registeredRestaurant =
                    await _registrationRepository.RegisterAsync(
                        user,
                        restaurant,
                        cuisineIds,
                        activeTerms.Id
                    );

                // Return the successfully registered owner and restaurant.
                return (user, registeredRestaurant);
            }
            catch
            {
                // 9. Remove the uploaded image if database registration fails.
                if (!string.IsNullOrWhiteSpace(savedImagePath))
                {
                    await _imageStorageService.DeleteImageAsync(
                        savedImagePath
                    );
                }

                // Re-throw the original exception for centralized error handling.
                throw;
            }
        }

        // Checks whether the password meets the required security rules.
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
    }
}
