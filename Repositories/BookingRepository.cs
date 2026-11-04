using Microsoft.EntityFrameworkCore;
using RestaurantBookingAPI.Data;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly AppDbContext _context;

        public BookingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Booking>> GetAllAsync()
        {
            return await _context.Bookings
                .FromSqlRaw("EXEC GetAllBookings")
                .ToListAsync();
        }

        public async Task<Booking?> GetByIdAsync(int id)
        {
            var bookings = await _context.Bookings
                .FromSqlInterpolated($"EXEC GetBookingById @Id = {id}")
                .ToListAsync();

            return bookings.FirstOrDefault();
        }

        public async Task<List<Booking>> GetByCustomerIdAsync(int customerId)
        {
            return await _context.Bookings
                .FromSqlInterpolated($"EXEC GetBookingsByCustomer @CustomerId = {customerId}")
                .ToListAsync();
        }


        public async Task<List<Booking>> GetByTableAndDateAsync(
            int restaurantTableId,
            DateTime bookingDate)
        {
            return await _context.Bookings
                .FromSqlInterpolated(
                    $"EXEC dbo.GetBookingsByTableAndDate @RestaurantTableId = {restaurantTableId}, @BookingDate = {bookingDate.Date}")
                .ToListAsync();
        }




        public async Task<bool> HasConflictAsync(
            int restaurantTableId,
            DateTime bookingDate,
            TimeSpan startTime,
            TimeSpan endTime,
            int? excludeBookingId = null)
        {
            var results = await _context.Database
                .SqlQuery<bool>($"""
            EXEC dbo.CheckBookingConflict
                @RestaurantTableId = {restaurantTableId},
                @BookingDate = {bookingDate.Date},
                @StartTime = {startTime},
                @EndTime = {endTime},
                @ExcludeBookingId = {excludeBookingId}
            """)
                .ToListAsync();

            return results.FirstOrDefault();
        }





        public async Task<Booking> AddAsync(Booking booking)
        {
            var bookings = await _context.Bookings
                .FromSqlInterpolated($"""
            EXEC dbo.CreateBooking
                @CustomerId = {booking.CustomerId},
                @RestaurantTableId = {booking.RestaurantTableId},
                @BookingDate = {booking.BookingDate.Date},
                @StartTime = {booking.StartTime},
                @EndTime = {booking.EndTime},
                @NumberOfGuests = {booking.NumberOfGuests},
                @Status = {booking.Status},
                @SpecialRequest = {booking.SpecialRequest}
            """)
                .ToListAsync();

            return bookings.First();
        }




        public async Task<bool> UpdateAsync(Booking booking)
        {
            var bookings = await _context.Bookings
                .FromSqlInterpolated($"""
            EXEC dbo.UpdateBooking
                @Id = {booking.Id},
                @BookingDate = {booking.BookingDate.Date},
                @StartTime = {booking.StartTime},
                @EndTime = {booking.EndTime},
                @NumberOfGuests = {booking.NumberOfGuests},
                @Status = {booking.Status},
                @SpecialRequest = {booking.SpecialRequest}
            """)
                .ToListAsync();

            return bookings.Any();
        }


        public async Task<bool> CancelAsync(int id)
        {
            var bookings = await _context.Bookings
                .FromSqlInterpolated(
                    $"EXEC dbo.CancelBooking @Id = {id}")
                .ToListAsync();

            return bookings.Any();
        }


        public async Task<bool> DeleteAsync(int id)
        {
            var result = await _context.Database
                .SqlQueryRaw<bool>(
                    "EXEC dbo.DeleteBooking @Id = {0}", id)
                .ToListAsync();

            return result.FirstOrDefault();
        }


        public async Task<List<OwnerRestaurantBooking>>
    GetBookingsByRestaurantOwnerAsync(int ownerId)
        {
            var bookings = await (
                from booking in _context.Bookings.AsNoTracking()

                join table in _context.RestaurantTables.AsNoTracking()
                    on booking.RestaurantTableId equals table.Id

                join restaurant in _context.Restaurants.AsNoTracking()
                    on table.RestaurantId equals restaurant.Id

                join customer in _context.Customers.AsNoTracking()
                    on booking.CustomerId equals customer.Id

                where restaurant.OwnerUserId == ownerId

                orderby booking.BookingDate descending

                select new OwnerRestaurantBooking
                {
                    BookingId = booking.Id,
                    RestaurantId = restaurant.Id,
                    RestaurantName = restaurant.Name,

                    CustomerName = customer.Name,
                    CustomerEmail = customer.Email,
                    CustomerPhone = customer.Phone,

                    BookingDate = booking.BookingDate,
                    StartTime = booking.StartTime,
                    EndTime = booking.EndTime,
                    NumberOfGuests = booking.NumberOfGuests,

                    Status = booking.Status,
                    SpecialRequest = booking.SpecialRequest,
                    CreatedAt = booking.CreatedAt
                }
            ).ToListAsync();

            return bookings;
        }

    }
}