using RestaurantBookingAPI.Exceptions;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.IServices;
using RestaurantBookingAPI.Models;
using RestaurantBookingAPI.Validation;

namespace RestaurantBookingAPI.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _repository;
        private readonly CustomerValidator _validator;

        public CustomerService(
            ICustomerRepository repository,
            CustomerValidator validator)
        {
            _repository = repository;
            _validator = validator;
        }

        public async Task<List<Customer>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<Customer> CreateAsync(Customer customer)
        {
            var errors = _validator.Validate(customer);

            if (errors.Any())
            {
                throw new BadRequestException(
                    string.Join(" ", errors));
            }

            return await _repository.AddAsync(customer);
        }

        public async Task<bool> UpdateAsync(
            int id,
            Customer customer)
        {
            var errors = _validator.Validate(customer);

            if (errors.Any())
            {
                throw new BadRequestException(
                    string.Join(" ", errors));
            }

            var existingCustomer =
                await _repository.GetByIdAsync(id);

            if (existingCustomer == null)
            {
                return false;
            }

            existingCustomer.Name = customer.Name;
            existingCustomer.Email = customer.Email;
            existingCustomer.Phone = customer.Phone;

            return await _repository.UpdateAsync(existingCustomer);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}