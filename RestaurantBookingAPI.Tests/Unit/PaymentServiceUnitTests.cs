using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RestaurantBookingAPI.Data;
using RestaurantBookingAPI.Exceptions;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.Models;
using RestaurantBookingAPI.Repositories;
using RestaurantBookingAPI.Services;

namespace RestaurantBookingAPI.Tests.Unit;

public sealed class PaymentServiceUnitTests : IDisposable
{
    private readonly SqliteConnection _sqliteConnection;
    private readonly AppDbContext _dbContext;
    private readonly Mock<IPaymentRepository> _paymentRepositoryMock = new();
    private readonly Mock<IBookingRepository> _bookingRepositoryMock = new();
    private readonly Mock<ICustomerRepository> _customerRepositoryMock = new();
    private readonly PaymentService _service;

    public PaymentServiceUnitTests()
    {
        _sqliteConnection = new SqliteConnection("Data Source=:memory:");
        _sqliteConnection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;

        _dbContext = new AppDbContext(options);
        _dbContext.Database.EnsureCreated();

        _service = new PaymentService(
            _paymentRepositoryMock.Object,
            _bookingRepositoryMock.Object,
            _customerRepositoryMock.Object,
            _dbContext,
            NullLogger<PaymentService>.Instance);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _sqliteConnection.Dispose();
    }

    [Fact]
    public async Task InitiatePaymentAsync_ValidPendingBooking_CalculatesServerAmountAndCreatesPendingPayment()
    {
        var booking = CreateBooking(id: 10, customerId: 5, guests: 4, status: "Pending");
        var customer = new Customer { Id = 5, UserId = 42, Name = "Alice", Email = "alice@example.com", Phone = "123" };

        _bookingRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(booking);
        _customerRepositoryMock.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync(customer);
        _paymentRepositoryMock.Setup(r => r.GetByBookingIdAsync(10)).ReturnsAsync((Payment?)null);
        _paymentRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Payment>()))
            .ReturnsAsync((Payment p) =>
            {
                p.Id = 99;
                return p;
            });

        var result = await _service.InitiatePaymentAsync(bookingId: 10, userId: 42);

        Assert.Equal(99, result.Id);
        Assert.Equal(10, result.BookingId);
        Assert.Equal(400m, result.Amount);
        Assert.Equal("Demo", result.PaymentMethod);
        Assert.Equal("Pending", result.Status);
        Assert.Null(result.TransactionId);
        Assert.Null(result.PaidAt);
    }

    [Theory]
    [InlineData("Cancelled")]
    [InlineData("Completed")]
    [InlineData("Confirmed")]
    public async Task InitiatePaymentAsync_NonPendingBooking_ThrowsBadRequest(string status)
    {
        var booking = CreateBooking(id: 10, customerId: 5, guests: 2, status: status);
        var customer = new Customer { Id = 5, UserId = 42, Name = "Alice", Email = "alice@example.com", Phone = "123" };

        _bookingRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(booking);
        _customerRepositoryMock.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync(customer);

        await Assert.ThrowsAsync<BadRequestException>(() => _service.InitiatePaymentAsync(10, 42));
        _paymentRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task InitiatePaymentAsync_DuplicatePayment_ThrowsBadRequest()
    {
        var booking = CreateBooking(id: 10, customerId: 5, guests: 2, status: "Pending");
        var customer = new Customer { Id = 5, UserId = 42, Name = "Alice", Email = "alice@example.com", Phone = "123" };
        var existing = new Payment { Id = 1, BookingId = 10, Amount = 200m, Status = "Pending" };

        _bookingRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(booking);
        _customerRepositoryMock.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync(customer);
        _paymentRepositoryMock.Setup(r => r.GetByBookingIdAsync(10)).ReturnsAsync(existing);

        await Assert.ThrowsAsync<BadRequestException>(() => _service.InitiatePaymentAsync(10, 42));
        _paymentRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task InitiatePaymentAsync_BookingBelongsToAnotherCustomer_ThrowsNotFound()
    {
        var booking = CreateBooking(id: 10, customerId: 5, guests: 2, status: "Pending");
        var otherCustomer = new Customer { Id = 99, UserId = 77, Name = "Bob", Email = "bob@example.com", Phone = "123" };

        _bookingRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(booking);
        _customerRepositoryMock.Setup(r => r.GetByUserIdAsync(77)).ReturnsAsync(otherCustomer);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.InitiatePaymentAsync(10, 77));
        _paymentRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task ProcessDemoPaymentAsync_Success_SetsSuccessAndConfirmsBooking()
    {
        var booking = CreateBooking(id: 10, customerId: 5, guests: 3, status: "Pending");
        var customer = new Customer { Id = 5, UserId = 42, Name = "Alice", Email = "alice@example.com", Phone = "123" };
        var payment = new Payment { Id = 20, BookingId = 10, Amount = 300m, PaymentMethod = "Demo", Status = "Pending" };

        _paymentRepositoryMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(payment);
        _paymentRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(20)).ReturnsAsync(payment);
        _bookingRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(booking);
        _customerRepositoryMock.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync(customer);
        _paymentRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => p);
        _bookingRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Booking>())).ReturnsAsync(true);

        var result = await _service.ProcessDemoPaymentAsync(paymentId: 20, userId: 42, simulateSuccess: true);

        Assert.Equal("Success", result.Status);
        Assert.NotNull(result.PaidAt);
        Assert.StartsWith("DEMO-", result.TransactionId);
        Assert.Equal("Confirmed", booking.Status);
    }

    [Fact]
    public async Task ProcessDemoPaymentAsync_Failure_MarksFailedAndLeavesBookingPending()
    {
        var booking = CreateBooking(id: 10, customerId: 5, guests: 3, status: "Pending");
        var customer = new Customer { Id = 5, UserId = 42, Name = "Alice", Email = "alice@example.com", Phone = "123" };
        var payment = new Payment { Id = 20, BookingId = 10, Amount = 300m, PaymentMethod = "Demo", Status = "Pending" };

        _paymentRepositoryMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(payment);
        _paymentRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(20)).ReturnsAsync(payment);
        _bookingRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(booking);
        _customerRepositoryMock.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync(customer);
        _paymentRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => p);

        var result = await _service.ProcessDemoPaymentAsync(paymentId: 20, userId: 42, simulateSuccess: false);

        Assert.Equal("Failed", result.Status);
        Assert.Null(result.PaidAt);
        Assert.Null(result.TransactionId);
        Assert.Equal("Pending", booking.Status);
        _bookingRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task ProcessDemoPaymentAsync_RetryAfterFailure_SucceedsOnExistingPaymentRecord()
    {
        var booking = CreateBooking(id: 10, customerId: 5, guests: 2, status: "Pending");
        var customer = new Customer { Id = 5, UserId = 42, Name = "Alice", Email = "alice@example.com", Phone = "123" };
        var payment = new Payment { Id = 20, BookingId = 10, Amount = 200m, PaymentMethod = "Demo", Status = "Failed" };

        _paymentRepositoryMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(payment);
        _paymentRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(20)).ReturnsAsync(payment);
        _bookingRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(booking);
        _customerRepositoryMock.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync(customer);
        _paymentRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => p);
        _bookingRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Booking>())).ReturnsAsync(true);

        var result = await _service.ProcessDemoPaymentAsync(20, 42, simulateSuccess: true);

        Assert.Equal(20, result.Id);
        Assert.Equal("Success", result.Status);
        Assert.Equal("Confirmed", booking.Status);
        _paymentRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task ProcessDemoPaymentAsync_AlreadySucceeded_ThrowsBadRequest()
    {
        var booking = CreateBooking(id: 10, customerId: 5, guests: 2, status: "Confirmed");
        var customer = new Customer { Id = 5, UserId = 42, Name = "Alice", Email = "alice@example.com", Phone = "123" };
        var payment = new Payment
        {
            Id = 20,
            BookingId = 10,
            Amount = 200m,
            PaymentMethod = "Demo",
            Status = "Success",
            TransactionId = "DEMO-123",
            PaidAt = DateTime.UtcNow
        };

        _paymentRepositoryMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(payment);
        _paymentRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(20)).ReturnsAsync(payment);
        _bookingRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(booking);
        _customerRepositoryMock.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync(customer);

        await Assert.ThrowsAsync<BadRequestException>(() => _service.ProcessDemoPaymentAsync(20, 42, true));
        _paymentRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Payment>()), Times.Never);
        _bookingRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Booking>()), Times.Never);
    }

    [Theory]
    [InlineData("Cancelled", true)]
    [InlineData("Cancelled", false)]
    [InlineData("Completed", true)]
    [InlineData("Completed", false)]
    public async Task ProcessDemoPaymentAsync_CancelledOrCompletedBooking_ThrowsBadRequest(string bookingStatus, bool simulateSuccess)
    {
        var booking = CreateBooking(id: 10, customerId: 5, guests: 2, status: bookingStatus);
        var customer = new Customer { Id = 5, UserId = 42, Name = "Alice", Email = "alice@example.com", Phone = "123" };
        var payment = new Payment { Id = 20, BookingId = 10, Amount = 200m, PaymentMethod = "Demo", Status = "Pending" };

        _paymentRepositoryMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(payment);
        _paymentRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(20)).ReturnsAsync(payment);
        _bookingRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(booking);
        _customerRepositoryMock.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync(customer);

        await Assert.ThrowsAsync<BadRequestException>(() => _service.ProcessDemoPaymentAsync(20, 42, simulateSuccess));
        _paymentRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Payment>()), Times.Never);
        _bookingRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task ProcessDemoPaymentAsync_BookingUpdateFails_RollsBackTransaction()
    {
        var booking = CreateBooking(id: 10, customerId: 5, guests: 2, status: "Pending");
        var customer = new Customer { Id = 5, UserId = 42, Name = "Alice", Email = "alice@example.com", Phone = "123" };
        var payment = new Payment { Id = 20, BookingId = 10, Amount = 200m, PaymentMethod = "Demo", Status = "Pending" };

        _paymentRepositoryMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(payment);
        _paymentRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(20)).ReturnsAsync(payment);
        _bookingRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(booking);
        _customerRepositoryMock.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync(customer);
        _paymentRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => p);
        _bookingRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Booking>()))
            .ThrowsAsync(new InvalidOperationException("Simulated booking update failure"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ProcessDemoPaymentAsync(20, 42, true));
        Assert.Null(_dbContext.Database.CurrentTransaction);
    }

    [Fact]
    public async Task ProcessDemoPaymentAsync_OwnershipViolation_ThrowsNotFound()
    {
        var booking = CreateBooking(id: 10, customerId: 5, guests: 2, status: "Pending");
        var otherCustomer = new Customer { Id = 99, UserId = 77, Name = "Bob", Email = "bob@example.com", Phone = "123" };
        var payment = new Payment { Id = 20, BookingId = 10, Amount = 200m, PaymentMethod = "Demo", Status = "Pending" };

        _paymentRepositoryMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(payment);
        _bookingRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(booking);
        _customerRepositoryMock.Setup(r => r.GetByUserIdAsync(77)).ReturnsAsync(otherCustomer);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.ProcessDemoPaymentAsync(20, 77, true));
        _paymentRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task GetPaymentByIdAsync_WithUserId_OwnershipViolationReturnsNull()
    {
        var booking = CreateBooking(id: 10, customerId: 5, guests: 2, status: "Pending");
        var otherCustomer = new Customer { Id = 99, UserId = 77, Name = "Bob", Email = "bob@example.com", Phone = "123" };
        var payment = new Payment { Id = 20, BookingId = 10, Amount = 200m, PaymentMethod = "Demo", Status = "Pending" };

        _paymentRepositoryMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(payment);
        _customerRepositoryMock.Setup(r => r.GetByUserIdAsync(77)).ReturnsAsync(otherCustomer);
        _bookingRepositoryMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(booking);

        var result = await _service.GetPaymentByIdAsync(20, 77);

        Assert.Null(result);
    }

    private static Booking CreateBooking(int id, int customerId, int guests, string status) => new()
    {
        Id = id,
        CustomerId = customerId,
        RestaurantTableId = 1,
        BookingDate = DateTime.UtcNow.Date.AddDays(1),
        StartTime = new TimeSpan(18, 0, 0),
        EndTime = new TimeSpan(19, 0, 0),
        NumberOfGuests = guests,
        Status = status,
        CreatedAt = DateTime.UtcNow
    };
}

