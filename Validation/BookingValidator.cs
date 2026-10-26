using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Validation
{
    public class BookingValidator
    {
        public List<string> Validate(Booking booking)
        {
            var errors = new List<string>();

            if (booking.CustomerId <= 0)
            {
                errors.Add("CustomerId must be greater than 0.");
            }

            if (booking.RestaurantTableId <= 0)
            {
                errors.Add("RestaurantTableId must be greater than 0.");
            }

            if (booking.BookingDate == default)
            {
                errors.Add("Booking date is required.");
            }

            if (booking.StartTime >= booking.EndTime)
            {
                errors.Add(
                    "Start time must be earlier than end time.");
            }

            if (booking.NumberOfGuests < 1 ||
                booking.NumberOfGuests > 20)
            {
                errors.Add(
                    "Number of guests must be between 1 and 20.");
            }

            if (!string.IsNullOrWhiteSpace(booking.SpecialRequest) &&
                booking.SpecialRequest.Length > 500)
            {
                errors.Add(
                    "Special request cannot exceed 500 characters.");
            }

            return errors;
        }
    }
}