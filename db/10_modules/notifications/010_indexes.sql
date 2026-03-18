/*
  File: db/10_modules/notifications/010_indexes.sql
  Module: Notifications
  Purpose:
  - Create non-PK/non-constraint indexes for Notifications tables in CommercialNews V1.
  - Includes:
      * [notifications].[OutboxMessage]
      * [notifications].[EmailDelivery]

  Notes:
  - Idempotent: safe to re-run.
  - Unique constraints on [MessageId] are already defined in 001_tables.sql.
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
    THROW 52103, 'Table [notifications].[OutboxMessage] does not exist. Run 001_tables.sql first.', 1;
END
GO

/* =========================================================
   1) Worker polling / retry scheduling
   ========================================================= */

-- Main worker pull path:
-- Pending / Failed messages ordered by retry schedule and occurrence time.
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_OutboxMessage_Status_NextRetryAt_OccurredAt'
      AND [object_id] = OBJECT_ID(N'[notifications].[OutboxMessage]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_OutboxMessage_Status_NextRetryAt_OccurredAt]
    ON [notifications].[OutboxMessage]
    (
        [Status] ASC,
        [NextRetryAt] ASC,
        [OccurredAt] ASC
    )
    INCLUDE
    (
        [MessageId],
        [EventType],
        [AggregateType],
        [AggregateId],
        [AggregateVersion],
        [CorrelationId],
        [AttemptCount]
    );

    PRINT N'Created index: [notifications].[OutboxMessage].[IX_OutboxMessage_Status_NextRetryAt_OccurredAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[OutboxMessage].[IX_OutboxMessage_Status_NextRetryAt_OccurredAt]';
END
GO

/* =========================================================
   2) Per-aggregate ordering / troubleshooting
   ========================================================= */

-- Investigate event ordering for a single aggregate.
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

/* =========================================================
   3) Replay / time-range queries
   ========================================================= */

-- Time-ordered replay / rebuild / audit support
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
        [MessageId],
        [EventType],
        [AggregateType],
        [AggregateId],
        [Status]
    );

    PRINT N'Created index: [notifications].[OutboxMessage].[IX_OutboxMessage_OccurredAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[OutboxMessage].[IX_OutboxMessage_OccurredAt]';
END
GO

/* =========================================================
   4) Published / retention / cleanup support
   ========================================================= */

-- Retention and cleanup for published messages
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
        [EventType]
    );

    PRINT N'Created index: [notifications].[OutboxMessage].[IX_OutboxMessage_PublishedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[OutboxMessage].[IX_OutboxMessage_PublishedAt]';
END
GO

/* =========================================================
   5) Failed / dead-letter investigation
   ========================================================= */

-- Troubleshoot repeated failures / poison messages
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
        [LastError]
    );

    PRINT N'Created index: [notifications].[OutboxMessage].[IX_OutboxMessage_Status_AttemptCount_OccurredAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[OutboxMessage].[IX_OutboxMessage_Status_AttemptCount_OccurredAt]';
END
GO

/* =========================================================
   6) Correlation-based troubleshooting
   ========================================================= */

-- Trace a flow end-to-end by CorrelationId
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
        [MessageId],
        [EventType],
        [AggregateType],
        [AggregateId],
        [Status]
    );

    PRINT N'Created index: [notifications].[OutboxMessage].[IX_OutboxMessage_CorrelationId_OccurredAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[OutboxMessage].[IX_OutboxMessage_CorrelationId_OccurredAt]';
END
GO

/* =========================================================
   EmailDelivery indexes
   ========================================================= */

-- Worker retry / send queue

IF OBJECT_ID(N'[notifications].[EmailDelivery]', N'U') IS NULL
BEGIN
    THROW 52104, 'Table [notifications].[EmailDelivery] does not exist. Run notifications 001_tables.sql first.', 1;
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_EmailDelivery_Status_NextRetryAt_CreatedAt'
      AND [object_id] = OBJECT_ID(N'[notifications].[EmailDelivery]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmailDelivery_Status_NextRetryAt_CreatedAt]
    ON [notifications].[EmailDelivery]
    (
        [Status] ASC,
        [NextRetryAt] ASC,
        [CreatedAt] ASC
    )
    INCLUDE
    (
        [MessageId],
        [ToEmail],
        [TemplateKey],
        [AttemptCount],
        [CorrelationId]
    );

    PRINT N'Created index: [notifications].[EmailDelivery].[IX_EmailDelivery_Status_NextRetryAt_CreatedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[EmailDelivery].[IX_EmailDelivery_Status_NextRetryAt_CreatedAt]';
END
GO

-- Per-user delivery review
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_EmailDelivery_UserId_CreatedAt'
      AND [object_id] = OBJECT_ID(N'[notifications].[EmailDelivery]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmailDelivery_UserId_CreatedAt]
    ON [notifications].[EmailDelivery]
    (
        [UserId] ASC,
        [CreatedAt] ASC
    )
    INCLUDE
    (
        [MessageId],
        [ToEmail],
        [TemplateKey],
        [Status],
        [SentAt]
    );

    PRINT N'Created index: [notifications].[EmailDelivery].[IX_EmailDelivery_UserId_CreatedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[EmailDelivery].[IX_EmailDelivery_UserId_CreatedAt]';
END
GO

-- Troubleshooting by email + time
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
        [SentAt]
    );

    PRINT N'Created index: [notifications].[EmailDelivery].[IX_EmailDelivery_ToEmail_CreatedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[EmailDelivery].[IX_EmailDelivery_ToEmail_CreatedAt]';
END
GO

-- Sent mail retention / cleanup / reporting

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
        [Status],
        [TemplateKey],
        [ToEmail]
    );

    PRINT N'Created index: [notifications].[EmailDelivery].[IX_EmailDelivery_SentAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [notifications].[EmailDelivery].[IX_EmailDelivery_SentAt]';
END
GO