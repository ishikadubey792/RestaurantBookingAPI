using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.IRepository
{
    public interface IBookingRepository
    {
        Task<List<Booking>> GetAllAsync();

        Task<Booking?> GetByIdAsync(int id);

        Task<List<Booking>> GetByCustomerIdAsync(int customerId);

        Task<List<Booking>> GetByTableAndDateAsync(
            int restaurantTableId,
            DateTime bookingDate);

        Task<bool> HasConflictAsync(
            int restaurantTableId,
            DateTime bookingDate,
            TimeSpan startTime,
            TimeSpan endTime,
            int? excludeBookingId = null);

        Task<Booking> AddAsync(Booking booking);

        Task<bool> UpdateAsync(Booking booking);

        Task<bool> CancelAsync(int id);

        Task<bool> DeleteAsync(int id);

        Task<List<OwnerRestaurantBooking>>
    GetBookingsByRestaurantOwnerAsync(int ownerId);
    }
}