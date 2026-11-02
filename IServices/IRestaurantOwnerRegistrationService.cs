using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.IServices
{
    public interface IRestaurantOwnerRegistrationService
    {
        Task<(User User, Restaurant Restaurant)> RegisterAsync(
            RestaurantOwnerRegisterRequest request);
    }
}