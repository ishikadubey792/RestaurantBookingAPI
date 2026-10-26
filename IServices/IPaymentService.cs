
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.IServices
{
    public interface IPaymentService
    {
        Task<Payment> InitiatePaymentAsync(
            int bookingId,
            int userId);

        Task<Payment?> GetPaymentByIdAsync(int paymentId);

        Task<List<Payment>> GetPaymentHistoryAsync(int userId);

        Task<Payment> ProcessDemoPaymentAsync(
            int paymentId,
            bool simulateSuccess);
    }
}
