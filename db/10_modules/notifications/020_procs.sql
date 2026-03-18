/*
  File: db/10_modules/notifications/020_procs.sql
  Module: Notifications
  Purpose:
  - Create stored procedures for Notifications V1.
  - Includes:
      * shared outbox operations ([notifications].[OutboxMessage])
      * email delivery operations ([notifications].[EmailDelivery])

  Notes:
  - Broker is transport only; SQL remains durable truth.
  - Outbox is shared across modules (Identity, Content, etc.).
  - Application/service layer still owns orchestration and payload construction.
*/

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

USE [CommercialNews];
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 52201, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

IF SCHEMA_ID(N'notifications') IS NULL
BEGIN
    THROW 52202, 'Schema [notifications] does not exist. Run bootstrap scripts first.', 1;
END
GO

IF OBJECT_ID(N'[notifications].[OutboxMessage]', N'U') IS NULL
BEGIN
    THROW 52203, 'Table [notifications].[OutboxMessage] does not exist. Run notifications 001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[notifications].[EmailDelivery]', N'U') IS NULL
BEGIN
    THROW 52204, 'Table [notifications].[EmailDelivery] does not exist. Run notifications 001_tables.sql first.', 1;
END
GO

/* =========================================================
   OutboxMessage
   ========================================================= */

CREATE OR ALTER PROCEDURE [notifications].[OutboxMessage_Insert]
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
    @OccurredAt        DATETIME2(3),
    @OutboxMessageId   BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    INSERT INTO [notifications].[OutboxMessage]
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
        @OccurredAt
    );

    SET @OutboxMessageId = SCOPE_IDENTITY();
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[OutboxMessage_SelectById]
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
        [Status],
        [AttemptCount],
        [NextRetryAt],
        [LastAttemptAt],
        [PublishedAt],
        [LastError],
        [OccurredAt],
        [CreatedAt],
        [UpdatedAt]
    FROM [notifications].[OutboxMessage]
    WHERE [OutboxMessageId] = @OutboxMessageId;
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[OutboxMessage_SelectByMessageId]
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
        [Status],
        [AttemptCount],
        [NextRetryAt],
        [LastAttemptAt],
        [PublishedAt],
        [LastError],
        [OccurredAt],
        [CreatedAt],
        [UpdatedAt]
    FROM [notifications].[OutboxMessage]
    WHERE [MessageId] = @MessageId;
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[OutboxMessage_SelectPending]
    @TopN INT = 100,
    @Now DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @TopN IS NULL OR @TopN <= 0
        SET @TopN = 100;

    IF @Now IS NULL
        SET @Now = SYSUTCDATETIME();

    SELECT TOP (@TopN)
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
        [Status],
        [AttemptCount],
        [NextRetryAt],
        [LastAttemptAt],
        [PublishedAt],
        [LastError],
        [OccurredAt],
        [CreatedAt],
        [UpdatedAt]
    FROM [notifications].[OutboxMessage]
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
        CASE WHEN [NextRetryAt] IS NULL THEN [OccurredAt] ELSE [NextRetryAt] END ASC,
        [OccurredAt] ASC,
        [OutboxMessageId] ASC;
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[OutboxMessage_MarkProcessing]
    @OutboxMessageId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [notifications].[OutboxMessage]
    SET
        [Status] = 'Processing',
        [LastAttemptAt] = SYSUTCDATETIME(),
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [OutboxMessageId] = @OutboxMessageId
      AND [Status] IN ('Pending', 'Failed');
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[OutboxMessage_MarkPublished]
    @OutboxMessageId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [notifications].[OutboxMessage]
    SET
        [Status] = 'Published',
        [PublishedAt] = SYSUTCDATETIME(),
        [LastError] = NULL,
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [OutboxMessageId] = @OutboxMessageId
      AND [Status] IN ('Pending', 'Processing', 'Failed');
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[OutboxMessage_MarkFailed]
    @OutboxMessageId BIGINT,
    @NextRetryAt DATETIME2(3) = NULL,
    @LastError NVARCHAR(2000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [notifications].[OutboxMessage]
    SET
        [Status] = 'Failed',
        [AttemptCount] = [AttemptCount] + 1,
        [NextRetryAt] = @NextRetryAt,
        [LastError] = @LastError,
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [OutboxMessageId] = @OutboxMessageId
      AND [Status] IN ('Pending', 'Processing', 'Failed');
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[OutboxMessage_MarkDeadLetter]
    @OutboxMessageId BIGINT,
    @LastError NVARCHAR(2000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [notifications].[OutboxMessage]
    SET
        [Status] = 'DeadLetter',
        [AttemptCount] = [AttemptCount] + 1,
        [NextRetryAt] = NULL,
        [LastError] = @LastError,
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [OutboxMessageId] = @OutboxMessageId
      AND [Status] IN ('Pending', 'Processing', 'Failed');
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[OutboxMessage_ResetToPending]
    @OutboxMessageId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [notifications].[OutboxMessage]
    SET
        [Status] = 'Pending',
        [NextRetryAt] = NULL,
        [LastError] = NULL,
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [OutboxMessageId] = @OutboxMessageId
      AND [Status] IN ('Failed', 'DeadLetter', 'Processing');
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[OutboxMessage_SelectByAggregate]
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
        [CorrelationId],
        [Status],
        [AttemptCount],
        [NextRetryAt],
        [LastAttemptAt],
        [PublishedAt],
        [LastError],
        [OccurredAt],
        [CreatedAt],
        [UpdatedAt]
    FROM [notifications].[OutboxMessage]
    WHERE [AggregateType] = @AggregateType
      AND [AggregateId] = @AggregateId
    ORDER BY
        [AggregateVersion] ASC,
        [OccurredAt] ASC,
        [OutboxMessageId] ASC;
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[OutboxMessage_SelectByCorrelationId]
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
        [Status],
        [AttemptCount],
        [NextRetryAt],
        [LastAttemptAt],
        [PublishedAt],
        [LastError],
        [OccurredAt],
        [CreatedAt],
        [UpdatedAt]
    FROM [notifications].[OutboxMessage]
    WHERE [CorrelationId] = @CorrelationId
    ORDER BY [OccurredAt] ASC, [OutboxMessageId] ASC;
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[OutboxMessage_SelectByStatus]
    @Status VARCHAR(20),
    @TopN INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    IF @TopN IS NULL OR @TopN <= 0
        SET @TopN = 100;

    SELECT TOP (@TopN)
        [OutboxMessageId],
        [MessageId],
        [EventType],
        [AggregateType],
        [AggregateId],
        [AggregatePublicId],
        [AggregateVersion],
        [CorrelationId],
        [Status],
        [AttemptCount],
        [NextRetryAt],
        [LastAttemptAt],
        [PublishedAt],
        [LastError],
        [OccurredAt],
        [CreatedAt],
        [UpdatedAt]
    FROM [notifications].[OutboxMessage]
    WHERE [Status] = @Status
    ORDER BY [OccurredAt] ASC, [OutboxMessageId] ASC;
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[OutboxMessage_SelectRangeByOccurredAt]
    @FromOccurredAt DATETIME2(3),
    @ToOccurredAt DATETIME2(3)
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
        [Status],
        [AttemptCount],
        [NextRetryAt],
        [LastAttemptAt],
        [PublishedAt],
        [LastError],
        [OccurredAt],
        [CreatedAt],
        [UpdatedAt]
    FROM [notifications].[OutboxMessage]
    WHERE [OccurredAt] >= @FromOccurredAt
      AND [OccurredAt] < @ToOccurredAt
    ORDER BY [OccurredAt] ASC, [OutboxMessageId] ASC;
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[OutboxMessage_DeletePublishedBefore]
    @PublishedBefore DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DELETE FROM [notifications].[OutboxMessage]
    WHERE [Status] = 'Published'
      AND [PublishedAt] IS NOT NULL
      AND [PublishedAt] < @PublishedBefore;
END;
GO

/* =========================================================
   EmailDelivery
   ========================================================= */

CREATE OR ALTER PROCEDURE [notifications].[EmailDelivery_Insert]
    @MessageId         CHAR(26),
    @UserId            BIGINT = NULL,
    @ToEmail           NVARCHAR(320),
    @TemplateKey       NVARCHAR(100),
    @Subject           NVARCHAR(300) = NULL,
    @CorrelationId     NVARCHAR(100) = NULL,
    @EmailDeliveryId   BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    INSERT INTO [notifications].[EmailDelivery]
    (
        [MessageId],
        [UserId],
        [ToEmail],
        [TemplateKey],
        [Subject],
        [CorrelationId]
    )
    VALUES
    (
        @MessageId,
        @UserId,
        @ToEmail,
        @TemplateKey,
        @Subject,
        @CorrelationId
    );

    SET @EmailDeliveryId = SCOPE_IDENTITY();
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[EmailDelivery_SelectByMessageId]
    @MessageId CHAR(26)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [EmailDeliveryId],
        [MessageId],
        [UserId],
        [ToEmail],
        [TemplateKey],
        [Subject],
        [Status],
        [AttemptCount],
        [ProviderMessageId],
        [LastAttemptAt],
        [SentAt],
        [NextRetryAt],
        [LastError],
        [CorrelationId],
        [CreatedAt],
        [UpdatedAt]
    FROM [notifications].[EmailDelivery]
    WHERE [MessageId] = @MessageId;
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[EmailDelivery_SelectPending]
    @TopN INT = 100,
    @Now DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @TopN IS NULL OR @TopN <= 0
        SET @TopN = 100;

    IF @Now IS NULL
        SET @Now = SYSUTCDATETIME();

    SELECT TOP (@TopN)
        [EmailDeliveryId],
        [MessageId],
        [UserId],
        [ToEmail],
        [TemplateKey],
        [Subject],
        [Status],
        [AttemptCount],
        [ProviderMessageId],
        [LastAttemptAt],
        [SentAt],
        [NextRetryAt],
        [LastError],
        [CorrelationId],
        [CreatedAt],
        [UpdatedAt]
    FROM [notifications].[EmailDelivery]
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
        CASE WHEN [NextRetryAt] IS NULL THEN [CreatedAt] ELSE [NextRetryAt] END ASC,
        [CreatedAt] ASC,
        [EmailDeliveryId] ASC;
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[EmailDelivery_MarkProcessing]
    @EmailDeliveryId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [notifications].[EmailDelivery]
    SET
        [Status] = 'Processing',
        [LastAttemptAt] = SYSUTCDATETIME(),
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [EmailDeliveryId] = @EmailDeliveryId
      AND [Status] IN ('Pending', 'Failed');
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[EmailDelivery_MarkSent]
    @EmailDeliveryId   BIGINT,
    @ProviderMessageId NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [notifications].[EmailDelivery]
    SET
        [Status] = 'Sent',
        [ProviderMessageId] = @ProviderMessageId,
        [SentAt] = SYSUTCDATETIME(),
        [LastError] = NULL,
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [EmailDeliveryId] = @EmailDeliveryId
      AND [Status] IN ('Pending', 'Processing', 'Failed');
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[EmailDelivery_MarkFailed]
    @EmailDeliveryId BIGINT,
    @NextRetryAt DATETIME2(3) = NULL,
    @LastError NVARCHAR(2000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [notifications].[EmailDelivery]
    SET
        [Status] = 'Failed',
        [AttemptCount] = [AttemptCount] + 1,
        [NextRetryAt] = @NextRetryAt,
        [LastError] = @LastError,
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [EmailDeliveryId] = @EmailDeliveryId
      AND [Status] IN ('Pending', 'Processing', 'Failed');
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[EmailDelivery_MarkDeadLetter]
    @EmailDeliveryId BIGINT,
    @LastError NVARCHAR(2000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [notifications].[EmailDelivery]
    SET
        [Status] = 'DeadLetter',
        [AttemptCount] = [AttemptCount] + 1,
        [NextRetryAt] = NULL,
        [LastError] = @LastError,
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [EmailDeliveryId] = @EmailDeliveryId
      AND [Status] IN ('Pending', 'Processing', 'Failed');
END;
GO