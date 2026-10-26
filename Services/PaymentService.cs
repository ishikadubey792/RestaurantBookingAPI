
using RestaurantBookingAPI.Exceptions;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.IServices;
using RestaurantBookingAPI.Models;
using RestaurantBookingAPI.Repositories;
using Microsoft.EntityFrameworkCore;
using RestaurantBookingAPI.Data;

namespace RestaurantBookingAPI.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IBookingRepository _bookingRepository;

        private readonly ICustomerRepository _customerRepository;

        private readonly AppDbContext _context;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IBookingRepository bookingRepository,
            ICustomerRepository customerRepository,
            AppDbContext context)
        {
            _paymentRepository = paymentRepository;
            _bookingRepository = bookingRepository;
            _customerRepository = customerRepository;
            _context = context;
        }


        public async Task<Payment> InitiatePaymentAsync(
            int bookingId,
            int userId)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);

            if (booking == null)
            {
                throw new NotFoundException("Booking not found.");
            }

            // Verify that the booking belongs to the logged-in user
            var customer = await _customerRepository.GetByUserIdAsync(userId);

            if (customer == null || customer.Id != booking.CustomerId)
            {
                throw new NotFoundException("Booking not found.");
            }

            // Prevent duplicate payment records
            var existingPayment =
                await _paymentRepository.GetByBookingIdAsync(bookingId);

            if (existingPayment != null)
            {
                throw new BadRequestException(
                    "A payment record already exists for this booking.");
            }

            // ₹100 per guest
            decimal amount = booking.NumberOfGuests * 100m;

            var payment = new Payment
            {
                BookingId = bookingId,
                Amount = amount,
                PaymentMethod = "Demo",
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            return await _paymentRepository.AddAsync(payment);
        }

        public async Task<Payment?> GetPaymentByIdAsync(int paymentId)
        {
            return await _paymentRepository.GetByIdAsync(paymentId);
        }

        public async Task<List<Payment>> GetPaymentHistoryAsync(int userId)
        {
            return await _paymentRepository.GetByUserIdAsync(userId);
        }


        public async Task<Payment> ProcessDemoPaymentAsync(
            int paymentId,
            bool simulateSuccess)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var payment = await _paymentRepository.GetByIdAsync(paymentId);

                if (payment == null)
                {
                    throw new NotFoundException("Payment not found.");
                }

                if (payment.Status == "Success")
                {
                    throw new BadRequestException(
                        "This payment has already succeeded.");
                }

                if (payment.Status != "Pending" && payment.Status != "Failed")
                {
                    throw new BadRequestException(
                        "This payment cannot be processed in its current state.");
                }

                payment.Status = simulateSuccess ? "Success" : "Failed";

                if (simulateSuccess)
                {
                    payment.PaidAt = DateTime.UtcNow;
                    payment.TransactionId = $"DEMO-{Guid.NewGuid():N}";

                    var booking = await _bookingRepository
                        .GetByIdAsync(payment.BookingId);

                    if (booking == null)
                    {
                        throw new NotFoundException("Booking not found.");
                    }

                    booking.Status = "Confirmed";

                    await _bookingRepository.UpdateAsync(booking);
                }
                else
                {
                    payment.PaidAt = null;
                    payment.TransactionId = null;
                }

                await _paymentRepository.UpdateAsync(payment);

                await transaction.CommitAsync();

                return payment;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

    }
}
