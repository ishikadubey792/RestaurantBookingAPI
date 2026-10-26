
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.IRepository
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);

        Task<User> AddAsync(User user);

        Task UpdateAsync(User user);
    }
}
