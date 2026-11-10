
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Validation
{
    public class BookingValidator
    {
        public List<string> Validate(Booking booking)
        {
            var errors = new List<string>();

            // Customer ID must be greater than zero.
            if (booking.CustomerId <= 0)
            {
                errors.Add("CustomerId must be greater than 0.");
            }

            // Restaurant table ID must be greater than zero.
            if (booking.RestaurantTableId <= 0)
            {
                errors.Add("RestaurantTableId must be greater than 0.");
            }

            // Booking date is required.
            if (booking.BookingDate == default)
            {
                errors.Add("Booking date is required.");
            }

            // Start time must be earlier than end time.
            if (booking.StartTime >= booking.EndTime)
            {
                errors.Add("Start time must be earlier than end time.");
            }

            // Number of guests must be between 1 and 100.
            if (booking.NumberOfGuests < 1 ||
                booking.NumberOfGuests > 100)
            {
                errors.Add("Number of guests must be between 1 and 100.");
            }

            // Special request is optional, maximum 1000 characters.
            if (!string.IsNullOrWhiteSpace(booking.SpecialRequest) &&
                booking.SpecialRequest.Length > 1000)
            {
                errors.Add("Special request cannot exceed 1000 characters.");
            }

            return errors;
        }
    }
}
