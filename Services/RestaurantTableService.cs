using RestaurantBookingAPI.Exceptions;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.IServices;
using RestaurantBookingAPI.Models;
using RestaurantBookingAPI.Validation;

namespace RestaurantBookingAPI.Services
{
    public class RestaurantTableService : IRestaurantTableService
    {
        private readonly IRestaurantTableRepository _repository;
        private readonly RestaurantTableValidator _validator;

        public RestaurantTableService(
            IRestaurantTableRepository repository,
            RestaurantTableValidator validator)
        {
            _repository = repository;
            _validator = validator;
        }

        public async Task<List<RestaurantTable>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<RestaurantTable?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<List<RestaurantTable>> GetByRestaurantIdAsync(
            int restaurantId)
        {
            return await _repository
                .GetByRestaurantIdAsync(restaurantId);
        }

        public async Task<RestaurantTable> CreateAsync(
            RestaurantTable table)
        {
            var errors = _validator.Validate(table);

            if (errors.Any())
            {
                throw new BadRequestException(
                    string.Join(" ", errors));
            }

            return await _repository.AddAsync(table);
        }

        public async Task<bool> UpdateAsync(
            int id,
            RestaurantTable table)
        {
            var errors = _validator.Validate(table);

            if (errors.Any())
            {
                throw new BadRequestException(
                    string.Join(" ", errors));
            }

            var existingTable =
                await _repository.GetByIdAsync(id);

            if (existingTable == null)
            {
                return false;
            }

            existingTable.TableNumber =
                table.TableNumber;

            existingTable.Capacity =
                table.Capacity;

            existingTable.Status =
                table.Status;

            return await _repository
                .UpdateAsync(existingTable);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}