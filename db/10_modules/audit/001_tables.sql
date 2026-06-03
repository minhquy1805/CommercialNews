/*
  File: db/10_modules/audit/001_tables.sql
  Module: Audit
  Purpose:
  - Create Audit truth tables for CommercialNews V1.
  - [audit].[AuditLog] stores append-only canonical audit evidence.
  - [audit].[AuditIngestion] stores Audit consumer-side processing state.
  - Support async ingestion from Outbox/Broker/Worker.
  - Support investigation-ready querying by time, module, actor, resource, action, risk, severity, correlation, and message identity.
  - Idempotent: safe to re-run.

  Design principles:
  - Audit owns evidence truth, not originating business truth.
  - MessageId is copied from [outbox].[OutboxMessage].[MessageId].
  - MessageId is the canonical idempotency key.
  - PublicId is the Audit-owned API-facing identifier.
  - ActorInternalId may store producer-side InitiatorUserId, but Audit does not own Identity truth.
  - ResourceType/ResourceId are stored as strings; no hard FK to source module resources.
  - AuditLog append-only enforcement via deny UPDATE/DELETE or trigger belongs to policy / later hardening.
  - Index-heavy tuning belongs in 010_indexes.sql.
  - Stored procedures belong in 020_procs.sql.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 56001, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

USE [CommercialNews];
GO

IF SCHEMA_ID(N'audit') IS NULL
BEGIN
    THROW 56002, 'Schema [audit] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

/* =========================================================
   1) [audit].[AuditLog]
   Purpose:
   - Canonical append-only audit evidence.
   - One upstream MessageId should produce at most one AuditLog row.
   ========================================================= */
IF OBJECT_ID(N'[audit].[AuditLog]', N'U') IS NULL
BEGIN
    CREATE TABLE [audit].[AuditLog]
    (
        [AuditLogId]          BIGINT IDENTITY(1,1) NOT NULL,
        [PublicId]            CHAR(26)             NOT NULL,
        [MessageId]           CHAR(26)             NOT NULL,

        [EventType]           NVARCHAR(200)        NOT NULL,
        [EventVersion]        INT                  NULL,
        [SourceModule]        NVARCHAR(100)        NOT NULL,

        [Action]              NVARCHAR(120)        NOT NULL,
        [ActionCategory]      NVARCHAR(100)        NULL,

        [AggregateType]       NVARCHAR(100)        NULL,
        [AggregateId]         NVARCHAR(100)        NULL,
        [AggregatePublicId]   CHAR(26)             NULL,
        [AggregateVersion]    INT                  NULL,

        [ResourceType]        NVARCHAR(100)        NOT NULL,
        [ResourceId]          NVARCHAR(100)        NOT NULL,
        [ResourceDisplayName] NVARCHAR(300)        NULL,

        [ActorInternalId]     BIGINT               NULL,
        [ActorUserId]         CHAR(26)             NULL,
        [ActorEmail]          NVARCHAR(320)        NULL,
        [ActorDisplayName]    NVARCHAR(200)        NULL,

        [ActorType]           VARCHAR(30)          NOT NULL
            CONSTRAINT [DF_AuditLog_ActorType] DEFAULT ('System'),

        [Outcome]             VARCHAR(30)          NOT NULL,
        [Severity]            VARCHAR(30)          NOT NULL,
        [RiskLevel]           VARCHAR(30)          NOT NULL,

        [Summary]             NVARCHAR(500)        NOT NULL,

        [CorrelationId]       NVARCHAR(100)        NULL,
        [CausationId]         NVARCHAR(100)        NULL,
        [TraceId]             NVARCHAR(100)        NULL,

        [IpAddress]           NVARCHAR(45)         NULL,
        [UserAgent]           NVARCHAR(500)        NULL,

        [SourcePriority]      TINYINT              NULL,

        [OccurredAtUtc]       DATETIME2(3)         NOT NULL,
        [IngestedAtUtc]       DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_AuditLog_IngestedAtUtc] DEFAULT (SYSUTCDATETIME()),

        [CreatedAtUtc]        DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_AuditLog_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),

        [MetadataJson]        NVARCHAR(MAX)        NULL,
        [HeadersJson]         NVARCHAR(MAX)        NULL,
        [SanitizedPayloadJson] NVARCHAR(MAX)        NULL,
        [BeforeJson]          NVARCHAR(MAX)        NULL,
        [AfterJson]           NVARCHAR(MAX)        NULL,
        [ChangesJson]         NVARCHAR(MAX)        NULL,

        [Hash]                CHAR(64)             NULL,
        [PrevHash]            CHAR(64)             NULL,

        CONSTRAINT [PK_AuditLog]
            PRIMARY KEY CLUSTERED ([AuditLogId] ASC),

        CONSTRAINT [UQ_AuditLog_PublicId]
            UNIQUE ([PublicId]),

        CONSTRAINT [UQ_AuditLog_MessageId]
            UNIQUE ([MessageId]),

        CONSTRAINT [CK_AuditLog_PublicId_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([PublicId]))) > 0),

        CONSTRAINT [CK_AuditLog_MessageId_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([MessageId]))) > 0),

        CONSTRAINT [CK_AuditLog_EventType_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([EventType]))) > 0),

        CONSTRAINT [CK_AuditLog_SourceModule_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([SourceModule]))) > 0),

        CONSTRAINT [CK_AuditLog_Action_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([Action]))) > 0),

        CONSTRAINT [CK_AuditLog_ResourceType_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([ResourceType]))) > 0),

        CONSTRAINT [CK_AuditLog_ResourceId_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([ResourceId]))) > 0),

        CONSTRAINT [CK_AuditLog_Summary_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([Summary]))) > 0),

        CONSTRAINT [CK_AuditLog_EventVersion]
            CHECK ([EventVersion] IS NULL OR [EventVersion] >= 1),

        CONSTRAINT [CK_AuditLog_AggregateVersion]
            CHECK ([AggregateVersion] IS NULL OR [AggregateVersion] >= 1),

        CONSTRAINT [CK_AuditLog_SourcePriority]
            CHECK ([SourcePriority] IS NULL OR [SourcePriority] BETWEEN 1 AND 9),

        CONSTRAINT [CK_AuditLog_ActorType]
            CHECK ([ActorType] IN
            (
                'User',
                'Admin',
                'Moderator',
                'System',
                'Worker',
                'Anonymous',
                'External'
            )),

        CONSTRAINT [CK_AuditLog_Outcome]
            CHECK ([Outcome] IN
            (
                'Success',
                'Failure',
                'Denied',
                'Ignored'
            )),

        CONSTRAINT [CK_AuditLog_Severity]
            CHECK ([Severity] IN
            (
                'Info',
                'Warning',
                'Error',
                'Critical'
            )),

        CONSTRAINT [CK_AuditLog_RiskLevel]
            CHECK ([RiskLevel] IN
            (
                'Low',
                'Medium',
                'High',
                'Critical'
            )),

        CONSTRAINT [CK_AuditLog_MetadataJson_IsJson]
            CHECK ([MetadataJson] IS NULL OR ISJSON([MetadataJson]) = 1),

        CONSTRAINT [CK_AuditLog_HeadersJson_IsJson]
            CHECK ([HeadersJson] IS NULL OR ISJSON([HeadersJson]) = 1),

        CONSTRAINT [CK_AuditLog_SanitizedPayloadJson_IsJson]
            CHECK ([SanitizedPayloadJson] IS NULL OR ISJSON([SanitizedPayloadJson]) = 1),

        CONSTRAINT [CK_AuditLog_BeforeJson_IsJson]
            CHECK ([BeforeJson] IS NULL OR ISJSON([BeforeJson]) = 1),

        CONSTRAINT [CK_AuditLog_AfterJson_IsJson]
            CHECK ([AfterJson] IS NULL OR ISJSON([AfterJson]) = 1),

        CONSTRAINT [CK_AuditLog_ChangesJson_IsJson]
            CHECK ([ChangesJson] IS NULL OR ISJSON([ChangesJson]) = 1)
    );

    PRINT N'Created table: [audit].[AuditLog]';
END
ELSE
BEGIN
    PRINT N'Table exists: [audit].[AuditLog]';
END
GO

/* =========================================================
   2) [audit].[AuditIngestion]
   Purpose:
   - Audit consumer-side processing state.
   - Tracks whether Audit has seen/processed/failed/deduped a MessageId.
   - Separate from producer-side OutboxMessage.Status.
   ========================================================= */
IF OBJECT_ID(N'[audit].[AuditIngestion]', N'U') IS NULL
BEGIN
    CREATE TABLE [audit].[AuditIngestion]
    (
        [AuditIngestionId]      BIGINT IDENTITY(1,1) NOT NULL,
        [PublicId]              CHAR(26)             NOT NULL,
        [MessageId]             CHAR(26)             NOT NULL,

        [EventType]             NVARCHAR(200)        NOT NULL,

        [AggregateType]         NVARCHAR(100)        NULL,
        [AggregateId]           NVARCHAR(100)        NULL,
        [AggregatePublicId]     CHAR(26)             NULL,
        [AggregateVersion]      INT                  NULL,

        [CorrelationId]         NVARCHAR(100)        NULL,
        [SourcePriority]        TINYINT              NULL,

        [SourceOccurredAtUtc]   DATETIME2(3)         NOT NULL,
        [SourcePublishedAtUtc]  DATETIME2(3)         NULL,

        [ConsumerName]          NVARCHAR(150)        NOT NULL,

        [Status]                VARCHAR(30)          NOT NULL
            CONSTRAINT [DF_AuditIngestion_Status] DEFAULT ('Processing'),

        [AttemptCount]          INT                  NOT NULL
            CONSTRAINT [DF_AuditIngestion_AttemptCount] DEFAULT (0),

        [FirstReceivedAtUtc]    DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_AuditIngestion_FirstReceivedAtUtc] DEFAULT (SYSUTCDATETIME()),

        [LastAttemptAtUtc]      DATETIME2(3)         NULL,
        [ProcessedAtUtc]        DATETIME2(3)         NULL,
        [DeadLetteredAtUtc]     DATETIME2(3)         NULL,

        [LastErrorCode]         NVARCHAR(100)        NULL,
        [LastErrorMessage]      NVARCHAR(2000)       NULL,
        [LastErrorClass]        VARCHAR(30)          NULL,

        [CreatedAtUtc]          DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_AuditIngestion_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),

        [UpdatedAtUtc]          DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_AuditIngestion_UpdatedAtUtc] DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [PK_AuditIngestion]
            PRIMARY KEY CLUSTERED ([AuditIngestionId] ASC),

        CONSTRAINT [UQ_AuditIngestion_PublicId]
            UNIQUE ([PublicId]),

        CONSTRAINT [UQ_AuditIngestion_MessageId]
            UNIQUE ([MessageId]),

        CONSTRAINT [CK_AuditIngestion_PublicId_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([PublicId]))) > 0),

        CONSTRAINT [CK_AuditIngestion_MessageId_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([MessageId]))) > 0),

        CONSTRAINT [CK_AuditIngestion_EventType_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([EventType]))) > 0),

        CONSTRAINT [CK_AuditIngestion_ConsumerName_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([ConsumerName]))) > 0),

        CONSTRAINT [CK_AuditIngestion_AggregateVersion]
            CHECK ([AggregateVersion] IS NULL OR [AggregateVersion] >= 1),

        CONSTRAINT [CK_AuditIngestion_SourcePriority]
            CHECK ([SourcePriority] IS NULL OR [SourcePriority] BETWEEN 1 AND 9),

        CONSTRAINT [CK_AuditIngestion_Status]
            CHECK ([Status] IN
            (
                'Processing',
                'Succeeded',
                'Duplicate',
                'Ignored',
                'Failed',
                'DeadLettered'
            )),

        CONSTRAINT [CK_AuditIngestion_AttemptCount]
            CHECK ([AttemptCount] >= 0),

        CONSTRAINT [CK_AuditIngestion_LastErrorClass]
            CHECK ([LastErrorClass] IS NULL OR [LastErrorClass] IN
            (
                'Transient',
                'Permanent',
                'Ambiguous',
                'Validation',
                'Policy',
                'Redaction',
                'Unknown'
            )),

        CONSTRAINT [CK_AuditIngestion_SourcePublishedAtUtc]
            CHECK (
                [SourcePublishedAtUtc] IS NULL
                OR [SourcePublishedAtUtc] >= [SourceOccurredAtUtc]
            ),

        CONSTRAINT [CK_AuditIngestion_LastAttemptAtUtc]
            CHECK (
                [LastAttemptAtUtc] IS NULL
                OR [LastAttemptAtUtc] >= [FirstReceivedAtUtc]
            ),

        CONSTRAINT [CK_AuditIngestion_ProcessedAtUtc]
            CHECK (
                [ProcessedAtUtc] IS NULL
                OR [ProcessedAtUtc] >= [FirstReceivedAtUtc]
            ),

        CONSTRAINT [CK_AuditIngestion_DeadLetteredAtUtc]
            CHECK (
                [DeadLetteredAtUtc] IS NULL
                OR [DeadLetteredAtUtc] >= [FirstReceivedAtUtc]
            )
    );

    PRINT N'Created table: [audit].[AuditIngestion]';
END
ELSE
BEGIN
    PRINT N'Table exists: [audit].[AuditIngestion]';
END
GO

/* =========================================================
   Notes for later hardening
   =========================================================
   - Enforce AuditLog append-only semantics via:
       * DENY UPDATE, DELETE on [audit].[AuditLog], and/or
       * trigger blocking UPDATE/DELETE
   - [audit].[AuditIngestion] is operational consumer-side state and may be updated by Audit consumer.
   - Investigation indexes belong in 010_indexes.sql.
   - Read/query procedures belong in 020_procs.sql.
   - Never persist secrets, raw tokens, password hashes, session cookies, authorization headers, or unsafe PII.
   - MessageId is the canonical idempotency key copied from [outbox].[OutboxMessage].
   - Do not create hard FK from Audit to source module truth tables in V1.
========================================================= */
