using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RestaurantBookingAPI.Data;
using RestaurantBookingAPI.Exceptions;
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

        public async Task<Booking?> GetByIdForUpdateAsync(int id)
        {
            var bookings = await _context.Bookings
                .FromSqlInterpolated($"EXEC dbo.GetBookingByIdForUpdate @Id = {id}")
                .AsNoTracking()
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
            try
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
                    .AsNoTracking()
                    .ToListAsync();

                return bookings.Any();
            }
            catch (SqlException ex) when (ex.Number is >= 50031 and <= 50035)
            {
                throw new BadRequestException(ex.Message);
            }
        }


        public async Task<bool> CancelAsync(int id)
        {
            try
            {
                var bookings = await _context.Bookings
                    .FromSqlInterpolated(
                        $"EXEC dbo.CancelBooking @Id = {id}")
                    .AsNoTracking()
                    .ToListAsync();

                return bookings.Any();
            }
            catch (SqlException ex) when (ex.Number is >= 50031 and <= 50035)
            {
                throw new BadRequestException(ex.Message);
            }
        }


        public async Task<bool> DeleteAsync(int id)
        {
            var result = await _context.Database
                .SqlQueryRaw<bool>(
                    "EXEC dbo.DeleteBooking @Id = {0}", id)
                .ToListAsync();

            return result.FirstOrDefault();
        }


        public async Task<List<OwnerRestaurantBooking>> GetBookingsByRestaurantOwnerAsync(
            int ownerId,
            int? restaurantId = null,
            string? status = null,
            DateTime? bookingDate = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var query = from booking in _context.Bookings.AsNoTracking()
                        join table in _context.RestaurantTables.AsNoTracking()
                            on booking.RestaurantTableId equals table.Id
                        join restaurant in _context.Restaurants.AsNoTracking()
                            on table.RestaurantId equals restaurant.Id
                        join customer in _context.Customers.AsNoTracking()
                            on booking.CustomerId equals customer.Id
                        where restaurant.OwnerUserId == ownerId
                        select new { booking, table, restaurant, customer };

            if (restaurantId.HasValue)
            {
                query = query.Where(x => x.restaurant.Id == restaurantId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.booking.Status == status);
            }

            if (bookingDate.HasValue)
            {
                var bDate = bookingDate.Value.Date;
                query = query.Where(x => x.booking.BookingDate.Date == bDate);
            }

            if (fromDate.HasValue)
            {
                var fDate = fromDate.Value.Date;
                query = query.Where(x => x.booking.BookingDate.Date >= fDate);
            }

            if (toDate.HasValue)
            {
                var tDate = toDate.Value.Date;
                query = query.Where(x => x.booking.BookingDate.Date <= tDate);
            }

            var queryWithPayment = from item in query
                                   join p in _context.Payments.AsNoTracking()
                                       on item.booking.Id equals p.BookingId into payments
                                   from payment in payments.DefaultIfEmpty()
                                   orderby item.booking.BookingDate descending, item.booking.StartTime descending
                                   select new OwnerRestaurantBooking
                                   {
                                       BookingId = item.booking.Id,
                                       RestaurantId = item.restaurant.Id,
                                       RestaurantName = item.restaurant.Name,
                                       CustomerName = item.customer.Name,
                                       CustomerEmail = item.customer.Email,
                                       CustomerPhone = item.customer.Phone,
                                       BookingDate = item.booking.BookingDate,
                                       StartTime = item.booking.StartTime,
                                       EndTime = item.booking.EndTime,
                                       NumberOfGuests = item.booking.NumberOfGuests,
                                       Status = item.booking.Status,
                                       SpecialRequest = item.booking.SpecialRequest,
                                       CreatedAt = item.booking.CreatedAt,
                                       RestaurantTableId = item.table.Id,
                                       TableNumber = item.table.TableNumber,
                                       PaymentId = payment != null ? payment.Id : (int?)null,
                                       PaymentAmount = payment != null ? payment.Amount : (decimal?)null,
                                       PaymentStatus = payment != null ? payment.Status : null,
                                       PaymentMethod = payment != null ? payment.PaymentMethod : null,
                                       PaidAt = payment != null ? payment.PaidAt : null
                                   };

            return await queryWithPayment.ToListAsync();
        }

        public async Task<Booking?> GetByIdAndOwnerIdAsync(int bookingId, int ownerId)
        {
            var booking = await (
                from b in _context.Bookings.AsNoTracking()
                join table in _context.RestaurantTables.AsNoTracking()
                    on b.RestaurantTableId equals table.Id
                join restaurant in _context.Restaurants.AsNoTracking()
                    on table.RestaurantId equals restaurant.Id
                where b.Id == bookingId && restaurant.OwnerUserId == ownerId
                select b
            ).FirstOrDefaultAsync();

            return booking;
        }

    }
}