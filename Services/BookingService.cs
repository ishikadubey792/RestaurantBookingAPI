
using RestaurantBookingAPI.Exceptions;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.IServices;
using RestaurantBookingAPI.Models;
using RestaurantBookingAPI.Validation;

namespace RestaurantBookingAPI.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IRestaurantTableRepository _tableRepository;
        private readonly IOperatingHourRepository _operatingHourRepository;
        private readonly ILogger<BookingService> _logger;
        private readonly BookingValidator _validator;
        private readonly CheckAvailabilityValidator _availabilityValidator;

        // Constructor: Injects all required repositories, validators and logger.
        public BookingService(
            IBookingRepository bookingRepository,
            ICustomerRepository customerRepository,
            IRestaurantTableRepository tableRepository,
            IOperatingHourRepository operatingHourRepository,
            ILogger<BookingService> logger,
            BookingValidator validator,
            CheckAvailabilityValidator availabilityValidator)
        {
            _bookingRepository = bookingRepository;
            _customerRepository = customerRepository;
            _tableRepository = tableRepository;
            _operatingHourRepository = operatingHourRepository;
            _logger = logger;
            _validator = validator;
            _availabilityValidator = availabilityValidator;
        }

        // Retrieves all bookings.
        // Access to this operation must be restricted at the Controller level.
        public async Task<List<Booking>> GetAllAsync()
        {
            return await _bookingRepository.GetAllAsync();
        }

        // Retrieves a booking only if it belongs to the specified customer.
        // Returns null if the booking does not exist or belongs to another customer.
        public async Task<Booking?> GetByIdAsync(int id, int customerId)
        {
            var booking = await _bookingRepository.GetByIdAsync(id);

            if (booking == null || booking.CustomerId != customerId)
            {
                return null;
            }

            return booking;
        }

        // Retrieves all bookings belonging to the specified customer.
        public async Task<List<Booking>> GetByCustomerIdAsync(int customerId)
        {
            return await _bookingRepository.GetByCustomerIdAsync(customerId);
        }

        // Retrieves available restaurant tables.
        // This operation remains public for restaurant discovery and availability checking.
        public async Task<List<RestaurantTable>> GetAvailableTablesAsync(
            CheckAvailabilityModel request)
        {
            var errors = _availabilityValidator.Validate(request);

            if (errors.Any())
            {
                throw new BadRequestException(string.Join(" ", errors));
            }

            var tables = await _tableRepository.GetAvailableTablesAsync(
                request.BookingDate,
                request.StartTime,
                request.EndTime,
                request.NumberOfGuests);

            return tables;
        }

        // Creates a booking for the authenticated customer.
        // CustomerId comes from the backend, not from the request body.

        public async Task<Booking> CreateAsync(
            CreateBookingRequest request,
            int customerId)
        {
            // Create a Booking entity using the request and authenticated CustomerId.
            var booking = new Booking
            {
                CustomerId = customerId,
                RestaurantTableId = request.RestaurantTableId,
                BookingDate = request.BookingDate,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                NumberOfGuests = request.NumberOfGuests,
                SpecialRequest = request.SpecialRequest,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            // Validate the booking details.
            var errors = _validator.Validate(booking);

            if (errors.Any())
            {
                throw new BadRequestException(string.Join(" ", errors));
            }

            _logger.LogInformation(
                "Creating booking for CustomerId: {CustomerId}, TableId: {TableId}, Date: {BookingDate}",
                customerId,
                booking.RestaurantTableId,
                booking.BookingDate);

            // Verify that the authenticated customer exists.
            var customer = await _customerRepository.GetByIdAsync(customerId);

            if (customer == null)
            {
                throw new NotFoundException("Customer not found.");
            }

            // Verify that the selected restaurant table exists.
            var table = await _tableRepository.GetByIdAsync(
                booking.RestaurantTableId);

            if (table == null)
            {
                throw new NotFoundException("Restaurant table not found.");
            }

            // Check whether the table can accommodate the requested guests.
            if (booking.NumberOfGuests > table.Capacity)
            {
                throw new BadRequestException(
                    $"Table capacity is {table.Capacity}. " +
                    $"You cannot book this table for {booking.NumberOfGuests} guests.");
            }

            // Retrieve the restaurant's operating hours.
            var dayOfWeek = booking.BookingDate.DayOfWeek.ToString();

            var operatingHours = await _operatingHourRepository
                .GetByRestaurantIdAsync(table.RestaurantId);

            var todayHours = operatingHours
                .FirstOrDefault(x => x.DayOfWeek == dayOfWeek);

            if (todayHours == null || todayHours.IsClosed)
            {
                throw new BadRequestException(
                    "Restaurant is closed on the selected day.");
            }

            if (!todayHours.OpenTime.HasValue ||
                !todayHours.CloseTime.HasValue)
            {
                throw new BadRequestException(
                    "Operating hours are not configured for the selected day.");
            }

            // Ensure the requested time falls within restaurant operating hours.
            if (booking.StartTime < todayHours.OpenTime.Value ||
                booking.EndTime > todayHours.CloseTime.Value)
            {
                throw new BadRequestException(
                    $"Restaurant is open from {todayHours.OpenTime.Value} " +
                    $"to {todayHours.CloseTime.Value}.");
            }

            // Check whether another active booking conflicts with this time.
            var hasConflict = await _bookingRepository.HasConflictAsync(
                booking.RestaurantTableId,
                booking.BookingDate,
                booking.StartTime,
                booking.EndTime);

            if (hasConflict)
            {
                throw new BookingConflictException(
                    "This table is already booked for the selected time.");
            }

            // Normalize the date before saving.
            booking.BookingDate = booking.BookingDate.Date;

            // Save the booking using the existing repository and stored procedure.
            var createdBooking = await _bookingRepository.AddAsync(booking);

            _logger.LogInformation(
                "Booking created successfully. BookingId: {BookingId}",
                createdBooking.Id);

            return createdBooking;
        }


        // Updates an existing booking only if it belongs to the authenticated customer.

        public async Task<bool> UpdateAsync(
            int id,
            UpdateBookingRequest request,
            int customerId)
        {
            // Fetch the existing booking.
            var existingBooking = await _bookingRepository.GetByIdAsync(id);

            // Verify ownership before allowing modification.
            if (existingBooking == null ||
                existingBooking.CustomerId != customerId)
            {
                return false;
            }

            // Build a validation entity while preserving system-controlled fields.
            var booking = new Booking
            {
                Id = existingBooking.Id,
                CustomerId = existingBooking.CustomerId,
                RestaurantTableId = existingBooking.RestaurantTableId,
                BookingDate = request.BookingDate,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                NumberOfGuests = request.NumberOfGuests,
                Status = existingBooking.Status,
                SpecialRequest = request.SpecialRequest,
                CreatedAt = existingBooking.CreatedAt
            };

            // Validate the updated booking details.
            var errors = _validator.Validate(booking);

            if (errors.Any())
            {
                throw new BadRequestException(string.Join(" ", errors));
            }

            // Fetch the table associated with the existing booking.
            var table = await _tableRepository.GetByIdAsync(
                existingBooking.RestaurantTableId);

            if (table == null)
            {
                throw new NotFoundException("Restaurant table not found.");
            }

            // Check guest capacity.
            if (booking.NumberOfGuests > table.Capacity)
            {
                throw new BadRequestException(
                    $"Table capacity is {table.Capacity}. " +
                    $"You cannot update this booking for {booking.NumberOfGuests} guests.");
            }

            // Check restaurant operating hours for the updated date.
            var dayOfWeek = booking.BookingDate.DayOfWeek.ToString();

            var operatingHours = await _operatingHourRepository
                .GetByRestaurantIdAsync(table.RestaurantId);

            var todayHours = operatingHours
                .FirstOrDefault(x => x.DayOfWeek == dayOfWeek);

            if (todayHours == null || todayHours.IsClosed)
            {
                throw new BadRequestException(
                    "Restaurant is closed on the selected day.");
            }

            if (!todayHours.OpenTime.HasValue ||
                !todayHours.CloseTime.HasValue)
            {
                throw new BadRequestException(
                    "Operating hours are not configured for the selected day.");
            }

            // Ensure the updated time falls within operating hours.
            if (booking.StartTime < todayHours.OpenTime.Value ||
                booking.EndTime > todayHours.CloseTime.Value)
            {
                throw new BadRequestException(
                    $"Restaurant is open from {todayHours.OpenTime.Value} " +
                    $"to {todayHours.CloseTime.Value}.");
            }

            // Check for conflicts, excluding the current booking.
            var hasConflict = await _bookingRepository.HasConflictAsync(
                existingBooking.RestaurantTableId,
                booking.BookingDate,
                booking.StartTime,
                booking.EndTime,
                id);

            if (hasConflict)
            {
                throw new BookingConflictException(
                    "This table is already booked for the selected time.");
            }

            // Update only customer-editable fields.
            existingBooking.BookingDate = booking.BookingDate.Date;
            existingBooking.StartTime = booking.StartTime;
            existingBooking.EndTime = booking.EndTime;
            existingBooking.NumberOfGuests = booking.NumberOfGuests;
            existingBooking.SpecialRequest = booking.SpecialRequest;

            return await _bookingRepository.UpdateAsync(existingBooking);
        }


        // Cancels a booking only if it belongs to the authenticated customer.
        public async Task<bool> CancelAsync(int id, int customerId)
        {
            var booking = await _bookingRepository.GetByIdAsync(id);

            // Verify ownership before cancellation.
            if (booking == null || booking.CustomerId != customerId)
            {
                return false;
            }

            // Avoid updating a booking that is already cancelled.
            if (booking.Status == "Cancelled")
            {
                return true;
            }

            booking.Status = "Cancelled";

            return await _bookingRepository.UpdateAsync(booking);
        }

        // Deletes a booking only if it belongs to the authenticated customer.
        public async Task<bool> DeleteAsync(int id, int customerId)
        {
            var booking = await _bookingRepository.GetByIdAsync(id);

            // Verify ownership before deletion.
            if (booking == null || booking.CustomerId != customerId)
            {
                return false;
            }

            return await _bookingRepository.DeleteAsync(id);
        }

        // Retrieves bookings belonging to restaurants owned by a specific RestaurantOwner.
        // Owner filtering is handled by the repository.
        public async Task<List<OwnerRestaurantBooking>>
            GetBookingsByRestaurantOwnerAsync(int ownerId)
        {
            return await _bookingRepository
                .GetBookingsByRestaurantOwnerAsync(ownerId);
        }
    }
}
