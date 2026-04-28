/*
  File: db/10_modules/notifications/010_indexes.sql
  Module: Notifications
  Purpose:
  - Create non-PK/non-constraint indexes for Notifications tables in CommercialNews V1.
  - Match the current Notifications domain shape:
      * [notifications].[EmailDelivery]
      * [notifications].[EmailDeliveryAttempt]
      * [notifications].[EmailRateLimitLog]

  Notes:
  - Idempotent: safe to re-run.
  - PK / UQ / FK / CHECK constraints are defined in 001_tables.sql.
  - Shared producer-side outbox indexes belong to:
      db/10_modules/outbox/010_indexes.sql
  - Index design focuses on:
      * email delivery worker polling / retry scheduling
      * admin ops inspection
      * correlation-based troubleshooting
      * recipient/user lookup
      * rate-limit investigation
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 52101, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

USE [CommercialNews];
GO

IF SCHEMA_ID(N'notifications') IS NULL
BEGIN
    THROW 52102, 'Schema [notifications] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

IF OBJECT_ID(N'[notifications].[EmailDelivery]', N'U') IS NULL
BEGIN
    THROW 52104, 'Table [notifications].[EmailDelivery] does not exist. Run notifications 001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[notifications].[EmailDeliveryAttempt]', N'U') IS NULL
BEGIN
    THROW 52105, 'Table [notifications].[EmailDeliveryAttempt] does not exist. Run notifications 001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[notifications].[EmailRateLimitLog]', N'U') IS NULL
BEGIN
    THROW 52106, 'Table [notifications].[EmailRateLimitLog] does not exist. Run notifications 001_tables.sql first.', 1;
END
GO

/* =========================================================
   1) [notifications].[EmailDelivery]
   Worker polling / retry scheduling
   ========================================================= */

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_EmailDelivery_Status_Priority_NextRetryAt_CreatedAt'
      AND [object_id] = OBJECT_ID(N'[notifications].[EmailDelivery]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmailDelivery_Status_Priority_NextRetryAt_CreatedAt]
    ON [notifications].[EmailDelivery]
    (
        [Status] ASC,
        [Priority] ASC,
        [NextRetryAt] ASC,
        [CreatedAt] ASC
    )
    INCLUDE
    (
        [EmailDeliveryId],
        [MessageId],
        [RecipientUserId],
        [ToEmail],
        [TemplateKey],
        [AttemptCount],
        [CorrelationId],
        [LastAttemptAt],
        [Provider]
    );

    PRINT N'Created index: [notifications].[EmailDelivery].[IX_EmailDelivery_Status_Priority_NextRetryAt_CreatedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[EmailDelivery].[IX_EmailDelivery_Status_Priority_NextRetryAt_CreatedAt]';
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_EmailDelivery_RecipientUserId_CreatedAt'
      AND [object_id] = OBJECT_ID(N'[notifications].[EmailDelivery]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmailDelivery_RecipientUserId_CreatedAt]
    ON [notifications].[EmailDelivery]
    (
        [RecipientUserId] ASC,
        [CreatedAt] ASC
    )
    INCLUDE
    (
        [MessageId],
        [ToEmail],
        [TemplateKey],
        [Status],
        [AttemptCount],
        [SentAt],
        [CorrelationId]
    );

    PRINT N'Created index: [notifications].[EmailDelivery].[IX_EmailDelivery_RecipientUserId_CreatedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[EmailDelivery].[IX_EmailDelivery_RecipientUserId_CreatedAt]';
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_EmailDelivery_TemplateKey_CreatedAt'
      AND [object_id] = OBJECT_ID(N'[notifications].[EmailDelivery]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmailDelivery_TemplateKey_CreatedAt]
    ON [notifications].[EmailDelivery]
    (
        [TemplateKey] ASC,
        [CreatedAt] ASC
    )
    INCLUDE
    (
        [MessageId],
        [ToEmail],
        [Status],
        [AttemptCount],
        [SentAt],
        [LastErrorCode],
        [LastErrorClass],
        [Priority]
    );

    PRINT N'Created index: [notifications].[EmailDelivery].[IX_EmailDelivery_TemplateKey_CreatedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[EmailDelivery].[IX_EmailDelivery_TemplateKey_CreatedAt]';
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_EmailDelivery_CorrelationId_CreatedAt'
      AND [object_id] = OBJECT_ID(N'[notifications].[EmailDelivery]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmailDelivery_CorrelationId_CreatedAt]
    ON [notifications].[EmailDelivery]
    (
        [CorrelationId] ASC,
        [CreatedAt] ASC
    )
    INCLUDE
    (
        [MessageId],
        [TemplateKey],
        [Status],
        [AttemptCount],
        [RecipientUserId],
        [ToEmail],
        [Priority]
    );

    PRINT N'Created index: [notifications].[EmailDelivery].[IX_EmailDelivery_CorrelationId_CreatedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[EmailDelivery].[IX_EmailDelivery_CorrelationId_CreatedAt]';
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_EmailDelivery_SentAt'
      AND [object_id] = OBJECT_ID(N'[notifications].[EmailDelivery]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmailDelivery_SentAt]
    ON [notifications].[EmailDelivery] ([SentAt] ASC)
    INCLUDE
    (
        [MessageId],
        [TemplateKey],
        [Status],
        [RecipientUserId],
        [ToEmail],
        [Priority]
    );

    PRINT N'Created index: [notifications].[EmailDelivery].[IX_EmailDelivery_SentAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[EmailDelivery].[IX_EmailDelivery_SentAt]';
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_EmailDelivery_Status_LastAttemptAt'
      AND [object_id] = OBJECT_ID(N'[notifications].[EmailDelivery]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmailDelivery_Status_LastAttemptAt]
    ON [notifications].[EmailDelivery]
    (
        [Status] ASC,
        [LastAttemptAt] DESC
    )
    INCLUDE
    (
        [MessageId],
        [ToEmail],
        [TemplateKey],
        [AttemptCount],
        [LastErrorCode],
        [LastErrorClass],
        [CorrelationId],
        [Priority]
    );

    PRINT N'Created index: [notifications].[EmailDelivery].[IX_EmailDelivery_Status_LastAttemptAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[EmailDelivery].[IX_EmailDelivery_Status_LastAttemptAt]';
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_EmailDelivery_ToEmail_CreatedAt'
      AND [object_id] = OBJECT_ID(N'[notifications].[EmailDelivery]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmailDelivery_ToEmail_CreatedAt]
    ON [notifications].[EmailDelivery]
    (
        [ToEmail] ASC,
        [CreatedAt] ASC
    )
    INCLUDE
    (
        [MessageId],
        [TemplateKey],
        [Status],
        [AttemptCount],
        [CorrelationId]
    );

    PRINT N'Created index: [notifications].[EmailDelivery].[IX_EmailDelivery_ToEmail_CreatedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[EmailDelivery].[IX_EmailDelivery_ToEmail_CreatedAt]';
END
GO

/* =========================================================
   2) [notifications].[EmailDeliveryAttempt]
   Attempt history / troubleshooting
   ========================================================= */

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_EmailDeliveryAttempt_EmailDeliveryId_StartedAt'
      AND [object_id] = OBJECT_ID(N'[notifications].[EmailDeliveryAttempt]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmailDeliveryAttempt_EmailDeliveryId_StartedAt]
    ON [notifications].[EmailDeliveryAttempt]
    (
        [EmailDeliveryId] ASC,
        [StartedAt] ASC
    )
    INCLUDE
    (
        [MessageId],
        [AttemptNumber],
        [Outcome],
        [IsAmbiguous],
        [FinishedAt],
        [ProviderMessageId],
        [ProviderErrorCode],
        [ErrorClass]
    );

    PRINT N'Created index: [notifications].[EmailDeliveryAttempt].[IX_EmailDeliveryAttempt_EmailDeliveryId_StartedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[EmailDeliveryAttempt].[IX_EmailDeliveryAttempt_EmailDeliveryId_StartedAt]';
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_EmailDeliveryAttempt_MessageId_StartedAt'
      AND [object_id] = OBJECT_ID(N'[notifications].[EmailDeliveryAttempt]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmailDeliveryAttempt_MessageId_StartedAt]
    ON [notifications].[EmailDeliveryAttempt]
    (
        [MessageId] ASC,
        [StartedAt] ASC
    )
    INCLUDE
    (
        [EmailDeliveryId],
        [AttemptNumber],
        [Outcome],
        [IsAmbiguous],
        [FinishedAt],
        [ProviderMessageId],
        [ProviderErrorCode],
        [ErrorClass],
        [CorrelationId]
    );

    PRINT N'Created index: [notifications].[EmailDeliveryAttempt].[IX_EmailDeliveryAttempt_MessageId_StartedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[EmailDeliveryAttempt].[IX_EmailDeliveryAttempt_MessageId_StartedAt]';
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_EmailDeliveryAttempt_Outcome_StartedAt'
      AND [object_id] = OBJECT_ID(N'[notifications].[EmailDeliveryAttempt]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmailDeliveryAttempt_Outcome_StartedAt]
    ON [notifications].[EmailDeliveryAttempt]
    (
        [Outcome] ASC,
        [StartedAt] ASC
    )
    INCLUDE
    (
        [MessageId],
        [EmailDeliveryId],
        [AttemptNumber],
        [IsAmbiguous],
        [ErrorClass],
        [ProviderErrorCode]
    );

    PRINT N'Created index: [notifications].[EmailDeliveryAttempt].[IX_EmailDeliveryAttempt_Outcome_StartedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[EmailDeliveryAttempt].[IX_EmailDeliveryAttempt_Outcome_StartedAt]';
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_EmailDeliveryAttempt_IsAmbiguous_StartedAt'
      AND [object_id] = OBJECT_ID(N'[notifications].[EmailDeliveryAttempt]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmailDeliveryAttempt_IsAmbiguous_StartedAt]
    ON [notifications].[EmailDeliveryAttempt]
    (
        [IsAmbiguous] ASC,
        [StartedAt] ASC
    )
    INCLUDE
    (
        [MessageId],
        [EmailDeliveryId],
        [AttemptNumber],
        [Outcome],
        [ErrorClass],
        [ProviderErrorCode]
    );

    PRINT N'Created index: [notifications].[EmailDeliveryAttempt].[IX_EmailDeliveryAttempt_IsAmbiguous_StartedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[EmailDeliveryAttempt].[IX_EmailDeliveryAttempt_IsAmbiguous_StartedAt]';
END
GO

/* =========================================================
   3) [notifications].[EmailRateLimitLog]
   Rate-limit / abuse investigation
   ========================================================= */

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_EmailRateLimitLog_RecipientUserId_OccurredAt'
      AND [object_id] = OBJECT_ID(N'[notifications].[EmailRateLimitLog]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmailRateLimitLog_RecipientUserId_OccurredAt]
    ON [notifications].[EmailRateLimitLog]
    (
        [RecipientUserId] ASC,
        [OccurredAt] ASC
    )
    INCLUDE
    (
        [Endpoint],
        [Allowed],
        [Reason],
        [CorrelationId]
    );

    PRINT N'Created index: [notifications].[EmailRateLimitLog].[IX_EmailRateLimitLog_RecipientUserId_OccurredAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[EmailRateLimitLog].[IX_EmailRateLimitLog_RecipientUserId_OccurredAt]';
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_EmailRateLimitLog_IpAddress_OccurredAt'
      AND [object_id] = OBJECT_ID(N'[notifications].[EmailRateLimitLog]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmailRateLimitLog_IpAddress_OccurredAt]
    ON [notifications].[EmailRateLimitLog]
    (
        [IpAddress] ASC,
        [OccurredAt] ASC
    )
    INCLUDE
    (
        [Endpoint],
        [Allowed],
        [Reason],
        [CorrelationId]
    );

    PRINT N'Created index: [notifications].[EmailRateLimitLog].[IX_EmailRateLimitLog_IpAddress_OccurredAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[EmailRateLimitLog].[IX_EmailRateLimitLog_IpAddress_OccurredAt]';
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_EmailRateLimitLog_Endpoint_OccurredAt'
      AND [object_id] = OBJECT_ID(N'[notifications].[EmailRateLimitLog]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmailRateLimitLog_Endpoint_OccurredAt]
    ON [notifications].[EmailRateLimitLog]
    (
        [Endpoint] ASC,
        [OccurredAt] ASC
    )
    INCLUDE
    (
        [RecipientUserId],
        [Allowed],
        [Reason],
        [CorrelationId]
    );

    PRINT N'Created index: [notifications].[EmailRateLimitLog].[IX_EmailRateLimitLog_Endpoint_OccurredAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[EmailRateLimitLog].[IX_EmailRateLimitLog_Endpoint_OccurredAt]';
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_EmailRateLimitLog_ToEmailHash_OccurredAt'
      AND [object_id] = OBJECT_ID(N'[notifications].[EmailRateLimitLog]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmailRateLimitLog_ToEmailHash_OccurredAt]
    ON [notifications].[EmailRateLimitLog]
    (
        [ToEmailHash] ASC,
        [OccurredAt] ASC
    )
    INCLUDE
    (
        [Endpoint],
        [Allowed],
        [Reason],
        [CorrelationId]
    );

    PRINT N'Created index: [notifications].[EmailRateLimitLog].[IX_EmailRateLimitLog_ToEmailHash_OccurredAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[EmailRateLimitLog].[IX_EmailRateLimitLog_ToEmailHash_OccurredAt]';
END
GO