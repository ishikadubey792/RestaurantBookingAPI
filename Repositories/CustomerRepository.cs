
using Microsoft.EntityFrameworkCore;
using RestaurantBookingAPI.Data;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly AppDbContext _context;

        public CustomerRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Customer>> GetAllAsync()
        {
            return await _context.Customers
                .FromSqlRaw("EXEC dbo.GetAllCustomers")
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            var results = await _context.Customers
                .FromSqlInterpolated(
                    $"EXEC dbo.GetCustomerById @Id = {id}")
                .AsNoTracking()
                .ToListAsync();

            return results.FirstOrDefault();
        }

        public async Task<Customer> AddAsync(Customer customer)
        {
            var results = await _context.Customers
                .FromSqlInterpolated($"""
                    EXEC dbo.CreateCustomer
                        @UserId = {customer.UserId},
                        @Name = {customer.Name},
                        @Email = {customer.Email},
                        @Phone = {customer.Phone}
                    """)
                .AsNoTracking()
                .ToListAsync();

            var created = results.FirstOrDefault();

            if (created == null)
            {
                throw new InvalidOperationException(
                    "Customer could not be created.");
            }

            return created;
        }

        public async Task<bool> UpdateAsync(Customer customer)
        {
            var results = await _context.Customers
                .FromSqlInterpolated($"""
                    EXEC dbo.UpdateCustomer
                        @Id = {customer.Id},
                        @Name = {customer.Name},
                        @Email = {customer.Email},
                        @Phone = {customer.Phone}
                    """)
                .AsNoTracking()
                .ToListAsync();

            return results.Any();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var results = await _context.Database
                .SqlQueryRaw<int>(
                    "EXEC dbo.DeleteCustomer @Id = {0}", id)
                .ToListAsync();

            return results.FirstOrDefault() > 0;
        }

        public async Task<Customer?> GetByUserIdAsync(int userId)
        {
            var results = await _context.Customers
                .FromSqlInterpolated(
                    $"EXEC dbo.GetCustomerByUserId @UserId = {userId}")
                .AsNoTracking()
                .ToListAsync();

            return results.FirstOrDefault();
        }
    }
}
