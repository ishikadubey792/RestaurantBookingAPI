
using System.ComponentModel.DataAnnotations;

namespace RestaurantBookingAPI.Models
{
    // Contains only the fields required when a customer creates a booking.
    public class CreateBookingRequest
    {
        // Restaurant table ID must be positive.
        [Range(1, int.MaxValue,
            ErrorMessage = "Please select a valid restaurant table.")]
        public int RestaurantTableId { get; set; }

        // Booking date is required and cannot be in the past.
        [Required(ErrorMessage = "Booking date is required.")]
        [CustomValidation(typeof(CreateBookingRequest),
            nameof(ValidateBookingDate))]
        public DateTime BookingDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        // Maximum 20 guests per online booking.
        [Range(1, 20,
            ErrorMessage = "Number of guests must be between 1 and 20.")]
        public int NumberOfGuests { get; set; }

        // Special request is optional, maximum 1000 characters.
        [StringLength(1000,
            ErrorMessage = "Special request cannot exceed 1000 characters.")]
        public string? SpecialRequest { get; set; }

        // Validates that the booking date is not in the past.
        public static ValidationResult? ValidateBookingDate(DateTime date)
        {
            if (date.Date < DateTime.Today)
            {
                return new ValidationResult(
                    "Booking date cannot be in the past.");
            }

            return ValidationResult.Success;
        }
    }
}
