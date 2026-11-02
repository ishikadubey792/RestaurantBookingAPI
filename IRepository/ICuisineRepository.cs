using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Repositories
{
    public interface ICuisineRepository
    {
        Task<List<Cuisine>> GetAllActiveAsync();
        Task<bool> AreAllActiveAsync(List<int> cuisineIds);
    }
}