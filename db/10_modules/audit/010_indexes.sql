/*
  File: db/10_modules/audit/010_indexes.sql
  Module: Audit
  Purpose:
  - Create non-PK/non-constraint indexes for Audit tables in CommercialNews V1.
  - Includes:
      * [audit].[AuditLog]
      * [audit].[AuditIngestion]

  Design principles:
  - Idempotent: safe to re-run.
  - Unique constraints on [PublicId] and [MessageId] are already defined in 001_tables.sql.
  - Indexes are optimized for:
      * investigation queries
      * timeline queries
      * message/correlation tracing
      * dashboard summaries
      * consumer-side ingestion diagnostics

  Notes:
  - Avoid indexing JSON fields in V1 unless a specific computed-column strategy is designed.
  - Avoid speculative over-indexing; keep indexes aligned with known API access patterns.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 56101, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

USE [CommercialNews];
GO

IF SCHEMA_ID(N'audit') IS NULL
BEGIN
    THROW 56102, 'Schema [audit] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

IF OBJECT_ID(N'[audit].[AuditLog]', N'U') IS NULL
BEGIN
    THROW 56103, 'Table [audit].[AuditLog] does not exist. Run audit/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[audit].[AuditIngestion]', N'U') IS NULL
BEGIN
    THROW 56104, 'Table [audit].[AuditIngestion] does not exist. Run audit/001_tables.sql first.', 1;
END
GO

/* =========================================================
   1) [audit].[AuditLog] indexes
   ========================================================= */

/*
  General recent audit feed:
  - GET /api/v1/admin/audit/logs
  - dashboard recent events
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditLog_OccurredAtUtc'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditLog]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditLog_OccurredAtUtc]
    ON [audit].[AuditLog]
    (
        [OccurredAtUtc] DESC
    )
    INCLUDE
    (
        [PublicId],
        [MessageId],
        [SourceModule],
        [EventType],
        [Action],
        [ActionCategory],
        [ActorUserId],
        [ActorInternalId],
        [ResourceType],
        [ResourceId],
        [Outcome],
        [Severity],
        [RiskLevel],
        [Summary],
        [CorrelationId],
        [IngestedAtUtc]
    );

    PRINT N'Created index: [audit].[AuditLog].[IX_AuditLog_OccurredAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditLog].[IX_AuditLog_OccurredAtUtc]';
END
GO

/*
  Ingestion chronology:
  - operational views
  - occurred-to-ingest diagnostics
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditLog_IngestedAtUtc'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditLog]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditLog_IngestedAtUtc]
    ON [audit].[AuditLog]
    (
        [IngestedAtUtc] DESC
    )
    INCLUDE
    (
        [PublicId],
        [MessageId],
        [SourceModule],
        [EventType],
        [Action],
        [ResourceType],
        [ResourceId],
        [OccurredAtUtc],
        [Outcome],
        [Severity],
        [RiskLevel],
        [Summary]
    );

    PRINT N'Created index: [audit].[AuditLog].[IX_AuditLog_IngestedAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditLog].[IX_AuditLog_IngestedAtUtc]';
END
GO

/*
  Module-scoped investigation:
  - GET /api/v1/admin/audit/modules/{sourceModule}/logs
  - dashboard by module
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditLog_SourceModule_OccurredAtUtc'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditLog]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditLog_SourceModule_OccurredAtUtc]
    ON [audit].[AuditLog]
    (
        [SourceModule] ASC,
        [OccurredAtUtc] DESC
    )
    INCLUDE
    (
        [PublicId],
        [MessageId],
        [EventType],
        [Action],
        [ActionCategory],
        [ActorUserId],
        [ActorInternalId],
        [ResourceType],
        [ResourceId],
        [Outcome],
        [Severity],
        [RiskLevel],
        [Summary],
        [CorrelationId]
    );

    PRINT N'Created index: [audit].[AuditLog].[IX_AuditLog_SourceModule_OccurredAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditLog].[IX_AuditLog_SourceModule_OccurredAtUtc]';
END
GO

/*
  EventType-scoped investigation:
  - exact source event family lookup
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditLog_EventType_OccurredAtUtc'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditLog]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditLog_EventType_OccurredAtUtc]
    ON [audit].[AuditLog]
    (
        [EventType] ASC,
        [OccurredAtUtc] DESC
    )
    INCLUDE
    (
        [PublicId],
        [MessageId],
        [SourceModule],
        [Action],
        [ActionCategory],
        [ActorUserId],
        [ResourceType],
        [ResourceId],
        [Outcome],
        [Severity],
        [RiskLevel],
        [Summary],
        [CorrelationId]
    );

    PRINT N'Created index: [audit].[AuditLog].[IX_AuditLog_EventType_OccurredAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditLog].[IX_AuditLog_EventType_OccurredAtUtc]';
END
GO

/*
  Action-scoped investigation:
  - filter by normalized action
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditLog_Action_OccurredAtUtc'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditLog]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditLog_Action_OccurredAtUtc]
    ON [audit].[AuditLog]
    (
        [Action] ASC,
        [OccurredAtUtc] DESC
    )
    INCLUDE
    (
        [PublicId],
        [MessageId],
        [SourceModule],
        [EventType],
        [ActionCategory],
        [ActorUserId],
        [ActorInternalId],
        [ResourceType],
        [ResourceId],
        [Outcome],
        [Severity],
        [RiskLevel],
        [Summary],
        [CorrelationId]
    );

    PRINT N'Created index: [audit].[AuditLog].[IX_AuditLog_Action_OccurredAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditLog].[IX_AuditLog_Action_OccurredAtUtc]';
END
GO

/*
  Action category dashboard / filtering:
  - Authorization / IdentitySecurity / ContentLifecycle / Moderation
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditLog_ActionCategory_OccurredAtUtc'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditLog]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditLog_ActionCategory_OccurredAtUtc]
    ON [audit].[AuditLog]
    (
        [ActionCategory] ASC,
        [OccurredAtUtc] DESC
    )
    INCLUDE
    (
        [PublicId],
        [MessageId],
        [SourceModule],
        [EventType],
        [Action],
        [ActorUserId],
        [ResourceType],
        [ResourceId],
        [Outcome],
        [Severity],
        [RiskLevel],
        [Summary]
    );

    PRINT N'Created index: [audit].[AuditLog].[IX_AuditLog_ActionCategory_OccurredAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditLog].[IX_AuditLog_ActionCategory_OccurredAtUtc]';
END
GO

/*
  Actor timeline by stable public actor id.
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditLog_ActorUserId_OccurredAtUtc'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditLog]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditLog_ActorUserId_OccurredAtUtc]
    ON [audit].[AuditLog]
    (
        [ActorUserId] ASC,
        [OccurredAtUtc] DESC
    )
    INCLUDE
    (
        [PublicId],
        [MessageId],
        [SourceModule],
        [EventType],
        [Action],
        [ActionCategory],
        [ResourceType],
        [ResourceId],
        [Outcome],
        [Severity],
        [RiskLevel],
        [Summary],
        [CorrelationId]
    );

    PRINT N'Created index: [audit].[AuditLog].[IX_AuditLog_ActorUserId_OccurredAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditLog].[IX_AuditLog_ActorUserId_OccurredAtUtc]';
END
GO

/*
  Actor timeline by producer-side internal actor id.
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditLog_ActorInternalId_OccurredAtUtc'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditLog]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditLog_ActorInternalId_OccurredAtUtc]
    ON [audit].[AuditLog]
    (
        [ActorInternalId] ASC,
        [OccurredAtUtc] DESC
    )
    INCLUDE
    (
        [PublicId],
        [MessageId],
        [SourceModule],
        [EventType],
        [Action],
        [ActionCategory],
        [ResourceType],
        [ResourceId],
        [Outcome],
        [Severity],
        [RiskLevel],
        [Summary],
        [CorrelationId]
    );

    PRINT N'Created index: [audit].[AuditLog].[IX_AuditLog_ActorInternalId_OccurredAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditLog].[IX_AuditLog_ActorInternalId_OccurredAtUtc]';
END
GO

/*
  Resource timeline:
  - GET /api/v1/admin/audit/resources/{resourceType}/{resourceId}/timeline
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditLog_Resource_OccurredAtUtc'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditLog]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditLog_Resource_OccurredAtUtc]
    ON [audit].[AuditLog]
    (
        [ResourceType] ASC,
        [ResourceId] ASC,
        [OccurredAtUtc] DESC
    )
    INCLUDE
    (
        [PublicId],
        [MessageId],
        [SourceModule],
        [EventType],
        [Action],
        [ActionCategory],
        [ActorUserId],
        [ActorInternalId],
        [Outcome],
        [Severity],
        [RiskLevel],
        [Summary],
        [CorrelationId]
    );

    PRINT N'Created index: [audit].[AuditLog].[IX_AuditLog_Resource_OccurredAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditLog].[IX_AuditLog_Resource_OccurredAtUtc]';
END
GO

/*
  Correlation timeline:
  - GET /api/v1/admin/audit/logs/by-correlation/{correlationId}
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditLog_CorrelationId_OccurredAtUtc'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditLog]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditLog_CorrelationId_OccurredAtUtc]
    ON [audit].[AuditLog]
    (
        [CorrelationId] ASC,
        [OccurredAtUtc] DESC
    )
    INCLUDE
    (
        [PublicId],
        [MessageId],
        [SourceModule],
        [EventType],
        [Action],
        [ActionCategory],
        [ActorUserId],
        [ActorInternalId],
        [ResourceType],
        [ResourceId],
        [Outcome],
        [Severity],
        [RiskLevel],
        [Summary]
    );

    PRINT N'Created index: [audit].[AuditLog].[IX_AuditLog_CorrelationId_OccurredAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditLog].[IX_AuditLog_CorrelationId_OccurredAtUtc]';
END
GO

/*
  Risk-level dashboard / recent high-risk events.
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditLog_RiskLevel_OccurredAtUtc'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditLog]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditLog_RiskLevel_OccurredAtUtc]
    ON [audit].[AuditLog]
    (
        [RiskLevel] ASC,
        [OccurredAtUtc] DESC
    )
    INCLUDE
    (
        [PublicId],
        [MessageId],
        [SourceModule],
        [EventType],
        [Action],
        [ActionCategory],
        [ActorUserId],
        [ResourceType],
        [ResourceId],
        [Outcome],
        [Severity],
        [Summary],
        [CorrelationId]
    );

    PRINT N'Created index: [audit].[AuditLog].[IX_AuditLog_RiskLevel_OccurredAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditLog].[IX_AuditLog_RiskLevel_OccurredAtUtc]';
END
GO

/*
  Severity dashboard / filtering.
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditLog_Severity_OccurredAtUtc'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditLog]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditLog_Severity_OccurredAtUtc]
    ON [audit].[AuditLog]
    (
        [Severity] ASC,
        [OccurredAtUtc] DESC
    )
    INCLUDE
    (
        [PublicId],
        [MessageId],
        [SourceModule],
        [EventType],
        [Action],
        [ActionCategory],
        [ActorUserId],
        [ResourceType],
        [ResourceId],
        [Outcome],
        [RiskLevel],
        [Summary],
        [CorrelationId]
    );

    PRINT N'Created index: [audit].[AuditLog].[IX_AuditLog_Severity_OccurredAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditLog].[IX_AuditLog_Severity_OccurredAtUtc]';
END
GO

/*
  Outcome filtering / dashboard.
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditLog_Outcome_OccurredAtUtc'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditLog]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditLog_Outcome_OccurredAtUtc]
    ON [audit].[AuditLog]
    (
        [Outcome] ASC,
        [OccurredAtUtc] DESC
    )
    INCLUDE
    (
        [PublicId],
        [MessageId],
        [SourceModule],
        [EventType],
        [Action],
        [ActionCategory],
        [ActorUserId],
        [ResourceType],
        [ResourceId],
        [Severity],
        [RiskLevel],
        [Summary],
        [CorrelationId]
    );

    PRINT N'Created index: [audit].[AuditLog].[IX_AuditLog_Outcome_OccurredAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditLog].[IX_AuditLog_Outcome_OccurredAtUtc]';
END
GO

/*
  Aggregate chronology:
  - useful for event sequencing diagnostics
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditLog_Aggregate_Version'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditLog]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditLog_Aggregate_Version]
    ON [audit].[AuditLog]
    (
        [AggregateType] ASC,
        [AggregateId] ASC,
        [AggregateVersion] ASC
    )
    INCLUDE
    (
        [PublicId],
        [MessageId],
        [SourceModule],
        [EventType],
        [Action],
        [ResourceType],
        [ResourceId],
        [OccurredAtUtc],
        [IngestedAtUtc],
        [CorrelationId]
    );

    PRINT N'Created index: [audit].[AuditLog].[IX_AuditLog_Aggregate_Version]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditLog].[IX_AuditLog_Aggregate_Version]';
END
GO

/* =========================================================
   2) [audit].[AuditIngestion] indexes
   ========================================================= */

/*
  Failed/processing/duplicate status dashboard:
  - GET /api/v1/admin/audit/ingestions
  - GET /api/v1/admin/audit/ingestions/failed
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditIngestion_Status_FirstReceivedAtUtc'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditIngestion]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditIngestion_Status_FirstReceivedAtUtc]
    ON [audit].[AuditIngestion]
    (
        [Status] ASC,
        [FirstReceivedAtUtc] DESC
    )
    INCLUDE
    (
        [PublicId],
        [MessageId],
        [EventType],
        [CorrelationId],
        [ConsumerName],
        [AttemptCount],
        [LastAttemptAtUtc],
        [ProcessedAtUtc],
        [LastErrorCode],
        [LastErrorClass]
    );

    PRINT N'Created index: [audit].[AuditIngestion].[IX_AuditIngestion_Status_FirstReceivedAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditIngestion].[IX_AuditIngestion_Status_FirstReceivedAtUtc]';
END
GO

/*
  Retry/failure diagnostics by last attempt time.
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditIngestion_Status_LastAttemptAtUtc'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditIngestion]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditIngestion_Status_LastAttemptAtUtc]
    ON [audit].[AuditIngestion]
    (
        [Status] ASC,
        [LastAttemptAtUtc] DESC
    )
    INCLUDE
    (
        [PublicId],
        [MessageId],
        [EventType],
        [CorrelationId],
        [ConsumerName],
        [AttemptCount],
        [FirstReceivedAtUtc],
        [ProcessedAtUtc],
        [LastErrorCode],
        [LastErrorClass]
    );

    PRINT N'Created index: [audit].[AuditIngestion].[IX_AuditIngestion_Status_LastAttemptAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditIngestion].[IX_AuditIngestion_Status_LastAttemptAtUtc]';
END
GO

/*
  Ingestion diagnostics by event type.
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditIngestion_EventType_FirstReceivedAtUtc'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditIngestion]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditIngestion_EventType_FirstReceivedAtUtc]
    ON [audit].[AuditIngestion]
    (
        [EventType] ASC,
        [FirstReceivedAtUtc] DESC
    )
    INCLUDE
    (
        [PublicId],
        [MessageId],
        [Status],
        [CorrelationId],
        [ConsumerName],
        [AttemptCount],
        [ProcessedAtUtc],
        [LastErrorCode],
        [LastErrorClass]
    );

    PRINT N'Created index: [audit].[AuditIngestion].[IX_AuditIngestion_EventType_FirstReceivedAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditIngestion].[IX_AuditIngestion_EventType_FirstReceivedAtUtc]';
END
GO

/*
  Ingestion diagnostics by correlation id.
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditIngestion_CorrelationId_FirstReceivedAtUtc'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditIngestion]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditIngestion_CorrelationId_FirstReceivedAtUtc]
    ON [audit].[AuditIngestion]
    (
        [CorrelationId] ASC,
        [FirstReceivedAtUtc] DESC
    )
    INCLUDE
    (
        [PublicId],
        [MessageId],
        [EventType],
        [Status],
        [ConsumerName],
        [AttemptCount],
        [ProcessedAtUtc],
        [LastErrorCode],
        [LastErrorClass]
    );

    PRINT N'Created index: [audit].[AuditIngestion].[IX_AuditIngestion_CorrelationId_FirstReceivedAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditIngestion].[IX_AuditIngestion_CorrelationId_FirstReceivedAtUtc]';
END
GO

/*
  Consumer operational health.
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditIngestion_ConsumerName_Status'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditIngestion]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditIngestion_ConsumerName_Status]
    ON [audit].[AuditIngestion]
    (
        [ConsumerName] ASC,
        [Status] ASC
    )
    INCLUDE
    (
        [PublicId],
        [MessageId],
        [EventType],
        [CorrelationId],
        [AttemptCount],
        [FirstReceivedAtUtc],
        [LastAttemptAtUtc],
        [ProcessedAtUtc],
        [LastErrorCode],
        [LastErrorClass]
    );

    PRINT N'Created index: [audit].[AuditIngestion].[IX_AuditIngestion_ConsumerName_Status]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditIngestion].[IX_AuditIngestion_ConsumerName_Status]';
END
GO

/*
  Source event time diagnostics:
  - occurred-to-received / occurred-to-processed lag analysis
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditIngestion_SourceOccurredAtUtc'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditIngestion]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditIngestion_SourceOccurredAtUtc]
    ON [audit].[AuditIngestion]
    (
        [SourceOccurredAtUtc] DESC
    )
    INCLUDE
    (
        [PublicId],
        [MessageId],
        [EventType],
        [Status],
        [CorrelationId],
        [ConsumerName],
        [FirstReceivedAtUtc],
        [ProcessedAtUtc]
    );

    PRINT N'Created index: [audit].[AuditIngestion].[IX_AuditIngestion_SourceOccurredAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditIngestion].[IX_AuditIngestion_SourceOccurredAtUtc]';
END
GO

/*
  Publish-to-ingest lag analysis where SourcePublishedAtUtc is available.
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditIngestion_SourcePublishedAtUtc'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditIngestion]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditIngestion_SourcePublishedAtUtc]
    ON [audit].[AuditIngestion]
    (
        [SourcePublishedAtUtc] DESC
    )
    INCLUDE
    (
        [PublicId],
        [MessageId],
        [EventType],
        [Status],
        [CorrelationId],
        [ConsumerName],
        [FirstReceivedAtUtc],
        [ProcessedAtUtc]
    );

    PRINT N'Created index: [audit].[AuditIngestion].[IX_AuditIngestion_SourcePublishedAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditIngestion].[IX_AuditIngestion_SourcePublishedAtUtc]';
END
GO

/*
  Processed time diagnostics.
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditIngestion_ProcessedAtUtc'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditIngestion]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditIngestion_ProcessedAtUtc]
    ON [audit].[AuditIngestion]
    (
        [ProcessedAtUtc] DESC
    )
    INCLUDE
    (
        [PublicId],
        [MessageId],
        [EventType],
        [Status],
        [CorrelationId],
        [ConsumerName],
        [FirstReceivedAtUtc],
        [SourceOccurredAtUtc],
        [SourcePublishedAtUtc]
    );

    PRINT N'Created index: [audit].[AuditIngestion].[IX_AuditIngestion_ProcessedAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditIngestion].[IX_AuditIngestion_ProcessedAtUtc]';
END
GO