
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.IRepository
{
    public interface IPasswordResetOtpRepository
    {
        Task<PasswordResetOtp> AddAsync(PasswordResetOtp otp);

        Task<PasswordResetOtp?> GetLatestUnusedAsync(int userId);

        Task UpdateAsync(PasswordResetOtp otp);
    }
}
