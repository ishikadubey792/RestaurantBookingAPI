
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

            // Verify duplicate day configuration within the same restaurant
            var existingHours =
                await _repository.GetByRestaurantIdAsync(operatingHour.RestaurantId);

            if (existingHours.Any(h => h.DayOfWeek.Equals(operatingHour.DayOfWeek, StringComparison.OrdinalIgnoreCase)))
            {
                throw new BadRequestException(
                    $"Operating hours for {operatingHour.DayOfWeek} are already configured for this restaurant.");
            }

            if (operatingHour.IsClosed)
            {
                operatingHour.OpenTime = null;
                operatingHour.CloseTime = null;
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

            // Prevent reassigning operating hours to another restaurant
            if (operatingHour.RestaurantId != 0 && operatingHour.RestaurantId != existingOperatingHour.RestaurantId)
            {
                var targetRestaurant =
                    await _restaurantRepository.GetByIdAndOwnerIdAsync(
                        operatingHour.RestaurantId,
                        ownerUserId);

                if (targetRestaurant == null)
                {
                    throw new BadRequestException(
                        "Cannot reassign operating hours to a restaurant you do not own.");
                }
            }

            // Verify duplicate day configuration within the restaurant (excluding this record)
            var existingHours =
                await _repository.GetByRestaurantIdAsync(existingOperatingHour.RestaurantId);

            if (existingHours.Any(h => h.Id != id && h.DayOfWeek.Equals(operatingHour.DayOfWeek, StringComparison.OrdinalIgnoreCase)))
            {
                throw new BadRequestException(
                    $"Operating hours for {operatingHour.DayOfWeek} are already configured for this restaurant.");
            }

            // Keep the original RestaurantId.
            // An owner cannot transfer an operating hour to another restaurant.
            existingOperatingHour.DayOfWeek =
                operatingHour.DayOfWeek;

            if (operatingHour.IsClosed)
            {
                existingOperatingHour.OpenTime = null;
                existingOperatingHour.CloseTime = null;
                existingOperatingHour.IsClosed = true;
            }
            else
            {
                existingOperatingHour.OpenTime =
                    operatingHour.OpenTime;

                existingOperatingHour.CloseTime =
                    operatingHour.CloseTime;

                existingOperatingHour.IsClosed = false;
            }

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
