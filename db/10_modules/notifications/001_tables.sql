/*
  File: db/10_modules/notifications/001_tables.sql
  Module: Notifications
  Purpose:
  - Create Notifications tables for CommercialNews V1.
  - Refactored toward the current domain shape:
      * [notifications].[OutboxMessage]         -> shared async intent / publication artifact
      * [notifications].[EmailDelivery]         -> canonical delivery workflow truth (new shape)
      * [notifications].[EmailDeliveryAttempt]  -> attempt-level operational history
      * [notifications].[EmailRateLimitLog]     -> optional investigation log

  Design principles:
  - Upstream modules write truth + outbox only; delivery is async.
  - Outbox is shared, but delivery truth belongs to Notifications.
  - Domain shape is the source of truth; tables should follow current domain contracts.
  - Do not store raw secrets/tokens in errors or logs.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 52001, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

USE [CommercialNews];
GO

IF SCHEMA_ID(N'notifications') IS NULL
BEGIN
    THROW 52002, 'Schema [notifications] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

IF SCHEMA_ID(N'identity') IS NULL
BEGIN
    THROW 52003, 'Schema [identity] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

IF OBJECT_ID(N'[identity].[UserAccount]', N'U') IS NULL
BEGIN
    THROW 52004, 'Table [identity].[UserAccount] does not exist. Run identity/001_tables.sql first.', 1;
END
GO

/* =========================================================
   1) [notifications].[OutboxMessage]
   Shared async intent / publication artifact
   ========================================================= */
IF OBJECT_ID(N'[notifications].[OutboxMessage]', N'U') IS NULL
BEGIN
    CREATE TABLE [notifications].[OutboxMessage]
    (
        [OutboxMessageId]   BIGINT IDENTITY(1,1) NOT NULL,
        [MessageId]         CHAR(26)             NOT NULL, -- ULID
        [EventType]         NVARCHAR(200)        NOT NULL,
        [AggregateType]     NVARCHAR(100)        NOT NULL,
        [AggregateId]       NVARCHAR(100)        NOT NULL,
        [AggregatePublicId] CHAR(26)             NULL,
        [AggregateVersion]  INT                  NULL,

        [Payload]           NVARCHAR(MAX)        NOT NULL,
        [Headers]           NVARCHAR(MAX)        NULL,

        [CorrelationId]     NVARCHAR(100)        NULL,
        [InitiatorUserId]   BIGINT               NULL,

        [Priority]          TINYINT              NOT NULL
            CONSTRAINT [DF_OutboxMessage_Priority] DEFAULT (5),

        [Status]            VARCHAR(20)          NOT NULL
            CONSTRAINT [DF_OutboxMessage_Status] DEFAULT ('Pending'),

        [AttemptCount]      INT                  NOT NULL
            CONSTRAINT [DF_OutboxMessage_AttemptCount] DEFAULT (0),

        [NextRetryAt]       DATETIME2(3)         NULL,
        [LastAttemptAt]     DATETIME2(3)         NULL,
        [PublishedAt]       DATETIME2(3)         NULL,

        [LastError]         NVARCHAR(2000)       NULL,
        [LastErrorCode]     NVARCHAR(100)        NULL,
        [LastErrorClass]    VARCHAR(30)          NULL,

        [OccurredAt]        DATETIME2(3)         NOT NULL,
        [CreatedAt]         DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_OutboxMessage_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt]         DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_OutboxMessage_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [PK_OutboxMessage]
            PRIMARY KEY CLUSTERED ([OutboxMessageId] ASC),

        CONSTRAINT [UQ_OutboxMessage_MessageId]
            UNIQUE ([MessageId]),

        CONSTRAINT [CK_OutboxMessage_Status]
            CHECK ([Status] IN ('Pending', 'Publishing', 'Published', 'Failed', 'Dead')),

        CONSTRAINT [CK_OutboxMessage_Priority]
            CHECK ([Priority] BETWEEN 1 AND 9),

        CONSTRAINT [CK_OutboxMessage_AttemptCount]
            CHECK ([AttemptCount] >= 0),

        CONSTRAINT [CK_OutboxMessage_AggregateVersion]
            CHECK ([AggregateVersion] IS NULL OR [AggregateVersion] >= 1),

        CONSTRAINT [CK_OutboxMessage_LastErrorClass]
            CHECK ([LastErrorClass] IS NULL OR [LastErrorClass] IN ('Transient', 'Permanent', 'Ambiguous', 'Policy', 'Template', 'Provider', 'Validation')),

        CONSTRAINT [CK_OutboxMessage_OccurredAt]
            CHECK ([OccurredAt] <= DATEADD(DAY, 1, [CreatedAt]))
    );

    PRINT N'Created table: [notifications].[OutboxMessage]';
END
ELSE
BEGIN
    PRINT N'Table exists: [notifications].[OutboxMessage]';
END
GO

/* =========================================================
   2) [notifications].[EmailDelivery]
   Canonical delivery workflow truth (new shape)
   ========================================================= */
IF OBJECT_ID(N'[notifications].[EmailDelivery]', N'U') IS NULL
BEGIN
    CREATE TABLE [notifications].[EmailDelivery]
    (
        [EmailDeliveryId]   BIGINT IDENTITY(1,1) NOT NULL,
        [MessageId]         CHAR(26)             NOT NULL, -- links to OutboxMessage.MessageId

        [BusinessDedupeKey] NVARCHAR(300)        NOT NULL,

        [RecipientUserId]   BIGINT               NULL,
        [ToEmail]           NVARCHAR(320)        NOT NULL,

        [TemplateKey]       NVARCHAR(100)        NOT NULL,
        [VariablesJson]     NVARCHAR(MAX)        NOT NULL,

        [Provider]          VARCHAR(30)          NOT NULL
            CONSTRAINT [DF_EmailDelivery_Provider] DEFAULT ('smtp'),

        [Priority]          TINYINT              NOT NULL
            CONSTRAINT [DF_EmailDelivery_Priority] DEFAULT (5),

        [Status]            VARCHAR(20)          NOT NULL
            CONSTRAINT [DF_EmailDelivery_Status] DEFAULT ('Queued'),

        [AttemptCount]      INT                  NOT NULL
            CONSTRAINT [DF_EmailDelivery_AttemptCount] DEFAULT (0),

        [LastAttemptAt]     DATETIME2(3)         NULL,
        [NextRetryAt]       DATETIME2(3)         NULL,
        [SentAt]            DATETIME2(3)         NULL,

        [LastErrorCode]     NVARCHAR(100)        NULL,
        [LastErrorClass]    VARCHAR(30)          NULL,

        [CorrelationId]     NVARCHAR(100)        NULL,

        [CreatedAt]         DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_EmailDelivery_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt]         DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_EmailDelivery_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [PK_EmailDelivery]
            PRIMARY KEY CLUSTERED ([EmailDeliveryId] ASC),

        CONSTRAINT [UQ_EmailDelivery_MessageId]
            UNIQUE ([MessageId]),

        CONSTRAINT [UQ_EmailDelivery_BusinessDedupeKey]
            UNIQUE ([BusinessDedupeKey]),

        CONSTRAINT [FK_EmailDelivery_OutboxMessage_MessageId]
            FOREIGN KEY ([MessageId])
            REFERENCES [notifications].[OutboxMessage]([MessageId]),

        CONSTRAINT [FK_EmailDelivery_UserAccount]
            FOREIGN KEY ([RecipientUserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [CK_EmailDelivery_Status]
            CHECK ([Status] IN ('Queued', 'Sending', 'Sent', 'Failed', 'Dead', 'Suppressed', 'Ambiguous')),

        CONSTRAINT [CK_EmailDelivery_Priority]
            CHECK ([Priority] BETWEEN 1 AND 9),

        CONSTRAINT [CK_EmailDelivery_AttemptCount]
            CHECK ([AttemptCount] >= 0),

        CONSTRAINT [CK_EmailDelivery_LastErrorClass]
            CHECK ([LastErrorClass] IS NULL OR [LastErrorClass] IN ('Transient', 'Permanent', 'Ambiguous', 'Policy', 'Template', 'Provider', 'Validation')),

        CONSTRAINT [CK_EmailDelivery_VariablesJson_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([VariablesJson]))) > 0),

        CONSTRAINT [CK_EmailDelivery_SentAt]
            CHECK ([SentAt] IS NULL OR [SentAt] >= [CreatedAt]),

        CONSTRAINT [CK_EmailDelivery_LastAttemptAt]
            CHECK ([LastAttemptAt] IS NULL OR [LastAttemptAt] >= [CreatedAt]),

        CONSTRAINT [CK_EmailDelivery_NextRetryAt]
            CHECK ([NextRetryAt] IS NULL OR [NextRetryAt] >= [CreatedAt])
    );

    PRINT N'Created table: [notifications].[EmailDelivery]';
END
ELSE
BEGIN
    PRINT N'Table exists: [notifications].[EmailDelivery]';
END
GO

/* =========================================================
   3) [notifications].[EmailDeliveryAttempt]
   Attempt-level operational history
   ========================================================= */
IF OBJECT_ID(N'[notifications].[EmailDeliveryAttempt]', N'U') IS NULL
BEGIN
    CREATE TABLE [notifications].[EmailDeliveryAttempt]
    (
        [EmailDeliveryAttemptId] BIGINT IDENTITY(1,1) NOT NULL,
        [EmailDeliveryId]        BIGINT               NOT NULL,
        [MessageId]              CHAR(26)             NOT NULL,

        [AttemptNumber]          INT                  NOT NULL,

        [StartedAt]              DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_EmailDeliveryAttempt_StartedAt] DEFAULT (SYSUTCDATETIME()),

        [FinishedAt]             DATETIME2(3)         NULL,

        [Outcome]                VARCHAR(30)          NOT NULL,
        [IsAmbiguous]            BIT                  NOT NULL
            CONSTRAINT [DF_EmailDeliveryAttempt_IsAmbiguous] DEFAULT (0),

        [ProviderMessageId]      NVARCHAR(200)        NULL,
        [ProviderErrorCode]      NVARCHAR(100)        NULL,
        [ErrorClass]             VARCHAR(30)          NULL,
        [ErrorDetail]            NVARCHAR(2000)       NULL,

        [CorrelationId]          NVARCHAR(100)        NULL,

        [CreatedAt]              DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_EmailDeliveryAttempt_CreatedAt] DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [PK_EmailDeliveryAttempt]
            PRIMARY KEY CLUSTERED ([EmailDeliveryAttemptId] ASC),

        CONSTRAINT [UQ_EmailDeliveryAttempt_EmailDeliveryId_AttemptNumber]
            UNIQUE ([EmailDeliveryId], [AttemptNumber]),

        CONSTRAINT [FK_EmailDeliveryAttempt_EmailDelivery]
            FOREIGN KEY ([EmailDeliveryId])
            REFERENCES [notifications].[EmailDelivery]([EmailDeliveryId]),

        CONSTRAINT [FK_EmailDeliveryAttempt_OutboxMessage_MessageId]
            FOREIGN KEY ([MessageId])
            REFERENCES [notifications].[OutboxMessage]([MessageId]),

        CONSTRAINT [CK_EmailDeliveryAttempt_AttemptNumber]
            CHECK ([AttemptNumber] >= 1),

        CONSTRAINT [CK_EmailDeliveryAttempt_Outcome]
            CHECK ([Outcome] IN ('Sent', 'Failed', 'Timeout', 'Suppressed', 'Skipped', 'ProviderRejected')),

        CONSTRAINT [CK_EmailDeliveryAttempt_ErrorClass]
            CHECK ([ErrorClass] IS NULL OR [ErrorClass] IN ('Transient', 'Permanent', 'Ambiguous', 'Policy', 'Template', 'Provider', 'Validation')),

        CONSTRAINT [CK_EmailDeliveryAttempt_FinishedAt]
            CHECK ([FinishedAt] IS NULL OR [FinishedAt] >= [StartedAt])
    );

    PRINT N'Created table: [notifications].[EmailDeliveryAttempt]';
END
ELSE
BEGIN
    PRINT N'Table exists: [notifications].[EmailDeliveryAttempt]';
END
GO

/* =========================================================
   4) [notifications].[EmailRateLimitLog]
   Optional investigation log
   ========================================================= */
IF OBJECT_ID(N'[notifications].[EmailRateLimitLog]', N'U') IS NULL
BEGIN
    CREATE TABLE [notifications].[EmailRateLimitLog]
    (
        [EmailRateLimitLogId] BIGINT IDENTITY(1,1) NOT NULL,
        [RecipientUserId]     BIGINT               NULL,

        [Endpoint]            NVARCHAR(60)         NOT NULL,
        [ToEmail]             NVARCHAR(320)        NULL,
        [ToEmailHash]         VARCHAR(64)          NULL,
        [IpAddress]           NVARCHAR(45)         NULL,

        [Allowed]             BIT                  NOT NULL
            CONSTRAINT [DF_EmailRateLimitLog_Allowed] DEFAULT (1),

        [Reason]              NVARCHAR(120)        NULL,
        [DecisionKey]         NVARCHAR(150)        NULL,

        [CorrelationId]       NVARCHAR(100)        NULL,
        [OccurredAt]          DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_EmailRateLimitLog_OccurredAt] DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [PK_EmailRateLimitLog]
            PRIMARY KEY CLUSTERED ([EmailRateLimitLogId] ASC),

        CONSTRAINT [FK_EmailRateLimitLog_UserAccount]
            FOREIGN KEY ([RecipientUserId])
            REFERENCES [identity].[UserAccount]([UserId])
    );

    PRINT N'Created table: [notifications].[EmailRateLimitLog]';
END
ELSE
BEGIN
    PRINT N'Table exists: [notifications].[EmailRateLimitLog]';
END
GO