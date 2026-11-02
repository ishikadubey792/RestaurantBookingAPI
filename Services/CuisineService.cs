using RestaurantBookingAPI.Models;
using RestaurantBookingAPI.Repositories;

namespace RestaurantBookingAPI.Services
{
    public class CuisineService : ICuisineService
    {
        private readonly ICuisineRepository _repository;

        public CuisineService(ICuisineRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Cuisine>> GetAllActiveAsync()
        {
            return await _repository.GetAllActiveAsync();
        }
    }
}