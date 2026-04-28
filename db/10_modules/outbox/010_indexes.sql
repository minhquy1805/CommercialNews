/*
  File: db/10_modules/outbox/010_indexes.sql
  Module: Outbox
  Purpose:
  - Create indexes for shared producer-side outbox.
  - Support worker claiming, aggregate investigation, retention cleanup,
    retry/dead inspection, and correlation lookup.
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

IF OBJECT_ID(N'[outbox].[OutboxMessage]', N'U') IS NULL
BEGIN
    THROW 52103, 'Table [outbox].[OutboxMessage] does not exist. Run outbox/001_tables.sql first.', 1;
END
GO

/* =========================================================
   1) Worker claim / retry scheduling
   ========================================================= */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_OutboxMessage_Status_Priority_NextRetryAt_OccurredAt'
      AND [object_id] = OBJECT_ID(N'[outbox].[OutboxMessage]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_OutboxMessage_Status_Priority_NextRetryAt_OccurredAt]
    ON [outbox].[OutboxMessage]
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

    PRINT N'Created index: [outbox].[OutboxMessage].[IX_OutboxMessage_Status_Priority_NextRetryAt_OccurredAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [outbox].[OutboxMessage].[IX_OutboxMessage_Status_Priority_NextRetryAt_OccurredAt]';
END
GO

/* =========================================================
   2) Aggregate investigation / ordering by aggregate
   ========================================================= */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_OutboxMessage_AggregateType_AggregateId_AggregateVersion'
      AND [object_id] = OBJECT_ID(N'[outbox].[OutboxMessage]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_OutboxMessage_AggregateType_AggregateId_AggregateVersion]
    ON [outbox].[OutboxMessage]
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

    PRINT N'Created index: [outbox].[OutboxMessage].[IX_OutboxMessage_AggregateType_AggregateId_AggregateVersion]';
END
ELSE
BEGIN
    PRINT N'Index exists: [outbox].[OutboxMessage].[IX_OutboxMessage_AggregateType_AggregateId_AggregateVersion]';
END
GO

/* =========================================================
   3) OccurredAt timeline / replay / investigation
   ========================================================= */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_OutboxMessage_OccurredAt'
      AND [object_id] = OBJECT_ID(N'[outbox].[OutboxMessage]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_OutboxMessage_OccurredAt]
    ON [outbox].[OutboxMessage] ([OccurredAt] ASC)
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

    PRINT N'Created index: [outbox].[OutboxMessage].[IX_OutboxMessage_OccurredAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [outbox].[OutboxMessage].[IX_OutboxMessage_OccurredAt]';
END
GO

/* =========================================================
   4) PublishedAt retention / cleanup
   ========================================================= */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_OutboxMessage_PublishedAt'
      AND [object_id] = OBJECT_ID(N'[outbox].[OutboxMessage]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_OutboxMessage_PublishedAt]
    ON [outbox].[OutboxMessage] ([PublishedAt] ASC)
    INCLUDE
    (
        [MessageId],
        [Status],
        [OccurredAt],
        [EventType],
        [AttemptCount]
    );

    PRINT N'Created index: [outbox].[OutboxMessage].[IX_OutboxMessage_PublishedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [outbox].[OutboxMessage].[IX_OutboxMessage_PublishedAt]';
END
GO

/* =========================================================
   5) Failed/dead inspection by status and attempt count
   ========================================================= */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_OutboxMessage_Status_AttemptCount_OccurredAt'
      AND [object_id] = OBJECT_ID(N'[outbox].[OutboxMessage]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_OutboxMessage_Status_AttemptCount_OccurredAt]
    ON [outbox].[OutboxMessage]
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

    PRINT N'Created index: [outbox].[OutboxMessage].[IX_OutboxMessage_Status_AttemptCount_OccurredAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [outbox].[OutboxMessage].[IX_OutboxMessage_Status_AttemptCount_OccurredAt]';
END
GO

/* =========================================================
   6) Correlation investigation
   ========================================================= */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_OutboxMessage_CorrelationId_OccurredAt'
      AND [object_id] = OBJECT_ID(N'[outbox].[OutboxMessage]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_OutboxMessage_CorrelationId_OccurredAt]
    ON [outbox].[OutboxMessage]
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

    PRINT N'Created index: [outbox].[OutboxMessage].[IX_OutboxMessage_CorrelationId_OccurredAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [outbox].[OutboxMessage].[IX_OutboxMessage_CorrelationId_OccurredAt]';
END
GO