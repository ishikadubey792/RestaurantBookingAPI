using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Validation
{
    public class RestaurantValidator
    {
        public List<string> Validate(Restaurant restaurant)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(restaurant.Name))
            {
                errors.Add("Restaurant name is required.");
            }
            else if (restaurant.Name.Length > 100)
            {
                errors.Add("Restaurant name cannot exceed 100 characters.");
            }

            if (string.IsNullOrWhiteSpace(restaurant.Address))
            {
                errors.Add("Restaurant address is required.");
            }
            else if (restaurant.Address.Length > 250)
            {
                errors.Add("Restaurant address cannot exceed 250 characters.");
            }

            if (string.IsNullOrWhiteSpace(restaurant.Phone))
            {
                errors.Add("Restaurant phone is required.");
            }
            else if (restaurant.Phone.Length > 20)
            {
                errors.Add("Restaurant phone cannot exceed 20 characters.");
            }

            if (!string.IsNullOrWhiteSpace(restaurant.Email))
            {
                if (restaurant.Email.Length > 150)
                {
                    errors.Add("Restaurant email cannot exceed 150 characters.");
                }

                if (!restaurant.Email.Contains("@"))
                {
                    errors.Add("Restaurant email is not valid.");
                }
            }

            return errors;
        }
    }
}