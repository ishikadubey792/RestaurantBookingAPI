using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.IServices
{
    public interface IBookingService
    {
        Task<List<Booking>> GetAllAsync();

        Task<Booking?> GetByIdAsync(int id);

        Task<List<Booking>> GetByCustomerIdAsync(int customerId);

        Task<Booking> CreateAsync(Booking booking);

        Task<bool> UpdateAsync(int id, Booking booking);

        Task<bool> CancelAsync(int id);


        Task<bool> DeleteAsync(int id);

        Task<List<RestaurantTable>> GetAvailableTablesAsync(
            CheckAvailabilityModel request);

        Task<List<OwnerRestaurantBooking>>
    GetBookingsByRestaurantOwnerAsync(int ownerId);
    }
}