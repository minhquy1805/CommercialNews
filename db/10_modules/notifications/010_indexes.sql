/*
  File: db/10_modules/notifications/010_indexes.sql
  Module: Notifications
  Purpose:
  - Create non-PK/non-constraint indexes for Notifications tables in CommercialNews V1.
  - Includes:
      * [notifications].[OutboxMessage]
      * [notifications].[EmailDelivery]
      * [notifications].[EmailDeliveryAttempt]
      * [notifications].[EmailRateLimitLog]

  Notes:
  - Idempotent: safe to re-run.
  - PK / UQ / FK / CHECK constraints are defined in 001_tables.sql.
  - Index design focuses on:
      * worker polling / retry scheduling
      * replay / remediation / cleanup
      * admin ops inspection
      * correlation-based troubleshooting
      * privacy-safer recipient lookups
      * auth-critical priority-aware delivery
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

IF OBJECT_ID(N'[notifications].[OutboxMessage]', N'U') IS NULL
BEGIN
    THROW 52103, 'Table [notifications].[OutboxMessage] does not exist. Run notifications 001_tables.sql first.', 1;
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
   1) [notifications].[OutboxMessage]
   ========================================================= */

-- Main outbox publisher pull path:
-- Pending / Failed candidates ordered by priority, retry schedule, and stable time/identity.
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_OutboxMessage_Status_Priority_NextRetryAt_OccurredAt'
      AND [object_id] = OBJECT_ID(N'[notifications].[OutboxMessage]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_OutboxMessage_Status_Priority_NextRetryAt_OccurredAt]
    ON [notifications].[OutboxMessage]
    (
        [Status] ASC,
        [Priority] ASC,
        [NextRetryAt] ASC,
        [OccurredAt] ASC
    )
    INCLUDE
    (
        [OutboxMessageId],
        [MessageId],
        [EventType],
        [AggregateType],
        [AggregateId],
        [AggregateVersion],
        [CorrelationId],
        [AttemptCount],
        [PublishedAt]
    );

    PRINT N'Created index: [notifications].[OutboxMessage].[IX_OutboxMessage_Status_Priority_NextRetryAt_OccurredAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[OutboxMessage].[IX_OutboxMessage_Status_Priority_NextRetryAt_OccurredAt]';
END
GO

-- Per-aggregate troubleshooting / ordering investigation.
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_OutboxMessage_AggregateType_AggregateId_AggregateVersion'
      AND [object_id] = OBJECT_ID(N'[notifications].[OutboxMessage]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_OutboxMessage_AggregateType_AggregateId_AggregateVersion]
    ON [notifications].[OutboxMessage]
    (
        [AggregateType] ASC,
        [AggregateId] ASC,
        [AggregateVersion] ASC
    )
    INCLUDE
    (
        [MessageId],
        [EventType],
        [Status],
        [OccurredAt],
        [PublishedAt],
        [CorrelationId]
    );

    PRINT N'Created index: [notifications].[OutboxMessage].[IX_OutboxMessage_AggregateType_AggregateId_AggregateVersion]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[OutboxMessage].[IX_OutboxMessage_AggregateType_AggregateId_AggregateVersion]';
END
GO

-- Time-range replay / rebuild / audit support.
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_OutboxMessage_OccurredAt'
      AND [object_id] = OBJECT_ID(N'[notifications].[OutboxMessage]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_OutboxMessage_OccurredAt]
    ON [notifications].[OutboxMessage] ([OccurredAt] ASC)
    INCLUDE
    (
        [OutboxMessageId],
        [MessageId],
        [EventType],
        [AggregateType],
        [AggregateId],
        [Status],
        [Priority]
    );

    PRINT N'Created index: [notifications].[OutboxMessage].[IX_OutboxMessage_OccurredAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[OutboxMessage].[IX_OutboxMessage_OccurredAt]';
END
GO

-- Cleanup / retention / publication investigation.
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_OutboxMessage_PublishedAt'
      AND [object_id] = OBJECT_ID(N'[notifications].[OutboxMessage]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_OutboxMessage_PublishedAt]
    ON [notifications].[OutboxMessage] ([PublishedAt] ASC)
    INCLUDE
    (
        [MessageId],
        [Status],
        [OccurredAt],
        [EventType],
        [AttemptCount]
    );

    PRINT N'Created index: [notifications].[OutboxMessage].[IX_OutboxMessage_PublishedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[OutboxMessage].[IX_OutboxMessage_PublishedAt]';
END
GO

-- Poison-message / repeated failure investigation.
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_OutboxMessage_Status_AttemptCount_OccurredAt'
      AND [object_id] = OBJECT_ID(N'[notifications].[OutboxMessage]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_OutboxMessage_Status_AttemptCount_OccurredAt]
    ON [notifications].[OutboxMessage]
    (
        [Status] ASC,
        [AttemptCount] DESC,
        [OccurredAt] ASC
    )
    INCLUDE
    (
        [MessageId],
        [EventType],
        [AggregateType],
        [AggregateId],
        [LastAttemptAt],
        [LastError],
        [LastErrorCode],
        [LastErrorClass]
    );

    PRINT N'Created index: [notifications].[OutboxMessage].[IX_OutboxMessage_Status_AttemptCount_OccurredAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[OutboxMessage].[IX_OutboxMessage_Status_AttemptCount_OccurredAt]';
END
GO

-- Correlation-based end-to-end tracing.
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_OutboxMessage_CorrelationId_OccurredAt'
      AND [object_id] = OBJECT_ID(N'[notifications].[OutboxMessage]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_OutboxMessage_CorrelationId_OccurredAt]
    ON [notifications].[OutboxMessage]
    (
        [CorrelationId] ASC,
        [OccurredAt] ASC
    )
    INCLUDE
    (
        [OutboxMessageId],
        [MessageId],
        [EventType],
        [AggregateType],
        [AggregateId],
        [Status],
        [Priority]
    );

    PRINT N'Created index: [notifications].[OutboxMessage].[IX_OutboxMessage_CorrelationId_OccurredAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[OutboxMessage].[IX_OutboxMessage_CorrelationId_OccurredAt]';
END
GO

/* =========================================================
   2) [notifications].[EmailDelivery]
   ========================================================= */

-- Main delivery queue / retry worker path with priority awareness.
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
        [ToEmailHash],
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

-- Per-recipient-user ops review.
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

-- Privacy-safer recipient lookup.
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_EmailDelivery_ToEmailHash_CreatedAt'
      AND [object_id] = OBJECT_ID(N'[notifications].[EmailDelivery]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmailDelivery_ToEmailHash_CreatedAt]
    ON [notifications].[EmailDelivery]
    (
        [ToEmailHash] ASC,
        [CreatedAt] ASC
    )
    INCLUDE
    (
        [MessageId],
        [TemplateKey],
        [Status],
        [AttemptCount],
        [SentAt],
        [CorrelationId]
    );

    PRINT N'Created index: [notifications].[EmailDelivery].[IX_EmailDelivery_ToEmailHash_CreatedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[EmailDelivery].[IX_EmailDelivery_ToEmailHash_CreatedAt]';
END
GO

-- Template-based reporting / dashboard queries.
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

-- Correlation-based troubleshooting.
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
        [ToEmailHash],
        [Priority]
    );

    PRINT N'Created index: [notifications].[EmailDelivery].[IX_EmailDelivery_CorrelationId_CreatedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[EmailDelivery].[IX_EmailDelivery_CorrelationId_CreatedAt]';
END
GO

-- Delivery cleanup / retention / sent-mail reporting.
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
        [ToEmailHash],
        [Priority]
    );

    PRINT N'Created index: [notifications].[EmailDelivery].[IX_EmailDelivery_SentAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[EmailDelivery].[IX_EmailDelivery_SentAt]';
END
GO

-- Failed / ambiguous / dead investigation.
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

/* =========================================================
   3) [notifications].[EmailDeliveryAttempt]
   ========================================================= */

-- Detail history lookup for a delivery.
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

-- Direct message-level investigation support.
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

-- Outcome-based ops investigation.
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

-- Ambiguity / timeout investigation.
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
   4) [notifications].[EmailRateLimitLog]
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