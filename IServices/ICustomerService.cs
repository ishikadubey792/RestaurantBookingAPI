using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.IServices
{
    public interface ICustomerService
    {
        Task<List<Customer>> GetAllAsync();
        Task<Customer?> GetByIdAsync(int id);
        Task<Customer> CreateAsync(Customer customer);
        Task<bool> UpdateAsync(int id, Customer customer);
        Task<bool> DeleteAsync(int id);
    }
}