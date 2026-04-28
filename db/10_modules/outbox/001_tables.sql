/*
  File: db/10_modules/outbox/001_tables.sql
  Module: Outbox
  Purpose:
  - Create shared Outbox tables for CommercialNews V1.
  - [outbox].[OutboxMessage] is the shared producer-side publication artifact.

  Design principles:
  - Upstream modules write truth + outbox in the same transaction.
  - Outbox is shared across modules.
  - Outbox.Published means producer-side handoff completed, not downstream side effect completed.
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

IF OBJECT_ID(N'[outbox].[OutboxMessage]', N'U') IS NULL
BEGIN
    CREATE TABLE [outbox].[OutboxMessage]
    (
        [OutboxMessageId]   BIGINT IDENTITY(1,1) NOT NULL,
        [MessageId]         CHAR(26)             NOT NULL,

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
            CHECK ([LastErrorClass] IS NULL OR [LastErrorClass] IN
            (
                'Transient',
                'Permanent',
                'Ambiguous',
                'Policy',
                'Template',
                'Provider',
                'Validation',
                'Unknown'
            )),

        CONSTRAINT [CK_OutboxMessage_OccurredAt]
            CHECK ([OccurredAt] <= DATEADD(DAY, 1, [CreatedAt])),

        CONSTRAINT [CK_OutboxMessage_PublishedAt]
            CHECK ([PublishedAt] IS NULL OR [PublishedAt] >= [OccurredAt]),

        CONSTRAINT [CK_OutboxMessage_LastAttemptAt]
            CHECK ([LastAttemptAt] IS NULL OR [LastAttemptAt] >= [OccurredAt]),

        CONSTRAINT [CK_OutboxMessage_NextRetryAt]
            CHECK ([NextRetryAt] IS NULL OR [NextRetryAt] >= [OccurredAt])
    );

    PRINT N'Created table: [outbox].[OutboxMessage]';
END
ELSE
BEGIN
    PRINT N'Table exists: [outbox].[OutboxMessage]';
END
GO