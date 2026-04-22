/*
  File: db/10_modules/notifications/020_procs.sql
  Module: Notifications
  Purpose:
  - Create stored procedures for Notifications V1.
  - Refactored to match the current EmailDelivery table/domain shape.
*/

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

IF OBJECT_ID(N'[notifications].[EmailDeliveryAttempt]', N'U') IS NULL
BEGIN
    THROW 52205, 'Table [notifications].[EmailDeliveryAttempt] does not exist. Run notifications 001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[notifications].[EmailRateLimitLog]', N'U') IS NULL
BEGIN
    THROW 52206, 'Table [notifications].[EmailRateLimitLog] does not exist. Run notifications 001_tables.sql first.', 1;
END
GO

/* =========================================================
   [notifications].[OutboxMessage]
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
    @Priority          TINYINT = 5,
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
    FROM [notifications].[OutboxMessage]
    WHERE [MessageId] = @MessageId;
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[OutboxMessage_ClaimPending]
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
        FROM [notifications].[OutboxMessage] WITH (READPAST, UPDLOCK, ROWLOCK)
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
    FROM [notifications].[OutboxMessage] o
    INNER JOIN [ClaimSet] c
        ON o.[OutboxMessageId] = c.[OutboxMessageId];
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[OutboxMessage_MarkPublished]
    @OutboxMessageId BIGINT,
    @AffectedRows INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [notifications].[OutboxMessage]
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

CREATE OR ALTER PROCEDURE [notifications].[OutboxMessage_MarkFailed]
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

    UPDATE [notifications].[OutboxMessage]
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

CREATE OR ALTER PROCEDURE [notifications].[OutboxMessage_MarkDead]
    @OutboxMessageId BIGINT,
    @LastError NVARCHAR(2000) = NULL,
    @LastErrorCode NVARCHAR(100) = NULL,
    @LastErrorClass VARCHAR(30) = NULL,
    @AffectedRows INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [notifications].[OutboxMessage]
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

CREATE OR ALTER PROCEDURE [notifications].[OutboxMessage_ResetToPending]
    @OutboxMessageId BIGINT,
    @AffectedRows INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [notifications].[OutboxMessage]
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
        [LastErrorCode],
        [LastErrorClass],
        [OccurredAt],
        [CreatedAt],
        [UpdatedAt]
    FROM [notifications].[OutboxMessage]
    WHERE [CorrelationId] = @CorrelationId
    ORDER BY [OccurredAt] ASC, [OutboxMessageId] ASC;
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[OutboxMessage_DeletePublishedBefore]
    @PublishedBefore DATETIME2(3),
    @AffectedRows INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DELETE FROM [notifications].[OutboxMessage]
    WHERE [Status] = 'Published'
      AND [PublishedAt] IS NOT NULL
      AND [PublishedAt] < @PublishedBefore;

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

/* =========================================================
   [notifications].[EmailDelivery] - new shape
   ========================================================= */

CREATE OR ALTER PROCEDURE [notifications].[EmailDelivery_Insert]
    @MessageId          CHAR(26),
    @BusinessDedupeKey  NVARCHAR(300),
    @RecipientUserId    BIGINT = NULL,
    @ToEmail            NVARCHAR(320),
    @TemplateKey        NVARCHAR(100),
    @VariablesJson      NVARCHAR(MAX),
    @Provider           VARCHAR(30) = 'smtp',
    @Priority           TINYINT = 5,
    @CorrelationId      NVARCHAR(100) = NULL,
    @EmailDeliveryId    BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    INSERT INTO [notifications].[EmailDelivery]
    (
        [MessageId],
        [BusinessDedupeKey],
        [RecipientUserId],
        [ToEmail],
        [TemplateKey],
        [VariablesJson],
        [Provider],
        [Priority],
        [CorrelationId]
    )
    VALUES
    (
        @MessageId,
        @BusinessDedupeKey,
        @RecipientUserId,
        @ToEmail,
        @TemplateKey,
        @VariablesJson,
        @Provider,
        @Priority,
        @CorrelationId
    );

    SET @EmailDeliveryId = CONVERT(BIGINT, SCOPE_IDENTITY());
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[EmailDelivery_SelectById]
    @EmailDeliveryId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [EmailDeliveryId],
        [MessageId],
        [BusinessDedupeKey],
        [RecipientUserId],
        [ToEmail],
        [TemplateKey],
        [VariablesJson],
        [Provider],
        [Priority],
        [Status],
        [AttemptCount],
        [LastAttemptAt],
        [NextRetryAt],
        [SentAt],
        [LastErrorCode],
        [LastErrorClass],
        [CorrelationId],
        [CreatedAt],
        [UpdatedAt]
    FROM [notifications].[EmailDelivery]
    WHERE [EmailDeliveryId] = @EmailDeliveryId;
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
        [BusinessDedupeKey],
        [RecipientUserId],
        [ToEmail],
        [TemplateKey],
        [VariablesJson],
        [Provider],
        [Priority],
        [Status],
        [AttemptCount],
        [LastAttemptAt],
        [NextRetryAt],
        [SentAt],
        [LastErrorCode],
        [LastErrorClass],
        [CorrelationId],
        [CreatedAt],
        [UpdatedAt]
    FROM [notifications].[EmailDelivery]
    WHERE [MessageId] = @MessageId;
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[EmailDelivery_SelectByBusinessDedupeKey]
    @BusinessDedupeKey NVARCHAR(300)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [EmailDeliveryId],
        [MessageId],
        [BusinessDedupeKey],
        [RecipientUserId],
        [ToEmail],
        [TemplateKey],
        [VariablesJson],
        [Provider],
        [Priority],
        [Status],
        [AttemptCount],
        [LastAttemptAt],
        [NextRetryAt],
        [SentAt],
        [LastErrorCode],
        [LastErrorClass],
        [CorrelationId],
        [CreatedAt],
        [UpdatedAt]
    FROM [notifications].[EmailDelivery]
    WHERE [BusinessDedupeKey] = @BusinessDedupeKey;
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[EmailDelivery_Search]
    @Page INT = 1,
    @PageSize INT = 20,
    @FromCreatedAt DATETIME2(3) = NULL,
    @ToCreatedAt DATETIME2(3) = NULL,
    @RecipientUserId BIGINT = NULL,
    @TemplateKey NVARCHAR(100) = NULL,
    @Status VARCHAR(20) = NULL,
    @CorrelationId NVARCHAR(100) = NULL,
    @MessageId CHAR(26) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @Page IS NULL OR @Page <= 0
        SET @Page = 1;

    IF @PageSize IS NULL OR @PageSize <= 0
        SET @PageSize = 20;

    ;WITH [Filtered] AS
    (
        SELECT
            [EmailDeliveryId],
            [MessageId],
            [BusinessDedupeKey],
            [RecipientUserId],
            [ToEmail],
            [TemplateKey],
            [VariablesJson],
            [Provider],
            [Priority],
            [Status],
            [AttemptCount],
            [LastAttemptAt],
            [NextRetryAt],
            [SentAt],
            [LastErrorCode],
            [LastErrorClass],
            [CorrelationId],
            [CreatedAt],
            [UpdatedAt]
        FROM [notifications].[EmailDelivery]
        WHERE
            (@FromCreatedAt IS NULL OR [CreatedAt] >= @FromCreatedAt)
            AND (@ToCreatedAt IS NULL OR [CreatedAt] < @ToCreatedAt)
            AND (@RecipientUserId IS NULL OR [RecipientUserId] = @RecipientUserId)
            AND (@TemplateKey IS NULL OR [TemplateKey] = @TemplateKey)
            AND (@Status IS NULL OR [Status] = @Status)
            AND (@CorrelationId IS NULL OR [CorrelationId] = @CorrelationId)
            AND (@MessageId IS NULL OR [MessageId] = @MessageId)
    )
    SELECT
        [EmailDeliveryId],
        [MessageId],
        [BusinessDedupeKey],
        [RecipientUserId],
        [ToEmail],
        [TemplateKey],
        [VariablesJson],
        [Provider],
        [Priority],
        [Status],
        [AttemptCount],
        [LastAttemptAt],
        [NextRetryAt],
        [SentAt],
        [LastErrorCode],
        [LastErrorClass],
        [CorrelationId],
        [CreatedAt],
        [UpdatedAt],
        COUNT(1) OVER() AS [TotalCount]
    FROM [Filtered]
    ORDER BY [CreatedAt] DESC, [EmailDeliveryId] DESC
    OFFSET ((@Page - 1) * @PageSize) ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[EmailDelivery_ClaimPending]
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
            [EmailDeliveryId]
        FROM [notifications].[EmailDelivery] WITH (READPAST, UPDLOCK, ROWLOCK)
        WHERE
            (
                [Status] = 'Queued'
                AND ([NextRetryAt] IS NULL OR [NextRetryAt] <= @Now)
            )
            OR
            (
                [Status] IN ('Failed', 'Ambiguous')
                AND [NextRetryAt] IS NOT NULL
                AND [NextRetryAt] <= @Now
            )
        ORDER BY
            [Priority] ASC,
            CASE WHEN [NextRetryAt] IS NULL THEN [CreatedAt] ELSE [NextRetryAt] END ASC,
            [CreatedAt] ASC,
            [EmailDeliveryId] ASC
    )
    UPDATE d
    SET
        [Status] = 'Sending',
        [LastAttemptAt] = @Now,
        [UpdatedAt] = @Now
    OUTPUT
        INSERTED.[EmailDeliveryId],
        INSERTED.[MessageId],
        INSERTED.[BusinessDedupeKey],
        INSERTED.[RecipientUserId],
        INSERTED.[ToEmail],
        INSERTED.[TemplateKey],
        INSERTED.[VariablesJson],
        INSERTED.[Provider],
        INSERTED.[Priority],
        INSERTED.[Status],
        INSERTED.[AttemptCount],
        INSERTED.[LastAttemptAt],
        INSERTED.[NextRetryAt],
        INSERTED.[SentAt],
        INSERTED.[LastErrorCode],
        INSERTED.[LastErrorClass],
        INSERTED.[CorrelationId],
        INSERTED.[CreatedAt],
        INSERTED.[UpdatedAt]
    FROM [notifications].[EmailDelivery] d
    INNER JOIN [ClaimSet] c
        ON d.[EmailDeliveryId] = c.[EmailDeliveryId];
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[EmailDelivery_MarkSent]
    @EmailDeliveryId BIGINT,
    @AffectedRows INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [notifications].[EmailDelivery]
    SET
        [Status] = 'Sent',
        [AttemptCount] = [AttemptCount] + 1,
        [SentAt] = SYSUTCDATETIME(),
        [NextRetryAt] = NULL,
        [LastErrorCode] = NULL,
        [LastErrorClass] = NULL,
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [EmailDeliveryId] = @EmailDeliveryId
      AND [Status] IN ('Queued', 'Sending', 'Failed', 'Ambiguous');

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[EmailDelivery_MarkFailed]
    @EmailDeliveryId BIGINT,
    @NextRetryAt DATETIME2(3) = NULL,
    @LastErrorCode NVARCHAR(100) = NULL,
    @LastErrorClass VARCHAR(30) = NULL,
    @AffectedRows INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [notifications].[EmailDelivery]
    SET
        [Status] = 'Failed',
        [AttemptCount] = [AttemptCount] + 1,
        [NextRetryAt] = @NextRetryAt,
        [LastErrorCode] = @LastErrorCode,
        [LastErrorClass] = @LastErrorClass,
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [EmailDeliveryId] = @EmailDeliveryId
      AND [Status] IN ('Queued', 'Sending', 'Failed', 'Ambiguous');

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[EmailDelivery_MarkDead]
    @EmailDeliveryId BIGINT,
    @LastErrorCode NVARCHAR(100) = NULL,
    @LastErrorClass VARCHAR(30) = NULL,
    @AffectedRows INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [notifications].[EmailDelivery]
    SET
        [Status] = 'Dead',
        [AttemptCount] = [AttemptCount] + 1,
        [NextRetryAt] = NULL,
        [LastErrorCode] = @LastErrorCode,
        [LastErrorClass] = @LastErrorClass,
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [EmailDeliveryId] = @EmailDeliveryId
      AND [Status] IN ('Queued', 'Sending', 'Failed', 'Ambiguous');

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[EmailDelivery_MarkSuppressed]
    @EmailDeliveryId BIGINT,
    @LastErrorCode NVARCHAR(100) = NULL,
    @LastErrorClass VARCHAR(30) = 'Policy',
    @AffectedRows INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [notifications].[EmailDelivery]
    SET
        [Status] = 'Suppressed',
        [NextRetryAt] = NULL,
        [LastErrorCode] = @LastErrorCode,
        [LastErrorClass] = @LastErrorClass,
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [EmailDeliveryId] = @EmailDeliveryId
      AND [Status] IN ('Queued', 'Sending', 'Failed', 'Ambiguous');

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[EmailDelivery_MarkAmbiguous]
    @EmailDeliveryId BIGINT,
    @NextRetryAt DATETIME2(3) = NULL,
    @LastErrorCode NVARCHAR(100) = NULL,
    @LastErrorClass VARCHAR(30) = 'Ambiguous',
    @AffectedRows INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [notifications].[EmailDelivery]
    SET
        [Status] = 'Ambiguous',
        [AttemptCount] = [AttemptCount] + 1,
        [NextRetryAt] = @NextRetryAt,
        [LastErrorCode] = @LastErrorCode,
        [LastErrorClass] = @LastErrorClass,
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [EmailDeliveryId] = @EmailDeliveryId
      AND [Status] IN ('Queued', 'Sending', 'Failed', 'Ambiguous');

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[EmailDelivery_ResetToQueued]
    @EmailDeliveryId BIGINT,
    @AffectedRows INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [notifications].[EmailDelivery]
    SET
        [Status] = 'Queued',
        [NextRetryAt] = NULL,
        [LastErrorCode] = NULL,
        [LastErrorClass] = NULL,
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [EmailDeliveryId] = @EmailDeliveryId
      AND [Status] IN ('Failed', 'Dead', 'Ambiguous', 'Sending');

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

/* =========================================================
   [notifications].[EmailDeliveryAttempt]
   ========================================================= */

CREATE OR ALTER PROCEDURE [notifications].[EmailDeliveryAttempt_Insert]
    @EmailDeliveryId BIGINT,
    @MessageId CHAR(26),
    @AttemptNumber INT,
    @StartedAt DATETIME2(3) = NULL,
    @FinishedAt DATETIME2(3) = NULL,
    @Outcome VARCHAR(30),
    @IsAmbiguous BIT = 0,
    @ProviderMessageId NVARCHAR(200) = NULL,
    @ProviderErrorCode NVARCHAR(100) = NULL,
    @ErrorClass VARCHAR(30) = NULL,
    @ErrorDetail NVARCHAR(2000) = NULL,
    @CorrelationId NVARCHAR(100) = NULL,
    @EmailDeliveryAttemptId BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @StartedAt IS NULL
        SET @StartedAt = SYSUTCDATETIME();

    INSERT INTO [notifications].[EmailDeliveryAttempt]
    (
        [EmailDeliveryId],
        [MessageId],
        [AttemptNumber],
        [StartedAt],
        [FinishedAt],
        [Outcome],
        [IsAmbiguous],
        [ProviderMessageId],
        [ProviderErrorCode],
        [ErrorClass],
        [ErrorDetail],
        [CorrelationId]
    )
    VALUES
    (
        @EmailDeliveryId,
        @MessageId,
        @AttemptNumber,
        @StartedAt,
        @FinishedAt,
        @Outcome,
        @IsAmbiguous,
        @ProviderMessageId,
        @ProviderErrorCode,
        @ErrorClass,
        @ErrorDetail,
        @CorrelationId
    );

    SET @EmailDeliveryAttemptId = CONVERT(BIGINT, SCOPE_IDENTITY());
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[EmailDeliveryAttempt_SelectByEmailDeliveryId]
    @EmailDeliveryId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [EmailDeliveryAttemptId],
        [EmailDeliveryId],
        [MessageId],
        [AttemptNumber],
        [StartedAt],
        [FinishedAt],
        [Outcome],
        [IsAmbiguous],
        [ProviderMessageId],
        [ProviderErrorCode],
        [ErrorClass],
        [ErrorDetail],
        [CorrelationId],
        [CreatedAt]
    FROM [notifications].[EmailDeliveryAttempt]
    WHERE [EmailDeliveryId] = @EmailDeliveryId
    ORDER BY [AttemptNumber] ASC, [EmailDeliveryAttemptId] ASC;
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[EmailDeliveryAttempt_SelectByMessageId]
    @MessageId CHAR(26)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [EmailDeliveryAttemptId],
        [EmailDeliveryId],
        [MessageId],
        [AttemptNumber],
        [StartedAt],
        [FinishedAt],
        [Outcome],
        [IsAmbiguous],
        [ProviderMessageId],
        [ProviderErrorCode],
        [ErrorClass],
        [ErrorDetail],
        [CorrelationId],
        [CreatedAt]
    FROM [notifications].[EmailDeliveryAttempt]
    WHERE [MessageId] = @MessageId
    ORDER BY [AttemptNumber] ASC, [EmailDeliveryAttemptId] ASC;
END;
GO

/* =========================================================
   [notifications].[EmailRateLimitLog]
   ========================================================= */

CREATE OR ALTER PROCEDURE [notifications].[EmailRateLimitLog_Insert]
    @RecipientUserId BIGINT = NULL,
    @Endpoint NVARCHAR(60),
    @ToEmail NVARCHAR(320) = NULL,
    @ToEmailHash VARCHAR(64) = NULL,
    @IpAddress NVARCHAR(45) = NULL,
    @Allowed BIT,
    @Reason NVARCHAR(120) = NULL,
    @DecisionKey NVARCHAR(150) = NULL,
    @CorrelationId NVARCHAR(100) = NULL,
    @EmailRateLimitLogId BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    INSERT INTO [notifications].[EmailRateLimitLog]
    (
        [RecipientUserId],
        [Endpoint],
        [ToEmail],
        [ToEmailHash],
        [IpAddress],
        [Allowed],
        [Reason],
        [DecisionKey],
        [CorrelationId]
    )
    VALUES
    (
        @RecipientUserId,
        @Endpoint,
        @ToEmail,
        @ToEmailHash,
        @IpAddress,
        @Allowed,
        @Reason,
        @DecisionKey,
        @CorrelationId
    );

    SET @EmailRateLimitLogId = CONVERT(BIGINT, SCOPE_IDENTITY());
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[EmailRateLimitLog_SelectRange]
    @FromOccurredAt DATETIME2(3),
    @ToOccurredAt DATETIME2(3),
    @Endpoint NVARCHAR(60) = NULL,
    @RecipientUserId BIGINT = NULL,
    @ToEmailHash VARCHAR(64) = NULL,
    @Allowed BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [EmailRateLimitLogId],
        [RecipientUserId],
        [Endpoint],
        [ToEmail],
        [ToEmailHash],
        [IpAddress],
        [Allowed],
        [Reason],
        [DecisionKey],
        [CorrelationId],
        [OccurredAt]
    FROM [notifications].[EmailRateLimitLog]
    WHERE [OccurredAt] >= @FromOccurredAt
      AND [OccurredAt] < @ToOccurredAt
      AND (@Endpoint IS NULL OR [Endpoint] = @Endpoint)
      AND (@RecipientUserId IS NULL OR [RecipientUserId] = @RecipientUserId)
      AND (@ToEmailHash IS NULL OR [ToEmailHash] = @ToEmailHash)
      AND (@Allowed IS NULL OR [Allowed] = @Allowed)
    ORDER BY [OccurredAt] DESC, [EmailRateLimitLogId] DESC;
END;
GO