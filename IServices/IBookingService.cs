
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.IServices
{
    public interface IBookingService
    {
        // Retrieves all bookings.
        // This method will be restricted from public access at the Controller level.
        Task<List<Booking>> GetAllAsync();


        // Retrieves a specific booking.
        // customerId is used to verify that the booking belongs to the authenticated customer.
        Task<Booking?> GetByIdAsync(int id, int customerId);


        // Retrieves all bookings belonging to a specific customer.
        // customerId must be resolved from the authenticated user's JWT.
        Task<List<Booking>> GetByCustomerIdAsync(int customerId);


        // Creates a booking using customer-provided booking details.
        Task<Booking> CreateAsync(
            CreateBookingRequest request,
            int customerId);

        // Updates an existing booking using customer-editable details.
        Task<bool> UpdateAsync(
            int id,
            UpdateBookingRequest request,
            int customerId);


        // Cancels an existing booking.
        // Only the customer who owns the booking can cancel it.
        Task<bool> CancelAsync(int id, int customerId);


        // Deletes an existing booking.
        // Only the customer who owns the booking can delete it.
        Task<bool> DeleteAsync(int id, int customerId);


        // Retrieves available restaurant tables based on date, time, and guest count.
        // This method is public and does not require customer authentication.
        Task<List<RestaurantTable>> GetAvailableTablesAsync(
            CheckAvailabilityModel request);


        // Retrieves bookings for all restaurants owned by a specific RestaurantOwner.
        // ownerId comes from the authenticated owner's JWT.
        Task<List<OwnerRestaurantBooking>>
            GetBookingsByRestaurantOwnerAsync(int ownerId);
    }
}
