/*
  File: db/10_modules/audit/010_indexes.sql
  Module: Audit
  Purpose:
  - Create non-PK/non-constraint indexes for Audit tables in CommercialNews V1.
  - Includes:
      * [audit].[AuditLog]

  Notes:
  - Idempotent: safe to re-run.
  - Unique constraint on [AuditEventId] is already defined in 001_tables.sql.
  - Indexes are optimized for investigation queries:
      * by time range
      * by actor + time
      * by resource + time
      * by action + time
      * by correlationId
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

/* =========================================================
   AuditLog indexes
   ========================================================= */

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditLog_OccurredAt'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditLog]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditLog_OccurredAt]
    ON [audit].[AuditLog]
    (
        [OccurredAt] DESC
    )
    INCLUDE
    (
        [AuditEventId],
        [ActorUserId],
        [Action],
        [ResourceType],
        [ResourceId],
        [Outcome],
        [Summary],
        [CorrelationId]
    );

    PRINT N'Created index: [audit].[AuditLog].[IX_AuditLog_OccurredAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditLog].[IX_AuditLog_OccurredAt]';
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditLog_ActorUserId_OccurredAt'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditLog]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditLog_ActorUserId_OccurredAt]
    ON [audit].[AuditLog]
    (
        [ActorUserId] ASC,
        [OccurredAt] DESC
    )
    INCLUDE
    (
        [AuditEventId],
        [Action],
        [ResourceType],
        [ResourceId],
        [Outcome],
        [Summary],
        [CorrelationId]
    );

    PRINT N'Created index: [audit].[AuditLog].[IX_AuditLog_ActorUserId_OccurredAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditLog].[IX_AuditLog_ActorUserId_OccurredAt]';
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditLog_Resource_OccurredAt'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditLog]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditLog_Resource_OccurredAt]
    ON [audit].[AuditLog]
    (
        [ResourceType] ASC,
        [ResourceId] ASC,
        [OccurredAt] DESC
    )
    INCLUDE
    (
        [AuditEventId],
        [ActorUserId],
        [Action],
        [Outcome],
        [Summary],
        [CorrelationId]
    );

    PRINT N'Created index: [audit].[AuditLog].[IX_AuditLog_Resource_OccurredAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditLog].[IX_AuditLog_Resource_OccurredAt]';
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditLog_Action_OccurredAt'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditLog]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditLog_Action_OccurredAt]
    ON [audit].[AuditLog]
    (
        [Action] ASC,
        [OccurredAt] DESC
    )
    INCLUDE
    (
        [AuditEventId],
        [ActorUserId],
        [ResourceType],
        [ResourceId],
        [Outcome],
        [Summary],
        [CorrelationId]
    );

    PRINT N'Created index: [audit].[AuditLog].[IX_AuditLog_Action_OccurredAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditLog].[IX_AuditLog_Action_OccurredAt]';
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_AuditLog_CorrelationId'
      AND [object_id] = OBJECT_ID(N'[audit].[AuditLog]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditLog_CorrelationId]
    ON [audit].[AuditLog]
    (
        [CorrelationId] ASC
    )
    INCLUDE
    (
        [OccurredAt],
        [AuditEventId],
        [ActorUserId],
        [Action],
        [ResourceType],
        [ResourceId],
        [Outcome],
        [Summary]
    );

    PRINT N'Created index: [audit].[AuditLog].[IX_AuditLog_CorrelationId]';
END
ELSE
BEGIN
    PRINT N'Index exists: [audit].[AuditLog].[IX_AuditLog_CorrelationId]';
END
GO