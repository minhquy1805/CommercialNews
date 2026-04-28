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
                [Status] IN ('Failed')
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
        [AttemptCount] = [AttemptCount] + 1,
        [LastAttemptAt] = @Now,
        [NextRetryAt] = NULL,
        [LastErrorCode] = NULL,
        [LastErrorClass] = NULL,
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
        [SentAt] = SYSUTCDATETIME(),
        [NextRetryAt] = NULL,
        [LastErrorCode] = NULL,
        [LastErrorClass] = NULL,
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [EmailDeliveryId] = @EmailDeliveryId
      AND [Status] = 'Sending';

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
        [NextRetryAt] = @NextRetryAt,
        [LastErrorCode] = @LastErrorCode,
        [LastErrorClass] = @LastErrorClass,
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [EmailDeliveryId] = @EmailDeliveryId
      AND [Status] = 'Sending';

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
        [NextRetryAt] = NULL,
        [LastErrorCode] = @LastErrorCode,
        [LastErrorClass] = @LastErrorClass,
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [EmailDeliveryId] = @EmailDeliveryId
      AND [Status] IN ('Sending', 'Failed');

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [notifications].[EmailDelivery_RequeueForRetry]
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
      AND [Status] = 'Failed';

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
    @Outcome VARCHAR(30) = 'Started',
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

CREATE OR ALTER PROCEDURE [notifications].[EmailDeliveryAttempt_UpdateOutcome]
    @EmailDeliveryAttemptId BIGINT,
    @FinishedAt DATETIME2(3),
    @Outcome VARCHAR(30),
    @IsAmbiguous BIT = 0,
    @ProviderMessageId NVARCHAR(200) = NULL,
    @ProviderErrorCode NVARCHAR(100) = NULL,
    @ErrorClass VARCHAR(30) = NULL,
    @ErrorDetail NVARCHAR(2000) = NULL,
    @AffectedRows INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [notifications].[EmailDeliveryAttempt]
    SET
        [FinishedAt] = @FinishedAt,
        [Outcome] = @Outcome,
        [IsAmbiguous] = @IsAmbiguous,
        [ProviderMessageId] = @ProviderMessageId,
        [ProviderErrorCode] = @ProviderErrorCode,
        [ErrorClass] = @ErrorClass,
        [ErrorDetail] = @ErrorDetail
    WHERE [EmailDeliveryAttemptId] = @EmailDeliveryAttemptId
      AND [FinishedAt] IS NULL;

    SET @AffectedRows = @@ROWCOUNT;
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