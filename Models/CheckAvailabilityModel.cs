using System.ComponentModel.DataAnnotations;

namespace RestaurantBookingAPI.Models
{
    public class CheckAvailabilityModel
    {
        [Required]
        public DateTime BookingDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        [Range(1, 20)]
        public int NumberOfGuests { get; set; }
    }
}