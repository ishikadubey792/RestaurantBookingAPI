
using Microsoft.EntityFrameworkCore;
using RestaurantBookingAPI.Data;
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

        public async Task UpdateAsync(Payment payment)
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

            if (!results.Any())
            {
                throw new KeyNotFoundException(
                    $"Payment with ID {payment.Id} was not found.");
            }
        }
    }
}
