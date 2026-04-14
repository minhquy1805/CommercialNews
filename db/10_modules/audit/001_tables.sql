/*
  File: db/10_modules/audit/001_tables.sql
  Module: Audit
  Purpose:
  - Create Audit truth tables for CommercialNews V1.
  - Append-only canonical audit evidence store.
  - Support async ingestion from Outbox/Broker/Worker.
  - Support investigation-ready querying by time, actor, resource, action, correlation.
  - Idempotent: safe to re-run.

  Notes:
  - Audit owns evidence truth, not originating business truth.
  - AuditEventId is the canonical idempotency key and follows the system outbox message identity format.
  - ActorUserId is nullable for system actions.
  - ResourceType/ResourceId are stored as strings (no hard FK to business resources).
  - Append-only enforcement via deny UPDATE/DELETE or trigger belongs to policy / later hardening.
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

IF SCHEMA_ID(N'identity') IS NULL
BEGIN
    THROW 56003, 'Schema [identity] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

IF OBJECT_ID(N'[identity].[UserAccount]', N'U') IS NULL
BEGIN
    THROW 56004, 'Table [identity].[UserAccount] does not exist. Run identity/001_tables.sql first.', 1;
END
GO

/* =========================================================
   1) [audit].[AuditLog]
   ========================================================= */
IF OBJECT_ID(N'[audit].[AuditLog]', N'U') IS NULL
BEGIN
    CREATE TABLE [audit].[AuditLog]
    (
        [AuditId]             BIGINT IDENTITY(1,1) NOT NULL,
        [AuditEventId]        CHAR(26)             NOT NULL,

        [ActorUserId]         BIGINT               NULL,

        [Action]              NVARCHAR(120)        NOT NULL,
        [ResourceType]        NVARCHAR(60)         NOT NULL,
        [ResourceId]          NVARCHAR(100)        NOT NULL,

        [Outcome]             NVARCHAR(30)         NULL, -- e.g. Success / Failure
        [Summary]             NVARCHAR(300)        NOT NULL,
        [Reason]              NVARCHAR(500)        NULL,

        [OccurredAt]          DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_AuditLog_OccurredAt] DEFAULT (SYSUTCDATETIME()),
        [CorrelationId]       NVARCHAR(100)        NULL,

        [IpAddress]           NVARCHAR(45)         NULL,
        [UserAgent]           NVARCHAR(300)        NULL,

        [OldValuesJson]       NVARCHAR(MAX)        NULL,
        [NewValuesJson]       NVARCHAR(MAX)        NULL,
        [MetadataJson]        NVARCHAR(MAX)        NULL,

        CONSTRAINT [PK_AuditLog]
            PRIMARY KEY CLUSTERED ([AuditId] ASC),

        CONSTRAINT [UQ_AuditLog_AuditEventId]
            UNIQUE ([AuditEventId]),

        CONSTRAINT [FK_AuditLog_ActorUser]
            FOREIGN KEY ([ActorUserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [CK_AuditLog_AuditEventId_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([AuditEventId]))) > 0),

        CONSTRAINT [CK_AuditLog_Action_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([Action]))) > 0),

        CONSTRAINT [CK_AuditLog_ResourceType_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([ResourceType]))) > 0),

        CONSTRAINT [CK_AuditLog_ResourceId_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([ResourceId]))) > 0),

        CONSTRAINT [CK_AuditLog_Summary_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([Summary]))) > 0),

        CONSTRAINT [CK_AuditLog_Outcome_Allowed]
            CHECK (
                [Outcome] IS NULL
                OR [Outcome] IN (N'Success', N'Failure')
            )
    );

    PRINT N'Created table: [audit].[AuditLog]';
END
ELSE
BEGIN
    PRINT N'Table exists: [audit].[AuditLog]';
END
GO

/* =========================================================
   Notes for later hardening
   =========================================================
   - Enforce append-only semantics via:
       * DENY UPDATE, DELETE on [audit].[AuditLog], and/or
       * trigger blocking UPDATE/DELETE
   - Investigation indexes belong in 010_indexes.sql
   - Read/query procedures belong in 020_procs.sql
   - Never persist secrets, raw tokens, password hashes, or unsafe PII
========================================================= */