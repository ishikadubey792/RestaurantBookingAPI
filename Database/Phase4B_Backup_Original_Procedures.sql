/*
    Phase 4B Backup — Original SQL Server Stored Procedures
    Database: RestaurantBookingDB_SP
    Captured before applying Phase 4B Payment Hardening changes.
*/

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- 1. Original dbo.CreatePayment
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

    INSERT INTO dbo.Payments
        (BookingId, Amount, PaymentMethod, Status,
         TransactionId, CreatedAt, PaidAt)
    VALUES
        (@BookingId, @Amount, @PaymentMethod, @Status,
         @TransactionId, SYSDATETIME(), @PaidAt);

    DECLARE @NewId INT = SCOPE_IDENTITY();

    SELECT Id, BookingId, Amount, PaymentMethod, Status,
           TransactionId, CreatedAt, PaidAt
    FROM dbo.Payments
    WHERE Id = @NewId;
END;
GO

-- 2. Original dbo.UpdatePayment
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

    UPDATE dbo.Payments
    SET BookingId = @BookingId,
        Amount = @Amount,
        PaymentMethod = @PaymentMethod,
        Status = @Status,
        TransactionId = @TransactionId,
        PaidAt = @PaidAt
    WHERE Id = @Id;

    IF @@ROWCOUNT = 0
    BEGIN
        SELECT TOP (0) Id, BookingId, Amount, PaymentMethod, Status,
               TransactionId, CreatedAt, PaidAt
        FROM dbo.Payments;
        RETURN;
    END;

    SELECT Id, BookingId, Amount, PaymentMethod, Status,
           TransactionId, CreatedAt, PaidAt
    FROM dbo.Payments
    WHERE Id = @Id;
END;
GO

-- 3. Original dbo.CancelBooking
CREATE OR ALTER PROCEDURE dbo.CancelBooking
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Booking
    SET Status = 'Cancelled'
    WHERE Id = @Id
      AND Status <> 'Cancelled';

    SELECT *
    FROM dbo.Booking
    WHERE Id = @Id;
END;
GO

-- 4. Original dbo.UpdateBooking
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
    DECLARE @LockResult INT;
    DECLARE @OldLockResource NVARCHAR(255);
    DECLARE @NewLockResource NVARCHAR(255);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT
            @RestaurantTableId = RestaurantTableId,
            @OldBookingDate = BookingDate
        FROM dbo.Booking WITH (UPDLOCK, HOLDLOCK)
        WHERE Id = @Id;

        IF @RestaurantTableId IS NULL
        BEGIN
            COMMIT TRANSACTION;

            SELECT * FROM dbo.Booking WHERE Id = @Id;
            RETURN;
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

