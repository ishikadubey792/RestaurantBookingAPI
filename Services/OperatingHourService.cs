
using RestaurantBookingAPI.Exceptions;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.IServices;
using RestaurantBookingAPI.Models;
using RestaurantBookingAPI.Validation;

namespace RestaurantBookingAPI.Services
{
    public class OperatingHourService : IOperatingHourService
    {
        private readonly IOperatingHourRepository _repository;
        private readonly IRestaurantRepository _restaurantRepository;
        private readonly OperatingHourValidator _validator;

        public OperatingHourService(
            IOperatingHourRepository repository,
            IRestaurantRepository restaurantRepository,
            OperatingHourValidator validator)
        {
            _repository = repository;
            _restaurantRepository = restaurantRepository;
            _validator = validator;
        }

        public async Task<List<OperatingHour>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<OperatingHour?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<List<OperatingHour>> GetByRestaurantIdAsync(
            int restaurantId)
        {
            return await _repository.GetByRestaurantIdAsync(restaurantId);
        }

        public async Task<OperatingHour> CreateAsync(
            OperatingHour operatingHour,
            int ownerUserId)
        {
            var errors = _validator.Validate(operatingHour);

            if (errors.Any())
            {
                throw new BadRequestException(
                    string.Join(" ", errors));
            }

            // Verify restaurant ownership.
            var restaurant =
                await _restaurantRepository.GetByIdAndOwnerIdAsync(
                    operatingHour.RestaurantId,
                    ownerUserId);

            if (restaurant == null)
            {
                throw new BadRequestException(
                    "Restaurant not found or you do not have permission to manage it.");
            }

            return await _repository.AddAsync(operatingHour);
        }

        public async Task<bool> UpdateAsync(
            int id,
            OperatingHour operatingHour,
            int ownerUserId)
        {
            var errors = _validator.Validate(operatingHour);

            if (errors.Any())
            {
                throw new BadRequestException(
                    string.Join(" ", errors));
            }

            var existingOperatingHour =
                await _repository.GetByIdAsync(id);

            if (existingOperatingHour == null)
            {
                return false;
            }

            // Verify ownership using the existing record.
            var restaurant =
                await _restaurantRepository.GetByIdAndOwnerIdAsync(
                    existingOperatingHour.RestaurantId,
                    ownerUserId);

            if (restaurant == null)
            {
                return false;
            }

            // Keep the original RestaurantId.
            // An owner cannot transfer an operating hour to another restaurant.
            existingOperatingHour.DayOfWeek =
                operatingHour.DayOfWeek;

            existingOperatingHour.OpenTime =
                operatingHour.OpenTime;

            existingOperatingHour.CloseTime =
                operatingHour.CloseTime;

            existingOperatingHour.IsClosed =
                operatingHour.IsClosed;

            return await _repository.UpdateAsync(existingOperatingHour);
        }

        public async Task<bool> DeleteAsync(
            int id,
            int ownerUserId)
        {
            var existingOperatingHour =
                await _repository.GetByIdAsync(id);

            if (existingOperatingHour == null)
            {
                return false;
            }

            // Verify ownership before deletion.
            var restaurant =
                await _restaurantRepository.GetByIdAndOwnerIdAsync(
                    existingOperatingHour.RestaurantId,
                    ownerUserId);

            if (restaurant == null)
            {
                return false;
            }

            return await _repository.DeleteAsync(id);
        }
    }
}
