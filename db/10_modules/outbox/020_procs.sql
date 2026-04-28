SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 52201, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

USE [CommercialNews];
GO

IF OBJECT_ID(N'[outbox].[OutboxMessage]', N'U') IS NULL
BEGIN
    THROW 52203, 'Table [outbox].[OutboxMessage] does not exist. Run outbox 001_tables.sql first.', 1;
END
GO

/* =========================================================
   [outbox].[OutboxMessage]
   ========================================================= */

CREATE OR ALTER PROCEDURE [outbox].[OutboxMessage_Insert]
    @MessageId         CHAR(26),
    @EventType         NVARCHAR(200),
    @AggregateType     NVARCHAR(100),
    @AggregateId       NVARCHAR(100),
    @AggregatePublicId CHAR(26) = NULL,
    @AggregateVersion  INT = NULL,
    @Payload           NVARCHAR(MAX),
    @Headers           NVARCHAR(MAX) = NULL,
    @CorrelationId     NVARCHAR(100) = NULL,
    @InitiatorUserId   BIGINT = NULL,
    @Priority          TINYINT = 5,
    @OccurredAt        DATETIME2(3),
    @OutboxMessageId   BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    INSERT INTO [outbox].[OutboxMessage]
    (
        [MessageId],
        [EventType],
        [AggregateType],
        [AggregateId],
        [AggregatePublicId],
        [AggregateVersion],
        [Payload],
        [Headers],
        [CorrelationId],
        [InitiatorUserId],
        [Priority],
        [OccurredAt]
    )
    VALUES
    (
        @MessageId,
        @EventType,
        @AggregateType,
        @AggregateId,
        @AggregatePublicId,
        @AggregateVersion,
        @Payload,
        @Headers,
        @CorrelationId,
        @InitiatorUserId,
        @Priority,
        @OccurredAt
    );

    SET @OutboxMessageId = CONVERT(BIGINT, SCOPE_IDENTITY());
END;
GO

CREATE OR ALTER PROCEDURE [outbox].[OutboxMessage_SelectById]
    @OutboxMessageId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [OutboxMessageId],
        [MessageId],
        [EventType],
        [AggregateType],
        [AggregateId],
        [AggregatePublicId],
        [AggregateVersion],
        [Payload],
        [Headers],
        [CorrelationId],
        [InitiatorUserId],
        [Priority],
        [Status],
        [AttemptCount],
        [NextRetryAt],
        [LastAttemptAt],
        [PublishedAt],
        [LastError],
        [LastErrorCode],
        [LastErrorClass],
        [OccurredAt],
        [CreatedAt],
        [UpdatedAt]
    FROM [outbox].[OutboxMessage]
    WHERE [OutboxMessageId] = @OutboxMessageId;
END;
GO

CREATE OR ALTER PROCEDURE [outbox].[OutboxMessage_SelectByMessageId]
    @MessageId CHAR(26)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [OutboxMessageId],
        [MessageId],
        [EventType],
        [AggregateType],
        [AggregateId],
        [AggregatePublicId],
        [AggregateVersion],
        [Payload],
        [Headers],
        [CorrelationId],
        [InitiatorUserId],
        [Priority],
        [Status],
        [AttemptCount],
        [NextRetryAt],
        [LastAttemptAt],
        [PublishedAt],
        [LastError],
        [LastErrorCode],
        [LastErrorClass],
        [OccurredAt],
        [CreatedAt],
        [UpdatedAt]
    FROM [outbox].[OutboxMessage]
    WHERE [MessageId] = @MessageId;
END;
GO

CREATE OR ALTER PROCEDURE [outbox].[OutboxMessage_ClaimPending]
    @TopN INT = 100,
    @Now DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @TopN IS NULL OR @TopN <= 0
        SET @TopN = 100;

    IF @Now IS NULL
        SET @Now = SYSUTCDATETIME();

    ;WITH [ClaimSet] AS
    (
        SELECT TOP (@TopN)
            [OutboxMessageId]
        FROM [outbox].[OutboxMessage] WITH (READPAST, UPDLOCK, ROWLOCK)
        WHERE
            (
                [Status] = 'Pending'
                AND ([NextRetryAt] IS NULL OR [NextRetryAt] <= @Now)
            )
            OR
            (
                [Status] = 'Failed'
                AND [NextRetryAt] IS NOT NULL
                AND [NextRetryAt] <= @Now
            )
        ORDER BY
            [Priority] ASC,
            CASE WHEN [NextRetryAt] IS NULL THEN [OccurredAt] ELSE [NextRetryAt] END ASC,
            [OccurredAt] ASC,
            [OutboxMessageId] ASC
    )
    UPDATE o
    SET
        [Status] = 'Publishing',
        [LastAttemptAt] = @Now,
        [UpdatedAt] = @Now
    OUTPUT
        INSERTED.[OutboxMessageId],
        INSERTED.[MessageId],
        INSERTED.[EventType],
        INSERTED.[AggregateType],
        INSERTED.[AggregateId],
        INSERTED.[AggregatePublicId],
        INSERTED.[AggregateVersion],
        INSERTED.[Payload],
        INSERTED.[Headers],
        INSERTED.[CorrelationId],
        INSERTED.[InitiatorUserId],
        INSERTED.[Priority],
        INSERTED.[Status],
        INSERTED.[AttemptCount],
        INSERTED.[NextRetryAt],
        INSERTED.[LastAttemptAt],
        INSERTED.[PublishedAt],
        INSERTED.[LastError],
        INSERTED.[LastErrorCode],
        INSERTED.[LastErrorClass],
        INSERTED.[OccurredAt],
        INSERTED.[CreatedAt],
        INSERTED.[UpdatedAt]
    FROM [outbox].[OutboxMessage] o
    INNER JOIN [ClaimSet] c
        ON o.[OutboxMessageId] = c.[OutboxMessageId];
END;
GO

CREATE OR ALTER PROCEDURE [outbox].[OutboxMessage_MarkPublished]
    @OutboxMessageId BIGINT,
    @AffectedRows INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [outbox].[OutboxMessage]
    SET
        [Status] = 'Published',
        [PublishedAt] = SYSUTCDATETIME(),
        [LastError] = NULL,
        [LastErrorCode] = NULL,
        [LastErrorClass] = NULL,
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [OutboxMessageId] = @OutboxMessageId
      AND [Status] IN ('Publishing', 'Failed');

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [outbox].[OutboxMessage_MarkFailed]
    @OutboxMessageId BIGINT,
    @NextRetryAt DATETIME2(3) = NULL,
    @LastError NVARCHAR(2000) = NULL,
    @LastErrorCode NVARCHAR(100) = NULL,
    @LastErrorClass VARCHAR(30) = NULL,
    @AffectedRows INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [outbox].[OutboxMessage]
    SET
        [Status] = 'Failed',
        [AttemptCount] = [AttemptCount] + 1,
        [NextRetryAt] = @NextRetryAt,
        [LastError] = @LastError,
        [LastErrorCode] = @LastErrorCode,
        [LastErrorClass] = @LastErrorClass,
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [OutboxMessageId] = @OutboxMessageId
      AND [Status] IN ('Publishing', 'Failed');

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [outbox].[OutboxMessage_MarkDead]
    @OutboxMessageId BIGINT,
    @LastError NVARCHAR(2000) = NULL,
    @LastErrorCode NVARCHAR(100) = NULL,
    @LastErrorClass VARCHAR(30) = NULL,
    @AffectedRows INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [outbox].[OutboxMessage]
    SET
        [Status] = 'Dead',
        [AttemptCount] = [AttemptCount] + 1,
        [NextRetryAt] = NULL,
        [LastError] = @LastError,
        [LastErrorCode] = @LastErrorCode,
        [LastErrorClass] = @LastErrorClass,
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [OutboxMessageId] = @OutboxMessageId
      AND [Status] IN ('Publishing', 'Failed');

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [outbox].[OutboxMessage_ResetToPending]
    @OutboxMessageId BIGINT,
    @AffectedRows INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [outbox].[OutboxMessage]
    SET
        [Status] = 'Pending',
        [NextRetryAt] = NULL,
        [LastError] = NULL,
        [LastErrorCode] = NULL,
        [LastErrorClass] = NULL,
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [OutboxMessageId] = @OutboxMessageId
      AND [Status] IN ('Failed', 'Dead', 'Publishing');

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [outbox].[OutboxMessage_SelectByAggregate]
    @AggregateType NVARCHAR(100),
    @AggregateId NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [OutboxMessageId],
        [MessageId],
        [EventType],
        [AggregateType],
        [AggregateId],
        [AggregatePublicId],
        [AggregateVersion],
        [Payload],
        [Headers],
        [CorrelationId],
        [InitiatorUserId],
        [Priority],
        [Status],
        [AttemptCount],
        [NextRetryAt],
        [LastAttemptAt],
        [PublishedAt],
        [LastError],
        [LastErrorCode],
        [LastErrorClass],
        [OccurredAt],
        [CreatedAt],
        [UpdatedAt]
    FROM [outbox].[OutboxMessage]
    WHERE [AggregateType] = @AggregateType
      AND [AggregateId] = @AggregateId
    ORDER BY
        [AggregateVersion] ASC,
        [OccurredAt] ASC,
        [OutboxMessageId] ASC;
END;
GO

CREATE OR ALTER PROCEDURE [outbox].[OutboxMessage_DeletePublishedBefore]
    @PublishedBefore DATETIME2(3),
    @AffectedRows INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DELETE FROM [outbox].[OutboxMessage]
    WHERE [Status] = 'Published'
      AND [PublishedAt] IS NOT NULL
      AND [PublishedAt] < @PublishedBefore;

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [outbox].[OutboxMessage_SelectByCorrelationId]
    @CorrelationId NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [OutboxMessageId],
        [MessageId],
        [EventType],
        [AggregateType],
        [AggregateId],
        [AggregatePublicId],
        [AggregateVersion],
        [Payload],
        [Headers],
        [CorrelationId],
        [InitiatorUserId],
        [Priority],
        [Status],
        [AttemptCount],
        [NextRetryAt],
        [LastAttemptAt],
        [PublishedAt],
        [LastError],
        [LastErrorCode],
        [LastErrorClass],
        [OccurredAt],
        [CreatedAt],
        [UpdatedAt]
    FROM [outbox].[OutboxMessage]
    WHERE [CorrelationId] = @CorrelationId
    ORDER BY [OccurredAt] ASC, [OutboxMessageId] ASC;
END;
GO