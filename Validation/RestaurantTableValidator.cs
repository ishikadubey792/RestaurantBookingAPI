using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Validation
{
    public class RestaurantTableValidator
    {
        public List<string> Validate(RestaurantTable table)
        {
            var errors = new List<string>();

            if (table.RestaurantId <= 0)
            {
                errors.Add("RestaurantId must be greater than 0.");
            }

            if (table.TableNumber <= 0)
            {
                errors.Add("Table number must be greater than 0.");
            }

            if (table.Capacity <= 0)
            {
                errors.Add("Table capacity must be greater than 0.");
            }

            if (string.IsNullOrWhiteSpace(table.Status))
            {
                errors.Add("Table status is required.");
            }
            else if (table.Status.Length > 20)
            {
                errors.Add("Table status cannot exceed 20 characters.");
            }

            return errors;
        }
    }
}