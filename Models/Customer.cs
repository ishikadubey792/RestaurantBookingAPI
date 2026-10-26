
namespace RestaurantBookingAPI.Models
{
    public class Customer
    {
        public int Id { get; set; }

        // Links Customer to the registered User
        public int? UserId { get; set; }

        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }
}
