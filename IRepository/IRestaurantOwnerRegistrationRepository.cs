using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Repositories
{
    public interface IRestaurantOwnerRegistrationRepository
    {
        Task<Restaurant> RegisterAsync(
            User user,
            Restaurant restaurant,
            List<int> cuisineIds
        );
    }
}