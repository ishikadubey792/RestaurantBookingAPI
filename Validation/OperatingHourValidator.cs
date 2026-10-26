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

            if (operatingHour.IsClosed)
            {
                // Restaurant closed hai,
                // isliye OpenTime/CloseTime optional hain.
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

            if (operatingHour.OpenTime.HasValue &&
                operatingHour.CloseTime.HasValue &&
                operatingHour.OpenTime.Value >=
                operatingHour.CloseTime.Value)
            {
                errors.Add(
                    "Open time must be earlier than close time.");
            }

            return errors;
        }
    }
}