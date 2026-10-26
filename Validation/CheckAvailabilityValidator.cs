using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Validation
{
    public class CheckAvailabilityValidator
    {
        public List<string> Validate(
            CheckAvailabilityModel request)
        {
            var errors = new List<string>();

            if (request.BookingDate == default)
            {
                errors.Add("Booking date is required.");
            }

            if (request.StartTime >= request.EndTime)
            {
                errors.Add(
                    "Start time must be earlier than end time.");
            }

            if (request.NumberOfGuests < 1 ||
                request.NumberOfGuests > 20)
            {
                errors.Add(
                    "Number of guests must be between 1 and 20.");
            }

            return errors;
        }
    }
}