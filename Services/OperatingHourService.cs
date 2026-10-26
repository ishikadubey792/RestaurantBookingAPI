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
        private readonly OperatingHourValidator _validator;

        public OperatingHourService(
            IOperatingHourRepository repository,
            OperatingHourValidator validator)
        {
            _repository = repository;
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
            return await _repository
                .GetByRestaurantIdAsync(restaurantId);
        }

        public async Task<OperatingHour> CreateAsync(
            OperatingHour operatingHour)
        {
            var errors = _validator.Validate(operatingHour);

            if (errors.Any())
            {
                throw new BadRequestException(
                    string.Join(" ", errors));
            }

            return await _repository.AddAsync(operatingHour);
        }

        public async Task<bool> UpdateAsync(
            int id,
            OperatingHour operatingHour)
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

            existingOperatingHour.RestaurantId =
                operatingHour.RestaurantId;

            existingOperatingHour.DayOfWeek =
                operatingHour.DayOfWeek;

            existingOperatingHour.OpenTime =
                operatingHour.OpenTime;

            existingOperatingHour.CloseTime =
                operatingHour.CloseTime;

            existingOperatingHour.IsClosed =
                operatingHour.IsClosed;

            return await _repository
                .UpdateAsync(existingOperatingHour);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}