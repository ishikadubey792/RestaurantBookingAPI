using Microsoft.EntityFrameworkCore;
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

        public async Task<List<Booking>> GetAllAsync()
        {
            return await _bookingRepository.GetAllAsync();
        }

        public async Task<Booking?> GetByIdAsync(int id)
        {
            return await _bookingRepository.GetByIdAsync(id);
        }

        public async Task<List<Booking>> GetByCustomerIdAsync(
            int customerId)
        {
            return await _bookingRepository
                .GetByCustomerIdAsync(customerId);
        }

        public async Task<List<RestaurantTable>> GetAvailableTablesAsync(
            CheckAvailabilityModel request)
        {
            var errors = _availabilityValidator.Validate(request);

            if (errors.Any())
            {
                throw new BadRequestException(
                    string.Join(" ", errors));
            }

            // 2. Get available tables from repository
            var tables =
                await _tableRepository.GetAvailableTablesAsync(
                    request.BookingDate,
                    request.StartTime,
                    request.EndTime,
                    request.NumberOfGuests);

            return tables;
        }

        public async Task<Booking> CreateAsync(Booking booking)
        {

            var errors = _validator.Validate(booking);

            if (errors.Any())
            {
                throw new BadRequestException(
                    string.Join(" ", errors));
            }

            _logger.LogInformation(
                "Creating booking for CustomerId: {CustomerId}, TableId: {TableId}, Date: {BookingDate}",
                booking.CustomerId,
                booking.RestaurantTableId,
                booking.BookingDate);

            // 1. Check customer
            var customer = await _customerRepository
                .GetByIdAsync(booking.CustomerId);

            if (customer == null)
            {
                throw new NotFoundException(
                    "Customer not found.");
            }

            // 2. Check table
            var table = await _tableRepository
                .GetByIdAsync(booking.RestaurantTableId);

            if (table == null)
            {
                throw new NotFoundException(
                    "Restaurant table not found.");
            }

            // 3. Check guest capacity
            if (booking.NumberOfGuests > table.Capacity)
            {
                throw new BadRequestException(
                    $"Table capacity is {table.Capacity}. " +
                    $"You cannot book this table for " +
                    $"{booking.NumberOfGuests} guests.");
            }

            // 5. Check restaurant operating hours
            var dayOfWeek =
                booking.BookingDate.DayOfWeek.ToString();

            var operatingHours =
                await _operatingHourRepository
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

            if (booking.StartTime < todayHours.OpenTime.Value ||
                booking.EndTime > todayHours.CloseTime.Value)
            {
                throw new BadRequestException(
                    $"Restaurant is open from " +
                    $"{todayHours.OpenTime.Value} to " +
                    $"{todayHours.CloseTime.Value}.");
            }

            // 6. Check booking conflict
            var hasConflict =
                await _bookingRepository.HasConflictAsync(
                    booking.RestaurantTableId,
                    booking.BookingDate,
                    booking.StartTime,
                    booking.EndTime);

            if (hasConflict)
            {
                _logger.LogWarning(
                    "Booking conflict detected for TableId: {TableId}, Date: {BookingDate}, StartTime: {StartTime}, EndTime: {EndTime}",
                    booking.RestaurantTableId,
                    booking.BookingDate,
                    booking.StartTime,
                    booking.EndTime);

                throw new BookingConflictException(
                    "This table is already booked for the selected time.");
            }

            // 7. Set system-controlled values
            booking.BookingDate = booking.BookingDate.Date;
            booking.Status = "Pending";
            booking.CreatedAt = DateTime.Now;

            // 8. Save booking
            var createdBooking =
                await _bookingRepository.AddAsync(booking);

            _logger.LogInformation(
                "Booking created successfully. BookingId: {BookingId}",
                createdBooking.Id);

            return createdBooking;
        }

        public async Task<bool> UpdateAsync(
            int id,
            Booking booking)
        {
            var existingBooking =
                await _bookingRepository.GetByIdAsync(id);

            if (existingBooking == null)
            {
                return false;
            }

            var errors = _validator.Validate(booking);

            if (errors.Any())
            {
                throw new BadRequestException(
                    string.Join(" ", errors));
            }

            // Fetch table
            var table =
                await _tableRepository
                    .GetByIdAsync(existingBooking.RestaurantTableId);

            if (table == null)
            {
                throw new NotFoundException(
                    "Restaurant table not found.");
            }

            // Capacity check
            if (booking.NumberOfGuests > table.Capacity)
            {
                throw new BadRequestException(
                    $"Table capacity is {table.Capacity}. " +
                    $"You cannot update this booking for " +
                    $"{booking.NumberOfGuests} guests.");
            }

            

            // Check restaurant operating hours
            var dayOfWeek =
                booking.BookingDate.DayOfWeek.ToString();

            var operatingHours =
                await _operatingHourRepository
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

            if (booking.StartTime < todayHours.OpenTime.Value ||
                booking.EndTime > todayHours.CloseTime.Value)
            {
                throw new BadRequestException(
                    $"Restaurant is open from " +
                    $"{todayHours.OpenTime.Value} to " +
                    $"{todayHours.CloseTime.Value}.");
            }

            // Check table conflict
            var hasConflict =
                await _bookingRepository.HasConflictAsync(
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

            // Update only allowed fields
            existingBooking.BookingDate =
                booking.BookingDate.Date;

            existingBooking.StartTime =
                booking.StartTime;

            existingBooking.EndTime =
                booking.EndTime;

            existingBooking.NumberOfGuests =
                booking.NumberOfGuests;

            existingBooking.SpecialRequest =
                booking.SpecialRequest;

            return await _bookingRepository
                .UpdateAsync(existingBooking);
        }


        public async Task<bool> CancelAsync(int id)
        {
            var booking = await _bookingRepository.GetByIdAsync(id);

            if (booking == null)
            {
                return false;
            }

            if (booking.Status == "Cancelled")
            {
                return true;
            }

            booking.Status = "Cancelled";

            return await _bookingRepository.UpdateAsync(booking);
        }



        public async Task<bool> DeleteAsync(int id)
        {
            return await _bookingRepository.DeleteAsync(id);
        }
    }
}