using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Services
{
    public interface ICuisineService
    {
        Task<List<Cuisine>> GetAllActiveAsync();
    }
}