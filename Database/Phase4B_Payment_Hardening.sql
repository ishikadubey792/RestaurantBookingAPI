/*
    Phase 4B — Backend Payment Hardening
    Database: RestaurantBookingDB_SP

    Summary of changes:
    1. New dbo.GetBookingByIdForUpdate — acquires UPDLOCK, ROWLOCK on Booking row inside caller transaction.
    2. New dbo.GetPaymentByIdForUpdate — acquires UPDLOCK, ROWLOCK on Payments row inside caller transaction.
    3. Updated dbo.CreatePayment — locks Booking row, enforces Pending booking status, rejects Cancelled/Completed/non-Pending
       bookings, enforces initial Pending payment invariants, and guards against duplicate payments.
    4. Updated dbo.UpdatePayment — locks Payments row, prevents BookingId/Amount/CreatedAt modification,
       validates supported payment statuses ('Pending', 'Failed', 'Success'), prevents reverting Success payments,
       and enforces TransactionId/PaidAt invariants while keeping UX_Payments_BookingId and UX_Payments_TransactionId intact.
    5. Updated dbo.CancelBooking — acquires UPDLOCK, ROWLOCK on Booking row, prevents cancelling Completed bookings,
       and keeps cancellation atomic against concurrent payment confirmation.
    6. Updated dbo.UpdateBooking — locks Booking row and validates current Status so Cancelled/Completed bookings
       cannot be restored to Confirmed/Pending, and Confirmed bookings cannot be reverted to Pending.
*/

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- 1. Row-locked Booking lookup for transactional workflows
CREATE OR ALTER PROCEDURE dbo.GetBookingByIdForUpdate
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        CustomerId,
        RestaurantTableId,
        BookingDate,
        StartTime,
        EndTime,
        NumberOfGuests,
        Status,
        SpecialRequest,
        CreatedAt
    FROM dbo.Booking WITH (UPDLOCK, ROWLOCK)
    WHERE Id = @Id;
END;
GO

-- 2. Row-locked Payment lookup for transactional workflows
CREATE OR ALTER PROCEDURE dbo.GetPaymentByIdForUpdate
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        BookingId,
        Amount,
        PaymentMethod,
        Status,
        TransactionId,
        CreatedAt,
        PaidAt
    FROM dbo.Payments WITH (UPDLOCK, ROWLOCK)
    WHERE Id = @Id;
END;
GO

-- 3. Hardened CreatePayment
CREATE OR ALTER PROCEDURE dbo.CreatePayment
    @BookingId INT,
    @Amount DECIMAL(10,2),
    @PaymentMethod NVARCHAR(100),
    @Status NVARCHAR(40),
    @TransactionId NVARCHAR(200) = NULL,
    @PaidAt DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @BookingStatus NVARCHAR(40);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @BookingStatus = Status
        FROM dbo.Booking WITH (UPDLOCK, ROWLOCK)
        WHERE Id = @BookingId;

        IF @BookingStatus IS NULL
        BEGIN
            THROW 50010, 'Booking not found.', 1;
        END;

        IF @BookingStatus = 'Cancelled'
        BEGIN
            THROW 50011, 'Payment cannot be initiated for a cancelled booking.', 1;
        END;

        IF @BookingStatus = 'Completed'
        BEGIN
            THROW 50012, 'Payment cannot be initiated for a completed booking.', 1;
        END;

        IF @BookingStatus COLLATE Latin1_General_CS_AS <> 'Pending'
        BEGIN
            THROW 50013, 'Payment can only be initiated for a pending booking.', 1;
        END;

        IF @Amount IS NULL OR @Amount <= 0
        BEGIN
            THROW 50014, 'Payment amount must be greater than zero.', 1;
        END;

        IF @Status IS NULL OR @Status COLLATE Latin1_General_CS_AS <> 'Pending'
        BEGIN
            THROW 50015, 'Initial payment status must be Pending.', 1;
        END;

        IF @TransactionId IS NOT NULL OR @PaidAt IS NOT NULL
        BEGIN
            THROW 50016, 'Initial pending payment must not include TransactionId or PaidAt.', 1;
        END;

        IF @PaymentMethod IS NULL OR LTRIM(RTRIM(@PaymentMethod)) = ''
        BEGIN
            THROW 50017, 'Payment method is required.', 1;
        END;

        IF EXISTS (
            SELECT 1
            FROM dbo.Payments WITH (UPDLOCK, HOLDLOCK)
            WHERE BookingId = @BookingId
        )
        BEGIN
            THROW 50018, 'A payment record already exists for this booking.', 1;
        END;

        INSERT INTO dbo.Payments
            (BookingId, Amount, PaymentMethod, Status,
             TransactionId, CreatedAt, PaidAt)
        VALUES
            (@BookingId, @Amount, @PaymentMethod, @Status,
             NULL, SYSUTCDATETIME(), NULL);

        DECLARE @NewId INT = SCOPE_IDENTITY();

        COMMIT TRANSACTION;

        SELECT Id, BookingId, Amount, PaymentMethod, Status,
               TransactionId, CreatedAt, PaidAt
        FROM dbo.Payments
        WHERE Id = @NewId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH;
END;
GO

-- 4. Hardened UpdatePayment
CREATE OR ALTER PROCEDURE dbo.UpdatePayment
    @Id INT,
    @BookingId INT,
    @Amount DECIMAL(10,2),
    @PaymentMethod NVARCHAR(100),
    @Status NVARCHAR(40),
    @TransactionId NVARCHAR(200) = NULL,
    @PaidAt DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ExistingBookingId INT;
    DECLARE @ExistingAmount DECIMAL(10,2);
    DECLARE @ExistingStatus NVARCHAR(40);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT
            @ExistingBookingId = BookingId,
            @ExistingAmount = Amount,
            @ExistingStatus = Status
        FROM dbo.Payments WITH (UPDLOCK, ROWLOCK)
        WHERE Id = @Id;

        IF @ExistingBookingId IS NULL
        BEGIN
            COMMIT TRANSACTION;

            SELECT TOP (0) Id, BookingId, Amount, PaymentMethod, Status,
                   TransactionId, CreatedAt, PaidAt
            FROM dbo.Payments;
            RETURN;
        END;

        IF @BookingId <> @ExistingBookingId
        BEGIN
            THROW 50021, 'Payment BookingId cannot be modified.', 1;
        END;

        IF @Amount <> @ExistingAmount
        BEGIN
            THROW 50022, 'Payment Amount cannot be modified.', 1;
        END;

        IF @ExistingStatus = 'Success'
        BEGIN
            THROW 50023, 'A successful payment cannot be modified or reverted.', 1;
        END;

        IF @Status IS NULL
           OR @Status COLLATE Latin1_General_CS_AS NOT IN ('Pending', 'Failed', 'Success')
        BEGIN
            THROW 50024, 'Invalid payment status. Supported statuses are Pending, Failed, and Success.', 1;
        END;

        IF @PaymentMethod IS NULL OR LTRIM(RTRIM(@PaymentMethod)) = ''
        BEGIN
            THROW 50025, 'Payment method is required.', 1;
        END;

        IF @ExistingStatus = 'Failed' AND @Status = 'Pending'
        BEGIN
            THROW 50026, 'A failed payment cannot be reverted to Pending.', 1;
        END;

        IF @Status = 'Success'
           AND (@TransactionId IS NULL OR LTRIM(RTRIM(@TransactionId)) = '' OR @PaidAt IS NULL)
        BEGIN
            THROW 50027, 'Successful payment requires both TransactionId and PaidAt.', 1;
        END;

        IF @Status IN ('Pending', 'Failed')
           AND (@TransactionId IS NOT NULL OR @PaidAt IS NOT NULL)
        BEGIN
            THROW 50028, 'Pending or Failed payment must not have TransactionId or PaidAt.', 1;
        END;

        -- Preserve BookingId, Amount, and CreatedAt
        UPDATE dbo.Payments
        SET PaymentMethod = @PaymentMethod,
            Status = @Status,
            TransactionId = @TransactionId,
            PaidAt = @PaidAt
        WHERE Id = @Id;

        COMMIT TRANSACTION;

        SELECT Id, BookingId, Amount, PaymentMethod, Status,
               TransactionId, CreatedAt, PaidAt
        FROM dbo.Payments
        WHERE Id = @Id;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH;
END;
GO

-- 5. Hardened CancelBooking
CREATE OR ALTER PROCEDURE dbo.CancelBooking
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @CurrentStatus NVARCHAR(40);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @CurrentStatus = Status
        FROM dbo.Booking WITH (UPDLOCK, ROWLOCK)
        WHERE Id = @Id;

        IF @CurrentStatus IS NULL
        BEGIN
            COMMIT TRANSACTION;

            SELECT TOP (0) *
            FROM dbo.Booking;
            RETURN;
        END;

        IF @CurrentStatus = 'Completed'
        BEGIN
            THROW 50035, 'A completed booking cannot be cancelled.', 1;
        END;

        IF @CurrentStatus <> 'Cancelled'
        BEGIN
            UPDATE dbo.Booking
            SET Status = 'Cancelled'
            WHERE Id = @Id
              AND Status <> 'Cancelled'
              AND Status <> 'Completed';
        END;

        COMMIT TRANSACTION;

        SELECT *
        FROM dbo.Booking
        WHERE Id = @Id;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH;
END;
GO

-- 6. Hardened UpdateBooking
CREATE OR ALTER PROCEDURE [dbo].[UpdateBooking]
    @Id INT,
    @BookingDate DATE,
    @StartTime TIME,
    @EndTime TIME,
    @NumberOfGuests INT,
    @Status NVARCHAR(50),
    @SpecialRequest NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @RestaurantTableId INT;
    DECLARE @OldBookingDate DATE;
    DECLARE @CurrentStatus NVARCHAR(40);
    DECLARE @LockResult INT;
    DECLARE @OldLockResource NVARCHAR(255);
    DECLARE @NewLockResource NVARCHAR(255);

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Validate target booking status before acquiring additional locks
        IF @Status IS NULL
           OR @Status COLLATE Latin1_General_CS_AS NOT IN ('Pending', 'Confirmed', 'Cancelled', 'Completed')
        BEGIN
            THROW 50031, 'Invalid booking status.', 1;
        END;

        -- Lock the booking row first to serialize updates, cancellations, and payment confirmations
        SELECT
            @RestaurantTableId = RestaurantTableId,
            @OldBookingDate = BookingDate,
            @CurrentStatus = Status
        FROM dbo.Booking WITH (UPDLOCK, HOLDLOCK)
        WHERE Id = @Id;

        IF @RestaurantTableId IS NULL
        BEGIN
            COMMIT TRANSACTION;

            SELECT * FROM dbo.Booking WHERE Id = @Id;
            RETURN;
        END;

        IF @CurrentStatus = 'Cancelled' AND @Status <> 'Cancelled'
        BEGIN
            THROW 50032, 'A cancelled booking cannot be modified or confirmed.', 1;
        END;

        IF @CurrentStatus = 'Completed' AND @Status <> 'Completed'
        BEGIN
            THROW 50033, 'A completed booking cannot be modified or confirmed.', 1;
        END;

        IF @CurrentStatus = 'Confirmed' AND @Status = 'Pending'
        BEGIN
            THROW 50034, 'A confirmed booking cannot be reverted to Pending.', 1;
        END;

        SET @OldLockResource = CONCAT(
            'Booking_Table_', @RestaurantTableId,
            '_Date_', CONVERT(CHAR(10), @OldBookingDate, 23)
        );

        SET @NewLockResource = CONCAT(
            'Booking_Table_', @RestaurantTableId,
            '_Date_', CONVERT(CHAR(10), @BookingDate, 23)
        );

        IF @OldBookingDate <= @BookingDate
        BEGIN
            EXEC @LockResult = sys.sp_getapplock
                @Resource = @OldLockResource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 10000;

            IF @LockResult < 0
                THROW 50003, 'Unable to acquire booking lock. Please try again.', 1;

            IF @OldBookingDate <> @BookingDate
            BEGIN
                EXEC @LockResult = sys.sp_getapplock
                    @Resource = @NewLockResource,
                    @LockMode = 'Exclusive',
                    @LockOwner = 'Transaction',
                    @LockTimeout = 10000;

                IF @LockResult < 0
                    THROW 50003, 'Unable to acquire booking lock. Please try again.', 1;
            END;
        END
        ELSE
        BEGIN
            EXEC @LockResult = sys.sp_getapplock
                @Resource = @NewLockResource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 10000;

            IF @LockResult < 0
                THROW 50003, 'Unable to acquire booking lock. Please try again.', 1;

            EXEC @LockResult = sys.sp_getapplock
                @Resource = @OldLockResource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 10000;

            IF @LockResult < 0
                THROW 50003, 'Unable to acquire booking lock. Please try again.', 1;
        END;

        IF ISNULL(@Status, '') <> 'Cancelled'
        AND EXISTS
        (
            SELECT 1
            FROM dbo.Booking
            WHERE RestaurantTableId = @RestaurantTableId
              AND BookingDate = @BookingDate
              AND Status <> 'Cancelled'
              AND Id <> @Id
              AND StartTime < @EndTime
              AND EndTime > @StartTime
        )
        BEGIN
            THROW 50002, 'This table is already booked for the selected time.', 1;
        END;

        UPDATE dbo.Booking
        SET
            BookingDate = @BookingDate,
            StartTime = @StartTime,
            EndTime = @EndTime,
            NumberOfGuests = @NumberOfGuests,
            Status = @Status,
            SpecialRequest = @SpecialRequest
        WHERE Id = @Id;

        COMMIT TRANSACTION;

        SELECT * FROM dbo.Booking WHERE Id = @Id;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH;
END;
GO

