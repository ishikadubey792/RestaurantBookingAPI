
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Validation
{
    public class OperatingHourValidator
    {
        public List<string> Validate(OperatingHour operatingHour)
        {
            var errors = new List<string>();

            if (operatingHour.RestaurantId <= 0)
            {
                errors.Add(
                    "RestaurantId must be greater than 0.");
            }

            if (string.IsNullOrWhiteSpace(
                operatingHour.DayOfWeek))
            {
                errors.Add(
                    "Day of week is required.");
            }
            else
            {
                string[] validDays =
                {
                    "Monday",
                    "Tuesday",
                    "Wednesday",
                    "Thursday",
                    "Friday",
                    "Saturday",
                    "Sunday"
                };

                if (!validDays.Contains(
                    operatingHour.DayOfWeek,
                    StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add(
                        "Day of week is not valid.");
                }
            }

            // Closed day does not require opening or closing time.
            if (operatingHour.IsClosed)
            {
                return errors;
            }

            if (!operatingHour.OpenTime.HasValue)
            {
                errors.Add(
                    "Open time is required when restaurant is open.");
            }

            if (!operatingHour.CloseTime.HasValue)
            {
                errors.Add(
                    "Close time is required when restaurant is open.");
            }

            // Validate time duration only when both values exist.
            if (operatingHour.OpenTime.HasValue &&
                operatingHour.CloseTime.HasValue)
            {
                var duration =
                    operatingHour.CloseTime.Value -
                    operatingHour.OpenTime.Value;

                if (duration <= TimeSpan.Zero)
                {
                    errors.Add(
                        "Open time must be earlier than close time.");
                }
                else if (duration < TimeSpan.FromHours(1))
                {
                    errors.Add(
                        "Operating hours must be at least 1 hour.");
                }
            }

            return errors;
        }
    }
}
