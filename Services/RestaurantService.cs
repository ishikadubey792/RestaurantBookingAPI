using RestaurantBookingAPI.Exceptions;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.IServices;
using RestaurantBookingAPI.Models;
using RestaurantBookingAPI.Validation;

namespace RestaurantBookingAPI.Services
{
    public class RestaurantService : IRestaurantService
    {
        private readonly IRestaurantRepository _repository;
        private readonly RestaurantValidator _validator;

        public RestaurantService(
            IRestaurantRepository repository,
            RestaurantValidator validator)
        {
            _repository = repository;
            _validator = validator;
        }

        public async Task<List<Restaurant>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Restaurant?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<Restaurant> CreateAsync(Restaurant restaurant)
        {
            var errors = _validator.Validate(restaurant);

            if (errors.Any())
            {
                throw new BadRequestException(
                    string.Join(" ", errors));
            }

            restaurant.IsActive = true;

            return await _repository.AddAsync(restaurant);
        }

        public async Task<bool> UpdateAsync(
    int id,
    Restaurant restaurant)
        {
            var errors = _validator.Validate(restaurant);

            if (errors.Any())
            {
                throw new BadRequestException(
                    string.Join(" ", errors));
            }

            var existingRestaurant =
                await _repository.GetByIdAsync(id);

            if (existingRestaurant == null)
            {
                return false;
            }

            existingRestaurant.Name = restaurant.Name;
            existingRestaurant.Address = restaurant.Address;
            existingRestaurant.Phone = restaurant.Phone;
            existingRestaurant.Email = restaurant.Email;
            existingRestaurant.IsActive = restaurant.IsActive;

            existingRestaurant.OwnerUserId = restaurant.OwnerUserId;
            existingRestaurant.City = restaurant.City;
            existingRestaurant.Description = restaurant.Description;
            existingRestaurant.CoverImagePath = restaurant.CoverImagePath;

            return await _repository.UpdateAsync(existingRestaurant);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<List<Restaurant>> GetMyRestaurantsAsync(int ownerUserId)
        {
            return await _repository.GetByOwnerIdAsync(ownerUserId);
        }


        public async Task<Restaurant?> GetMyRestaurantByIdAsync(
                int restaurantId,
                int ownerUserId)
        {
            return await _repository.GetByIdAndOwnerIdAsync(
                restaurantId,
                ownerUserId
            );
        }

        public async Task<bool> UpdateMyRestaurantAsync(
    int restaurantId,
    int ownerUserId,
    Restaurant restaurant)
        {
            var existingRestaurant =
                await _repository.GetByIdAndOwnerIdAsync(
                    restaurantId,
                    ownerUserId
                );

            if (existingRestaurant == null)
            {
                return false;
            }

            var errors = _validator.Validate(restaurant);

            if (errors.Any())
            {
                throw new BadRequestException(
                    string.Join(" ", errors)
                );
            }

            // Update only owner-editable properties
            existingRestaurant.Name = restaurant.Name;
            existingRestaurant.Address = restaurant.Address;
            existingRestaurant.Phone = restaurant.Phone;
            existingRestaurant.Email = restaurant.Email;
            existingRestaurant.City = restaurant.City;
            existingRestaurant.Description = restaurant.Description;

            return await _repository.UpdateAsync(existingRestaurant);
        }


        public async Task<bool> DeleteMyRestaurantAsync(
            int restaurantId,
            int ownerUserId)
        {
            var existingRestaurant =
                await _repository.GetByIdAndOwnerIdAsync(
                    restaurantId,
                    ownerUserId
                );

            if (existingRestaurant == null)
            {
                return false;
            }

            return await _repository.DeleteAsync(restaurantId);
        }
    }
}