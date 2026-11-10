
using System.ComponentModel.DataAnnotations;

namespace RestaurantBookingAPI.Models
{
    // Contains only the fields a customer is allowed to update.
    public class UpdateBookingRequest
    {
        // Booking date cannot be in the past.
        [Required(ErrorMessage = "Booking date is required.")]
        [CustomValidation(typeof(UpdateBookingRequest), nameof(ValidateBookingDate))]
        public DateTime BookingDate { get; set; }

        // Start time of the booking.
        public TimeSpan StartTime { get; set; }

        // End time of the booking.
        public TimeSpan EndTime { get; set; }

        // Number of guests must be between 1 and 20.
        [Range(1, 20, ErrorMessage = "Number of guests must be between 1 and 20.")]
        public int NumberOfGuests { get; set; }

        // Special request is optional, maximum 1000 characters.
        [StringLength(1000, ErrorMessage = "Special request cannot exceed 1000 characters.")]
        public string? SpecialRequest { get; set; }

        // Custom validation for booking date.
        public static ValidationResult? ValidateBookingDate(DateTime date)
        {
            if (date.Date < DateTime.Today)
            {
                return new ValidationResult("Booking date cannot be in the past.");
            }

            return ValidationResult.Success;
        }
    }
}
