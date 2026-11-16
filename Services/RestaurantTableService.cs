
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
        private readonly IRestaurantRepository _restaurantRepository;
        private readonly RestaurantTableValidator _validator;

        public RestaurantTableService(
            IRestaurantTableRepository repository,
            IRestaurantRepository restaurantRepository,
            RestaurantTableValidator validator)
        {
            _repository = repository;
            _restaurantRepository = restaurantRepository;
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
            return await _repository.GetByRestaurantIdAsync(restaurantId);
        }

        public async Task<RestaurantTable> CreateAsync(
            RestaurantTable table,
            int ownerUserId)
        {
            var errors = _validator.Validate(table);

            if (errors.Any())
            {
                throw new BadRequestException(
                    string.Join(" ", errors));
            }

            // Verify that this restaurant belongs to the logged-in owner.
            var restaurant =
                await _restaurantRepository.GetByIdAndOwnerIdAsync(
                    table.RestaurantId,
                    ownerUserId);

            if (restaurant == null)
            {
                throw new BadRequestException(
                    "Restaurant not found or you do not have permission to manage it.");
            }

            // Verify duplicate table number within the same restaurant
            var existingTables =
                await _repository.GetByRestaurantIdAsync(table.RestaurantId);

            if (existingTables.Any(t => t.TableNumber == table.TableNumber))
            {
                throw new BadRequestException(
                    $"Table number {table.TableNumber} already exists in this restaurant.");
            }

            return await _repository.AddAsync(table);
        }

        public async Task<bool> UpdateAsync(
            int id,
            RestaurantTable table,
            int ownerUserId)
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

            // Verify ownership using the existing table's restaurant.
            var restaurant =
                await _restaurantRepository.GetByIdAndOwnerIdAsync(
                    existingTable.RestaurantId,
                    ownerUserId);

            if (restaurant == null)
            {
                return false;
            }

            // Prevent reassigning table to a different restaurant
            if (table.RestaurantId != 0 && table.RestaurantId != existingTable.RestaurantId)
            {
                var targetRestaurant =
                    await _restaurantRepository.GetByIdAndOwnerIdAsync(
                        table.RestaurantId,
                        ownerUserId);

                if (targetRestaurant == null)
                {
                    throw new BadRequestException(
                        "Cannot reassign table to a restaurant you do not own.");
                }
            }

            // Verify duplicate table number within the restaurant (excluding this table)
            var existingTables =
                await _repository.GetByRestaurantIdAsync(existingTable.RestaurantId);

            if (existingTables.Any(t => t.Id != id && t.TableNumber == table.TableNumber))
            {
                throw new BadRequestException(
                    $"Table number {table.TableNumber} already exists in this restaurant.");
            }

            // Do not allow the owner to move a table to another restaurant.
            existingTable.TableNumber = table.TableNumber;
            existingTable.Capacity = table.Capacity;
            existingTable.Status = table.Status;

            return await _repository.UpdateAsync(existingTable);
        }

        public async Task<bool> DeleteAsync(
            int id,
            int ownerUserId)
        {
            var existingTable =
                await _repository.GetByIdAsync(id);

            if (existingTable == null)
            {
                return false;
            }

            // Verify ownership before deletion.
            var restaurant =
                await _restaurantRepository.GetByIdAndOwnerIdAsync(
                    existingTable.RestaurantId,
                    ownerUserId);

            if (restaurant == null)
            {
                return false;
            }

            return await _repository.DeleteAsync(id);
        }
    }
}
