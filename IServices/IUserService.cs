
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.IServices
{
    public interface IUserService
    {
        Task<User> RegisterAsync(User user, string password);

        Task<User?> LoginAsync(LoginRequest request);

        Task ForgotPasswordAsync(string email);

        Task<bool> VerifyResetOtpAsync(string email, string otp);

        Task ResetPasswordAsync(
            string email,
            string otp,
            string newPassword);
    }
}
