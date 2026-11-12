
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Repositories
{
    public interface IPaymentRepository
    {
        Task<Payment> AddAsync(Payment payment);

        Task<Payment?> GetByIdAsync(int id);

        Task<Payment?> GetByIdForUpdateAsync(int id);

        Task<Payment?> GetByBookingIdAsync(int bookingId);

        Task<List<Payment>> GetByUserIdAsync(int userId);

        Task<Payment> UpdateAsync(Payment payment);
    }
}
