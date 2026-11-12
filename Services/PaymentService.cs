
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using RestaurantBookingAPI.Data;
using RestaurantBookingAPI.Exceptions;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.IServices;
using RestaurantBookingAPI.Models;
using RestaurantBookingAPI.Repositories;

namespace RestaurantBookingAPI.Services
{
    public class PaymentService : IPaymentService
    {
        private const decimal DemoPricePerGuest = 100m;

        private readonly IPaymentRepository _paymentRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly AppDbContext _context;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IBookingRepository bookingRepository,
            ICustomerRepository customerRepository,
            AppDbContext context,
            ILogger<PaymentService> logger)
        {
            _paymentRepository = paymentRepository;
            _bookingRepository = bookingRepository;
            _customerRepository = customerRepository;
            _context = context;
            _logger = logger;
        }

        private async Task SafeRollbackAsync(IDbContextTransaction transaction)
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch (Exception rollbackEx)
            {
                _logger.LogWarning(
                    rollbackEx,
                    "Transaction rollback skipped because the transaction was already closed or rolled back.");
            }
        }

        private static void EnsureBookingEligibleForPayment(
            Booking booking,
            string operationContext)
        {
            if (string.Equals(booking.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException(
                    $"Payment cannot be {operationContext} for a cancelled booking.");
            }

            if (string.Equals(booking.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException(
                    $"Payment cannot be {operationContext} for a completed booking.");
            }

            if (!string.Equals(booking.Status, "Pending", StringComparison.Ordinal))
            {
                throw new BadRequestException(
                    $"Payment can only be {operationContext} for a pending booking.");
            }
        }

        public async Task<Payment> InitiatePaymentAsync(
            int bookingId,
            int userId)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var booking =
                    await _bookingRepository.GetByIdForUpdateAsync(bookingId);

                if (booking == null)
                {
                    throw new NotFoundException("Booking not found.");
                }

                // Verify that the booking belongs to the logged-in user
                var customer =
                    await _customerRepository.GetByUserIdAsync(userId);

                if (customer == null || customer.Id != booking.CustomerId)
                {
                    throw new NotFoundException("Booking not found.");
                }

                EnsureBookingEligibleForPayment(booking, "initiated");

                if (booking.NumberOfGuests <= 0)
                {
                    throw new BadRequestException(
                        "Booking guest count must be greater than zero.");
                }

                // Prevent duplicate payment records
                var existingPayment =
                    await _paymentRepository.GetByBookingIdAsync(bookingId);

                if (existingPayment != null)
                {
                    throw new BadRequestException(
                        "A payment record already exists for this booking.");
                }

                // ₹100 per guest (server-calculated)
                decimal amount = booking.NumberOfGuests * DemoPricePerGuest;

                var payment = new Payment
                {
                    BookingId = bookingId,
                    Amount = amount,
                    PaymentMethod = "Demo",
                    Status = "Pending",
                    TransactionId = null,
                    PaidAt = null,
                    CreatedAt = DateTime.UtcNow
                };

                var created = await _paymentRepository.AddAsync(payment);

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Payment {PaymentId} initiated for BookingId {BookingId} by CustomerId {CustomerId}.",
                    created.Id,
                    bookingId,
                    customer.Id);

                return created;
            }
            catch
            {
                await SafeRollbackAsync(transaction);
                throw;
            }
        }

        public async Task<Payment?> GetPaymentByIdAsync(
            int paymentId,
            int userId)
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId);

            if (payment == null)
            {
                return null;
            }

            var customer = await _customerRepository.GetByUserIdAsync(userId);
            var booking = await _bookingRepository.GetByIdAsync(payment.BookingId);

            if (customer == null ||
                booking == null ||
                customer.Id != booking.CustomerId)
            {
                return null;
            }

            return payment;
        }

        public async Task<List<Payment>> GetPaymentHistoryAsync(int userId)
        {
            return await _paymentRepository.GetByUserIdAsync(userId);
        }

        public async Task<Payment> ProcessDemoPaymentAsync(
            int paymentId,
            int userId,
            bool simulateSuccess)
        {
            // Initial lookup resolves the immutable BookingId so locks are always
            // acquired in canonical order: Booking -> Payments.
            var paymentLookup = await _paymentRepository.GetByIdAsync(paymentId);

            if (paymentLookup == null)
            {
                throw new NotFoundException("Payment not found.");
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var booking = await _bookingRepository
                    .GetByIdForUpdateAsync(paymentLookup.BookingId);

                if (booking == null)
                {
                    throw new NotFoundException("Booking not found.");
                }

                var customer =
                    await _customerRepository.GetByUserIdAsync(userId);

                if (customer == null || customer.Id != booking.CustomerId)
                {
                    throw new NotFoundException("Payment not found.");
                }

                var payment =
                    await _paymentRepository.GetByIdForUpdateAsync(paymentId);

                if (payment == null)
                {
                    throw new NotFoundException("Payment not found.");
                }

                if (payment.BookingId != booking.Id)
                {
                    throw new BadRequestException(
                        "Payment does not match the associated booking.");
                }

                if (string.Equals(payment.Status, "Success", StringComparison.OrdinalIgnoreCase))
                {
                    throw new BadRequestException(
                        "This payment has already succeeded.");
                }

                if (!string.Equals(payment.Status, "Pending", StringComparison.Ordinal) &&
                    !string.Equals(payment.Status, "Failed", StringComparison.Ordinal))
                {
                    throw new BadRequestException(
                        "This payment cannot be processed in its current state.");
                }

                EnsureBookingEligibleForPayment(booking, "processed");

                if (simulateSuccess)
                {
                    payment.Status = "Success";
                    payment.PaidAt = DateTime.UtcNow;
                    payment.TransactionId = $"DEMO-{Guid.NewGuid():N}";
                    booking.Status = "Confirmed";
                }
                else
                {
                    payment.Status = "Failed";
                    payment.PaidAt = null;
                    payment.TransactionId = null;
                }

                var updatedPayment =
                    await _paymentRepository.UpdateAsync(payment);

                if (simulateSuccess)
                {
                    var bookingUpdated =
                        await _bookingRepository.UpdateAsync(booking);

                    if (!bookingUpdated)
                    {
                        throw new NotFoundException("Booking not found.");
                    }
                }

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Demo payment {PaymentId} processed with status {Status} for BookingId {BookingId}.",
                    updatedPayment.Id,
                    updatedPayment.Status,
                    updatedPayment.BookingId);

                return updatedPayment;
            }
            catch (SqlException ex) when (ex.Number == 50002)
            {
                await SafeRollbackAsync(transaction);
                throw new BookingConflictException(
                    "This table is already booked for the selected time.");
            }
            catch
            {
                await SafeRollbackAsync(transaction);
                throw;
            }
        }
    }
}
