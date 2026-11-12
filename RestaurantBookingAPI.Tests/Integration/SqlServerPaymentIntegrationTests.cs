using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RestaurantBookingAPI.Data;
using RestaurantBookingAPI.Exceptions;
using RestaurantBookingAPI.Models;
using RestaurantBookingAPI.Repositories;
using RestaurantBookingAPI.Services;
using RestaurantBookingAPI.Validation;

namespace RestaurantBookingAPI.Tests.Integration;

[CollectionDefinition("SqlServerPaymentIntegration", DisableParallelization = true)]
public sealed class SqlServerPaymentIntegrationCollection { }

[Collection("SqlServerPaymentIntegration")]
public sealed class SqlServerPaymentIntegrationTests : IAsyncLifetime
{
    private const string ConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=RestaurantBookingDB_SP;Trusted_Connection=True;TrustServerCertificate=True;";

    private readonly List<int> _createdBookingIds = [];
    private readonly List<int> _createdTableIds = [];
    private readonly List<int> _createdRestaurantIds = [];
    private readonly List<int> _createdCustomerIds = [];
    private readonly List<int> _createdUserIds = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        if (_createdBookingIds.Count > 0)
        {
            var bookingIdList = string.Join(",", _createdBookingIds);
            await ExecuteNonQueryAsync(conn, $"DELETE FROM dbo.Payments WHERE BookingId IN ({bookingIdList});");
            await ExecuteNonQueryAsync(conn, $"DELETE FROM dbo.Booking WHERE Id IN ({bookingIdList});");
        }

        if (_createdTableIds.Count > 0)
        {
            await ExecuteNonQueryAsync(conn, $"DELETE FROM dbo.RestaurantTable WHERE Id IN ({string.Join(",", _createdTableIds)});");
        }

        if (_createdRestaurantIds.Count > 0)
        {
            await ExecuteNonQueryAsync(conn, $"DELETE FROM dbo.Restaurant WHERE Id IN ({string.Join(",", _createdRestaurantIds)});");
        }

        if (_createdCustomerIds.Count > 0)
        {
            await ExecuteNonQueryAsync(conn, $"DELETE FROM dbo.Customer WHERE Id IN ({string.Join(",", _createdCustomerIds)});");
        }

        if (_createdUserIds.Count > 0)
        {
            await ExecuteNonQueryAsync(conn, $"DELETE FROM dbo.Users WHERE Id IN ({string.Join(",", _createdUserIds)});");
        }
    }

    [Fact]
    public async Task Scenario01_InitiatePayment_ValidPendingBooking_SucceedsWithServerCalculatedAmount()
    {
        var fixture = await CreateFixtureAsync(bookingStatus: "Pending", guests: 4);
        await using var scope = CreatePaymentScope();

        var payment = await scope.Service.InitiatePaymentAsync(fixture.BookingId, fixture.OwnerUserId);

        Assert.True(payment.Id > 0);
        Assert.Equal(fixture.BookingId, payment.BookingId);
        Assert.Equal(400m, payment.Amount);
        Assert.Equal("Demo", payment.PaymentMethod);
        Assert.Equal("Pending", payment.Status);
        Assert.Null(payment.TransactionId);
        Assert.Null(payment.PaidAt);
    }

    [Fact]
    public async Task Scenario02_InitiatePayment_CancelledBooking_ThrowsBadRequestAndCreatesNoPayment()
    {
        var fixture = await CreateFixtureAsync(bookingStatus: "Cancelled", guests: 2);
        await using var scope = CreatePaymentScope();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            scope.Service.InitiatePaymentAsync(fixture.BookingId, fixture.OwnerUserId));

        Assert.Equal(0, await GetPaymentCountForBookingAsync(fixture.BookingId));
    }

    [Fact]
    public async Task Scenario03_InitiatePayment_CompletedBooking_ThrowsBadRequestAndCreatesNoPayment()
    {
        var fixture = await CreateFixtureAsync(bookingStatus: "Completed", guests: 2);
        await using var scope = CreatePaymentScope();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            scope.Service.InitiatePaymentAsync(fixture.BookingId, fixture.OwnerUserId));

        Assert.Equal(0, await GetPaymentCountForBookingAsync(fixture.BookingId));
    }

    [Fact]
    public async Task Scenario04_InitiatePayment_DuplicateSequentialAndConcurrent_AllowsOnlyOnePayment()
    {
        var fixture = await CreateFixtureAsync(bookingStatus: "Pending", guests: 3);
        var startGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<Exception?> AttemptInitiateAsync()
        {
            await using var scope = CreatePaymentScope();
            await startGate.Task;
            try
            {
                await scope.Service.InitiatePaymentAsync(fixture.BookingId, fixture.OwnerUserId);
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        var t1 = Task.Run(AttemptInitiateAsync);
        var t2 = Task.Run(AttemptInitiateAsync);
        startGate.SetResult();

        var results = await Task.WhenAll(t1, t2);

        Assert.Equal(1, results.Count(r => r == null));
        var failure = Assert.Single(results, r => r != null);
        Assert.IsType<BadRequestException>(failure);
        Assert.Equal(1, await GetPaymentCountForBookingAsync(fixture.BookingId));

        // Sequential duplicate also rejected
        await using var scope3 = CreatePaymentScope();
        await Assert.ThrowsAsync<BadRequestException>(() =>
            scope3.Service.InitiatePaymentAsync(fixture.BookingId, fixture.OwnerUserId));
    }

    [Fact]
    public async Task Scenario05_ProcessDemoPayment_Success_ConfirmsBookingAndSetsTransactionDetails()
    {
        var fixture = await CreateFixtureAsync(bookingStatus: "Pending", guests: 5);
        await using var scope = CreatePaymentScope();

        var initiated = await scope.Service.InitiatePaymentAsync(fixture.BookingId, fixture.OwnerUserId);
        var processed = await scope.Service.ProcessDemoPaymentAsync(initiated.Id, fixture.OwnerUserId, simulateSuccess: true);

        Assert.Equal("Success", processed.Status);
        Assert.NotNull(processed.PaidAt);
        Assert.StartsWith("DEMO-", processed.TransactionId);
        Assert.Equal(500m, processed.Amount);
        Assert.Equal("Confirmed", await GetBookingStatusAsync(fixture.BookingId));
    }

    [Fact]
    public async Task Scenario06_ProcessDemoPayment_Failure_MarksFailedAndLeavesBookingPending()
    {
        var fixture = await CreateFixtureAsync(bookingStatus: "Pending", guests: 2);
        await using var scope = CreatePaymentScope();

        var initiated = await scope.Service.InitiatePaymentAsync(fixture.BookingId, fixture.OwnerUserId);
        var processed = await scope.Service.ProcessDemoPaymentAsync(initiated.Id, fixture.OwnerUserId, simulateSuccess: false);

        Assert.Equal("Failed", processed.Status);
        Assert.Null(processed.PaidAt);
        Assert.Null(processed.TransactionId);
        Assert.Equal("Pending", await GetBookingStatusAsync(fixture.BookingId));
    }

    [Fact]
    public async Task Scenario07_ProcessDemoPayment_RetryAfterFailure_ReusesSameRecordAndConfirmsBooking()
    {
        var fixture = await CreateFixtureAsync(bookingStatus: "Pending", guests: 2);
        await using var scope = CreatePaymentScope();

        var initiated = await scope.Service.InitiatePaymentAsync(fixture.BookingId, fixture.OwnerUserId);
        var failed = await scope.Service.ProcessDemoPaymentAsync(initiated.Id, fixture.OwnerUserId, simulateSuccess: false);
        Assert.Equal("Failed", failed.Status);

        var retried = await scope.Service.ProcessDemoPaymentAsync(initiated.Id, fixture.OwnerUserId, simulateSuccess: true);

        Assert.Equal(initiated.Id, retried.Id);
        Assert.Equal("Success", retried.Status);
        Assert.NotNull(retried.PaidAt);
        Assert.StartsWith("DEMO-", retried.TransactionId);
        Assert.Equal(1, await GetPaymentCountForBookingAsync(fixture.BookingId));
        Assert.Equal("Confirmed", await GetBookingStatusAsync(fixture.BookingId));
    }

    [Fact]
    public async Task Scenario08_ProcessDemoPayment_ReprocessingSuccessfulPayment_ThrowsBadRequest()
    {
        var fixture = await CreateFixtureAsync(bookingStatus: "Pending", guests: 2);
        await using var scope = CreatePaymentScope();

        var initiated = await scope.Service.InitiatePaymentAsync(fixture.BookingId, fixture.OwnerUserId);
        var succeeded = await scope.Service.ProcessDemoPaymentAsync(initiated.Id, fixture.OwnerUserId, simulateSuccess: true);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            scope.Service.ProcessDemoPaymentAsync(initiated.Id, fixture.OwnerUserId, simulateSuccess: true));

        await Assert.ThrowsAsync<BadRequestException>(() =>
            scope.Service.ProcessDemoPaymentAsync(initiated.Id, fixture.OwnerUserId, simulateSuccess: false));

        var reloaded = await scope.Service.GetPaymentByIdAsync(initiated.Id, fixture.OwnerUserId);
        Assert.NotNull(reloaded);
        Assert.Equal("Success", reloaded!.Status);
        Assert.Equal(succeeded.TransactionId, reloaded.TransactionId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Scenario09_ProcessDemoPayment_CancelledBooking_ThrowsBadRequestAndNeverConfirms(bool simulateSuccess)
    {
        var fixture = await CreateFixtureAsync(bookingStatus: "Pending", guests: 2);
        int paymentId;

        await using (var scope = CreatePaymentScope())
        {
            var payment = await scope.Service.InitiatePaymentAsync(fixture.BookingId, fixture.OwnerUserId);
            paymentId = payment.Id;
        }

        await using (var bookingScope = CreateBookingScope())
        {
            var cancelled = await bookingScope.Service.CancelAsync(fixture.BookingId, fixture.CustomerId);
            Assert.True(cancelled);
        }

        await using (var processScope = CreatePaymentScope())
        {
            await Assert.ThrowsAsync<BadRequestException>(() =>
                processScope.Service.ProcessDemoPaymentAsync(paymentId, fixture.OwnerUserId, simulateSuccess));
        }

        Assert.Equal("Cancelled", await GetBookingStatusAsync(fixture.BookingId));
        var dbPayment = await GetPaymentRowAsync(paymentId);
        Assert.Equal("Pending", dbPayment.Status);
        Assert.Null(dbPayment.TransactionId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Scenario10_ProcessDemoPayment_CompletedBooking_ThrowsBadRequestAndNeverConfirms(bool simulateSuccess)
    {
        var fixture = await CreateFixtureAsync(bookingStatus: "Pending", guests: 2);
        int paymentId;

        await using (var scope = CreatePaymentScope())
        {
            var payment = await scope.Service.InitiatePaymentAsync(fixture.BookingId, fixture.OwnerUserId);
            paymentId = payment.Id;
        }

        await SetBookingStatusDirectlyAsync(fixture.BookingId, "Completed");

        await using (var processScope = CreatePaymentScope())
        {
            await Assert.ThrowsAsync<BadRequestException>(() =>
                processScope.Service.ProcessDemoPaymentAsync(paymentId, fixture.OwnerUserId, simulateSuccess));
        }

        Assert.Equal("Completed", await GetBookingStatusAsync(fixture.BookingId));
        var dbPayment = await GetPaymentRowAsync(paymentId);
        Assert.Equal("Pending", dbPayment.Status);
    }

    [Fact]
    public async Task Scenario11_ConcurrentProcessDemoPayment_OnlyOneSucceeds()
    {
        var fixture = await CreateFixtureAsync(bookingStatus: "Pending", guests: 3);
        int paymentId;

        await using (var initScope = CreatePaymentScope())
        {
            var created = await initScope.Service.InitiatePaymentAsync(fixture.BookingId, fixture.OwnerUserId);
            paymentId = created.Id;
        }

        var startGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<(Payment? Payment, Exception? Error)> RunWorkerAsync()
        {
            await using var scope = CreatePaymentScope();
            await startGate.Task;
            try
            {
                var p = await scope.Service.ProcessDemoPaymentAsync(paymentId, fixture.OwnerUserId, simulateSuccess: true);
                return (p, null);
            }
            catch (Exception ex)
            {
                return (null, ex);
            }
        }

        var t1 = Task.Run(RunWorkerAsync);
        var t2 = Task.Run(RunWorkerAsync);
        startGate.SetResult();

        var results = await Task.WhenAll(t1, t2);

        Assert.Equal(1, results.Count(r => r.Payment != null));
        Assert.Equal(1, results.Count(r => r.Error is BadRequestException));
        Assert.Equal("Confirmed", await GetBookingStatusAsync(fixture.BookingId));

        var finalPayment = await GetPaymentRowAsync(paymentId);
        Assert.Equal("Success", finalPayment.Status);
        Assert.NotNull(finalPayment.TransactionId);
        Assert.NotNull(finalPayment.PaidAt);
    }

    [Fact]
    public async Task Scenario12_ConcurrentCancellationAndPaymentConfirmation_NeverProducesCancelledRestoredToConfirmed()
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            var fixture = await CreateFixtureAsync(bookingStatus: "Pending", guests: 2, dayOffset: attempt + 1);
            int paymentId;

            await using (var initScope = CreatePaymentScope())
            {
                var created = await initScope.Service.InitiatePaymentAsync(fixture.BookingId, fixture.OwnerUserId);
                paymentId = created.Id;
            }

            var startGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var paymentTask = Task.Run<Exception?>(async () =>
            {
                await using var scope = CreatePaymentScope();
                await startGate.Task;
                try
                {
                    await scope.Service.ProcessDemoPaymentAsync(paymentId, fixture.OwnerUserId, simulateSuccess: true);
                    return null;
                }
                catch (Exception ex)
                {
                    return ex;
                }
            });

            var cancelTask = Task.Run<bool>(async () =>
            {
                await using var scope = CreateBookingScope();
                await startGate.Task;
                return await scope.Service.CancelAsync(fixture.BookingId, fixture.CustomerId);
            });

            startGate.SetResult();
            await Task.WhenAll(paymentTask, cancelTask);

            var cancelResult = await cancelTask;
            var paymentError = await paymentTask;

            Assert.True(cancelResult);

            var finalBookingStatus = await GetBookingStatusAsync(fixture.BookingId);
            var finalPayment = await GetPaymentRowAsync(paymentId);

            // Because cancellation ran, a Cancelled booking must NEVER end up Confirmed.
            Assert.Equal("Cancelled", finalBookingStatus);

            if (paymentError == null)
            {
                // Payment committed first, then CancelAsync transitioned Confirmed -> Cancelled.
                Assert.Equal("Success", finalPayment.Status);
                Assert.NotNull(finalPayment.TransactionId);
                Assert.NotNull(finalPayment.PaidAt);
            }
            else
            {
                // CancelAsync committed first, then ProcessDemoPaymentAsync rejected the payment.
                Assert.IsType<BadRequestException>(paymentError);
                Assert.Equal("Pending", finalPayment.Status);
                Assert.Null(finalPayment.TransactionId);
                Assert.Null(finalPayment.PaidAt);
            }
        }
    }

    [Fact]
    public async Task Scenario13_OwnershipViolations_OtherCustomerCannotInitiateRetrieveOrProcess()
    {
        var fixture = await CreateFixtureAsync(bookingStatus: "Pending", guests: 2);
        var otherUser = await CreateUserAndCustomerAsync();

        await using var scope = CreatePaymentScope();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            scope.Service.InitiatePaymentAsync(fixture.BookingId, otherUser.UserId));

        var payment = await scope.Service.InitiatePaymentAsync(fixture.BookingId, fixture.OwnerUserId);

        Assert.Null(await scope.Service.GetPaymentByIdAsync(payment.Id, otherUser.UserId));
        Assert.Empty(await scope.Service.GetPaymentHistoryAsync(otherUser.UserId));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            scope.Service.ProcessDemoPaymentAsync(payment.Id, otherUser.UserId, simulateSuccess: true));

        var stillPending = await GetPaymentRowAsync(payment.Id);
        Assert.Equal("Pending", stillPending.Status);
        Assert.Equal("Pending", await GetBookingStatusAsync(fixture.BookingId));
    }

    [Fact]
    public async Task Scenario14_TransactionRollback_WhenBookingUpdateFails_PaymentRevertsToPending()
    {
        var fixture = await CreateFixtureAsync(bookingStatus: "Pending", guests: 2);
        int paymentId;

        await using (var initScope = CreatePaymentScope())
        {
            var created = await initScope.Service.InitiatePaymentAsync(fixture.BookingId, fixture.OwnerUserId);
            paymentId = created.Id;
        }

        // Insert an overlapping Confirmed booking on the exact same table and slot so dbo.UpdateBooking fails with 50002.
        await InsertConflictingBookingAsync(fixture.CustomerId, fixture.TableId, fixture.BookingDate);

        await using (var scope = CreatePaymentScope())
        {
            await Assert.ThrowsAsync<BookingConflictException>(() =>
                scope.Service.ProcessDemoPaymentAsync(paymentId, fixture.OwnerUserId, simulateSuccess: true));
        }

        // Verify dbo.UpdatePayment (which executed before dbo.UpdateBooking) was rolled back.
        var dbPayment = await GetPaymentRowAsync(paymentId);
        Assert.Equal("Pending", dbPayment.Status);
        Assert.Null(dbPayment.TransactionId);
        Assert.Null(dbPayment.PaidAt);
        Assert.Equal("Pending", await GetBookingStatusAsync(fixture.BookingId));
    }

    [Fact]
    public async Task Scenario15_UpdatePayment_EnforcesImmutabilityCreatedAtPreservationAndUniqueTransactionId()
    {
        var fixture1 = await CreateFixtureAsync(bookingStatus: "Pending", guests: 2, dayOffset: 10);
        var fixture2 = await CreateFixtureAsync(bookingStatus: "Pending", guests: 3, dayOffset: 11);

        int payment1Id;
        int payment2Id;
        DateTime originalCreatedAt;

        await using (var scope = CreatePaymentScope())
        {
            var p1 = await scope.Service.InitiatePaymentAsync(fixture1.BookingId, fixture1.OwnerUserId);
            var p2 = await scope.Service.InitiatePaymentAsync(fixture2.BookingId, fixture2.OwnerUserId);
            payment1Id = p1.Id;
            payment2Id = p2.Id;
            originalCreatedAt = p1.CreatedAt;

            // Attempting to change BookingId via repository/SP must fail
            await Assert.ThrowsAsync<BadRequestException>(() =>
                scope.PaymentRepository.UpdateAsync(new Payment
                {
                    Id = payment1Id,
                    BookingId = fixture2.BookingId,
                    Amount = p1.Amount,
                    PaymentMethod = "Demo",
                    Status = "Failed"
                }));

            // Attempting to change Amount via repository/SP must fail
            await Assert.ThrowsAsync<BadRequestException>(() =>
                scope.PaymentRepository.UpdateAsync(new Payment
                {
                    Id = payment1Id,
                    BookingId = fixture1.BookingId,
                    Amount = 999m,
                    PaymentMethod = "Demo",
                    Status = "Failed"
                }));

            // Invalid status must fail
            await Assert.ThrowsAsync<BadRequestException>(() =>
                scope.PaymentRepository.UpdateAsync(new Payment
                {
                    Id = payment1Id,
                    BookingId = fixture1.BookingId,
                    Amount = p1.Amount,
                    PaymentMethod = "Demo",
                    Status = "Refunded"
                }));

            // Fail then succeed preserves CreatedAt
            var failed = await scope.Service.ProcessDemoPaymentAsync(payment1Id, fixture1.OwnerUserId, simulateSuccess: false);
            Assert.Equal(originalCreatedAt, failed.CreatedAt);

            // Failed -> Pending revert via SP must fail
            await Assert.ThrowsAsync<BadRequestException>(() =>
                scope.PaymentRepository.UpdateAsync(new Payment
                {
                    Id = payment1Id,
                    BookingId = fixture1.BookingId,
                    Amount = p1.Amount,
                    PaymentMethod = "Demo",
                    Status = "Pending"
                }));

            var succeeded = await scope.Service.ProcessDemoPaymentAsync(payment1Id, fixture1.OwnerUserId, simulateSuccess: true);
            Assert.Equal(originalCreatedAt, succeeded.CreatedAt);
            Assert.Equal(200m, succeeded.Amount);
            Assert.Equal(fixture1.BookingId, succeeded.BookingId);

            // Success -> Failed or Pending revert via SP must fail
            await Assert.ThrowsAsync<BadRequestException>(() =>
                scope.PaymentRepository.UpdateAsync(new Payment
                {
                    Id = payment1Id,
                    BookingId = fixture1.BookingId,
                    Amount = p1.Amount,
                    PaymentMethod = "Demo",
                    Status = "Failed"
                }));

            // Duplicate TransactionId across payment2 must be rejected by UX_Payments_TransactionId
            await Assert.ThrowsAsync<BadRequestException>(() =>
                scope.PaymentRepository.UpdateAsync(new Payment
                {
                    Id = payment2Id,
                    BookingId = fixture2.BookingId,
                    Amount = p2.Amount,
                    PaymentMethod = "Demo",
                    Status = "Success",
                    TransactionId = succeeded.TransactionId,
                    PaidAt = DateTime.UtcNow
                }));
        }

        var reloaded1 = await GetPaymentRowAsync(payment1Id);
        Assert.Equal("Success", reloaded1.Status);
        Assert.Equal(200m, reloaded1.Amount);
        Assert.Equal(fixture1.BookingId, reloaded1.BookingId);
        Assert.Equal(originalCreatedAt, reloaded1.CreatedAt);
    }

    [Fact]
    public async Task Scenario16_InitiateAndProcessPayment_UsesUtcTimestampsConsistently()
    {
        var fixture = await CreateFixtureAsync(bookingStatus: "Pending", guests: 2);
        await using var scope = CreatePaymentScope();

        var beforeUtc = DateTime.UtcNow.AddSeconds(-2);
        var initiated = await scope.Service.InitiatePaymentAsync(fixture.BookingId, fixture.OwnerUserId);
        var afterInitiateUtc = DateTime.UtcNow.AddSeconds(2);

        Assert.InRange(initiated.CreatedAt, beforeUtc, afterInitiateUtc);

        var processed = await scope.Service.ProcessDemoPaymentAsync(
            initiated.Id,
            fixture.OwnerUserId,
            simulateSuccess: true);

        Assert.NotNull(processed.PaidAt);
        Assert.True(processed.PaidAt!.Value >= initiated.CreatedAt);
        Assert.InRange(processed.PaidAt!.Value, beforeUtc, DateTime.UtcNow.AddSeconds(2));
    }

    private PaymentScope CreatePaymentScope()
    {
        var context = CreateDbContext();
        var paymentRepo = new PaymentRepository(context);
        var bookingRepo = new BookingRepository(context);
        var customerRepo = new CustomerRepository(context);
        var service = new PaymentService(
            paymentRepo,
            bookingRepo,
            customerRepo,
            context,
            NullLogger<PaymentService>.Instance);

        return new PaymentScope(context, service, paymentRepo);
    }

    private BookingScope CreateBookingScope()
    {
        var context = CreateDbContext();
        var bookingRepo = new BookingRepository(context);
        var customerRepo = new CustomerRepository(context);
        var tableRepo = new RestaurantTableRepository(context);
        var operatingHourRepo = new OperatingHourRepository(context);
        var service = new BookingService(
            bookingRepo,
            customerRepo,
            tableRepo,
            operatingHourRepo,
            NullLogger<BookingService>.Instance,
            new BookingValidator(),
            new CheckAvailabilityValidator());

        return new BookingScope(context, service);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    private async Task<(int OwnerUserId, int CustomerId, int TableId, int BookingId, DateTime BookingDate)> CreateFixtureAsync(
        string bookingStatus,
        int guests,
        int dayOffset = 0)
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        var owner = await InsertUserAndCustomerAsync(conn);

        var restaurantId = await ExecuteScalarIntAsync(conn, """
            INSERT INTO dbo.Restaurant (Name, Address, Phone, Email, IsActive, OwnerUserId, City, Description, CoverImagePath)
            VALUES (@Name, N'123 Test Street', N'9999999999', NULL, 1, @OwnerUserId, N'TestCity', N'Test', NULL);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """,
            new SqlParameter("@Name", $"Phase4B_Rest_{Guid.NewGuid():N}"[..30]),
            new SqlParameter("@OwnerUserId", owner.UserId));
        _createdRestaurantIds.Add(restaurantId);

        var tableId = await ExecuteScalarIntAsync(conn, """
            INSERT INTO dbo.RestaurantTable (RestaurantId, TableNumber, Capacity, Status)
            VALUES (@RestaurantId, 1, 10, N'Available');
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """,
            new SqlParameter("@RestaurantId", restaurantId));
        _createdTableIds.Add(tableId);

        var bookingDate = new DateTime(2099, 6, 1).AddDays(dayOffset);
        var bookingId = await ExecuteScalarIntAsync(conn, """
            INSERT INTO dbo.Booking (CustomerId, RestaurantTableId, BookingDate, StartTime, EndTime, NumberOfGuests, Status, SpecialRequest, CreatedAt)
            VALUES (@CustomerId, @TableId, @BookingDate, '18:00:00', '19:00:00', @Guests, @Status, NULL, SYSDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """,
            new SqlParameter("@CustomerId", owner.CustomerId),
            new SqlParameter("@TableId", tableId),
            new SqlParameter("@BookingDate", bookingDate),
            new SqlParameter("@Guests", guests),
            new SqlParameter("@Status", bookingStatus));
        _createdBookingIds.Add(bookingId);

        return (owner.UserId, owner.CustomerId, tableId, bookingId, bookingDate);
    }

    private async Task<(int UserId, int CustomerId)> CreateUserAndCustomerAsync()
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        return await InsertUserAndCustomerAsync(conn);
    }

    private async Task<(int UserId, int CustomerId)> InsertUserAndCustomerAsync(SqlConnection conn)
    {
        var token = Guid.NewGuid().ToString("N");
        var email = $"phase4b_{token}@test.local";

        var userId = await ExecuteScalarIntAsync(conn, """
            INSERT INTO dbo.Users (Name, Email, PasswordHash, Role, IsActive, CreatedAt)
            VALUES (@Name, @Email, N'TestHash', N'Customer', 1, SYSUTCDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """,
            new SqlParameter("@Name", $"User_{token[..8]}"),
            new SqlParameter("@Email", email));
        _createdUserIds.Add(userId);

        var customerId = await ExecuteScalarIntAsync(conn, """
            INSERT INTO dbo.Customer (Name, Email, Phone, UserId)
            VALUES (@Name, @Email, N'9999999999', @UserId);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """,
            new SqlParameter("@Name", $"Cust_{token[..8]}"),
            new SqlParameter("@Email", email),
            new SqlParameter("@UserId", userId));
        _createdCustomerIds.Add(customerId);

        return (userId, customerId);
    }

    private async Task InsertConflictingBookingAsync(int customerId, int tableId, DateTime bookingDate)
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        var conflictId = await ExecuteScalarIntAsync(conn, """
            INSERT INTO dbo.Booking (CustomerId, RestaurantTableId, BookingDate, StartTime, EndTime, NumberOfGuests, Status, SpecialRequest, CreatedAt)
            VALUES (@CustomerId, @TableId, @BookingDate, '18:00:00', '19:00:00', 2, N'Confirmed', NULL, SYSDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """,
            new SqlParameter("@CustomerId", customerId),
            new SqlParameter("@TableId", tableId),
            new SqlParameter("@BookingDate", bookingDate));
        _createdBookingIds.Add(conflictId);
    }

    private static async Task SetBookingStatusDirectlyAsync(int bookingId, string status)
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("UPDATE dbo.Booking SET Status = @Status WHERE Id = @Id;", conn);
        cmd.Parameters.AddWithValue("@Status", status);
        cmd.Parameters.AddWithValue("@Id", bookingId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<string> GetBookingStatusAsync(int bookingId)
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("SELECT Status FROM dbo.Booking WHERE Id = @Id;", conn);
        cmd.Parameters.AddWithValue("@Id", bookingId);
        return (string)(await cmd.ExecuteScalarAsync())!;
    }

    private static async Task<int> GetPaymentCountForBookingAsync(int bookingId)
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Payments WHERE BookingId = @BookingId;", conn);
        cmd.Parameters.AddWithValue("@BookingId", bookingId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static async Task<Payment> GetPaymentRowAsync(int paymentId)
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("""
            SELECT Id, BookingId, Amount, PaymentMethod, Status, TransactionId, CreatedAt, PaidAt
            FROM dbo.Payments
            WHERE Id = @Id;
            """, conn);
        cmd.Parameters.AddWithValue("@Id", paymentId);

        await using var reader = await cmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());

        return new Payment
        {
            Id = reader.GetInt32(0),
            BookingId = reader.GetInt32(1),
            Amount = reader.GetDecimal(2),
            PaymentMethod = reader.GetString(3),
            Status = reader.GetString(4),
            TransactionId = reader.IsDBNull(5) ? null : reader.GetString(5),
            CreatedAt = reader.GetDateTime(6),
            PaidAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
        };
    }

    private static async Task<int> ExecuteScalarIntAsync(SqlConnection conn, string sql, params SqlParameter[] parameters)
    {
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddRange(parameters);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private static async Task ExecuteNonQueryAsync(SqlConnection conn, string sql)
    {
        await using var cmd = new SqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private sealed class PaymentScope(AppDbContext context, PaymentService service, PaymentRepository paymentRepository) : IAsyncDisposable
    {
        public PaymentService Service { get; } = service;
        public PaymentRepository PaymentRepository { get; } = paymentRepository;
        public ValueTask DisposeAsync() => context.DisposeAsync();
    }

    private sealed class BookingScope(AppDbContext context, BookingService service) : IAsyncDisposable
    {
        public BookingService Service { get; } = service;
        public ValueTask DisposeAsync() => context.DisposeAsync();
    }
}

