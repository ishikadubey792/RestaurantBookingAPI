
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RestaurantBookingAPI.Data;
using RestaurantBookingAPI.Exceptions;
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly AppDbContext _context;

        public PaymentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Payment> AddAsync(Payment payment)
        {
            try
            {
                var results = await _context.Payments
                    .FromSqlInterpolated($"""
                        EXEC dbo.CreatePayment
                            @BookingId = {payment.BookingId},
                            @Amount = {payment.Amount},
                            @PaymentMethod = {payment.PaymentMethod},
                            @Status = {payment.Status},
                            @TransactionId = {payment.TransactionId},
                            @PaidAt = {payment.PaidAt}
                        """)
                    .AsNoTracking()
                    .ToListAsync();

                var created = results.FirstOrDefault();

                if (created == null)
                {
                    throw new InvalidOperationException(
                        "Payment could not be created.");
                }

                return created;
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627 or 50018)
            {
                throw new BadRequestException(
                    "A payment record already exists for this booking.");
            }
            catch (SqlException ex) when (ex.Number == 50010)
            {
                throw new NotFoundException("Booking not found.");
            }
            catch (SqlException ex) when (ex.Number is >= 50011 and <= 50017)
            {
                throw new BadRequestException(ex.Message);
            }
        }

        public async Task<Payment?> GetByIdAsync(int id)
        {
            var results = await _context.Payments
                .FromSqlInterpolated($"""
                    EXEC dbo.GetPaymentById @Id = {id}
                    """)
                .AsNoTracking()
                .ToListAsync();

            return results.FirstOrDefault();
        }

        public async Task<Payment?> GetByIdForUpdateAsync(int id)
        {
            var results = await _context.Payments
                .FromSqlInterpolated($"""
                    EXEC dbo.GetPaymentByIdForUpdate @Id = {id}
                    """)
                .AsNoTracking()
                .ToListAsync();

            return results.FirstOrDefault();
        }

        public async Task<Payment?> GetByBookingIdAsync(int bookingId)
        {
            var results = await _context.Payments
                .FromSqlInterpolated($"""
                    EXEC dbo.GetPaymentByBookingId
                        @BookingId = {bookingId}
                    """)
                .AsNoTracking()
                .ToListAsync();

            return results.FirstOrDefault();
        }

        public async Task<List<Payment>> GetByUserIdAsync(int userId)
        {
            return await _context.Payments
                .FromSqlInterpolated($"""
                    EXEC dbo.GetPaymentsByUserId @UserId = {userId}
                    """)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Payment> UpdateAsync(Payment payment)
        {
            try
            {
                var results = await _context.Payments
                    .FromSqlInterpolated($"""
                        EXEC dbo.UpdatePayment
                            @Id = {payment.Id},
                            @BookingId = {payment.BookingId},
                            @Amount = {payment.Amount},
                            @PaymentMethod = {payment.PaymentMethod},
                            @Status = {payment.Status},
                            @TransactionId = {payment.TransactionId},
                            @PaidAt = {payment.PaidAt}
                        """)
                    .AsNoTracking()
                    .ToListAsync();

                var updated = results.FirstOrDefault();

                if (updated == null)
                {
                    throw new KeyNotFoundException(
                        $"Payment with ID {payment.Id} was not found.");
                }

                return updated;
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627)
            {
                throw new BadRequestException(
                    "A duplicate transaction reference occurred. Please retry.");
            }
            catch (SqlException ex) when (ex.Number is >= 50021 and <= 50028)
            {
                throw new BadRequestException(ex.Message);
            }
        }
    }
}
