using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Validation
{
    public class CustomerValidator
    {
        public List<string> Validate(Customer customer)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(customer.Name))
            {
                errors.Add("Customer name is required.");
            }
            else if (customer.Name.Length > 100)
            {
                errors.Add("Customer name cannot exceed 100 characters.");
            }

            if (string.IsNullOrWhiteSpace(customer.Email))
            {
                errors.Add("Customer email is required.");
            }
            else
            {
                if (customer.Email.Length > 150)
                {
                    errors.Add("Customer email cannot exceed 150 characters.");
                }

                if (!customer.Email.Contains("@"))
                {
                    errors.Add("Customer email is not valid.");
                }
            }

            if (string.IsNullOrWhiteSpace(customer.Phone))
            {
                errors.Add("Customer phone is required.");
            }
            else if (customer.Phone.Length > 20)
            {
                errors.Add("Customer phone cannot exceed 20 characters.");
            }

            return errors;
        }
    }
}