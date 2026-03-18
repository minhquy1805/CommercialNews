/*
  File: db/10_modules/notifications/001_tables.sql
  Module: Notifications
  Purpose:
  - Create Notifications tables for CommercialNews V1.
  - Includes:
      * [notifications].[OutboxMessage]  -> shared durable outbox for async side effects
      * [notifications].[EmailDelivery]  -> email delivery-state truth

  Notes:
  - Message payload is stored as JSON text in NVARCHAR(MAX).
  - Broker is transport only; SQL remains durable truth.
  - EmailDelivery complements OutboxMessage and does not replace it.
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

        [Status]            VARCHAR(20)          NOT NULL
            CONSTRAINT [DF_OutboxMessage_Status] DEFAULT ('Pending'),

        [AttemptCount]      INT                  NOT NULL
            CONSTRAINT [DF_OutboxMessage_AttemptCount] DEFAULT (0),

        [NextRetryAt]       DATETIME2(3)         NULL,
        [LastAttemptAt]     DATETIME2(3)         NULL,
        [PublishedAt]       DATETIME2(3)         NULL,

        [LastError]         NVARCHAR(2000)       NULL,

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
            CHECK ([Status] IN ('Pending', 'Processing', 'Published', 'Failed', 'DeadLetter')),

        CONSTRAINT [CK_OutboxMessage_AttemptCount]
            CHECK ([AttemptCount] >= 0),

        CONSTRAINT [CK_OutboxMessage_AggregateVersion]
            CHECK ([AggregateVersion] IS NULL OR [AggregateVersion] >= 1),

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
   ========================================================= */
IF OBJECT_ID(N'[notifications].[EmailDelivery]', N'U') IS NULL
BEGIN
    CREATE TABLE [notifications].[EmailDelivery]
    (
        [EmailDeliveryId]     BIGINT IDENTITY(1,1) NOT NULL,
        [MessageId]           CHAR(26)             NOT NULL, -- links to OutboxMessage.MessageId

        [UserId]              BIGINT               NULL,
        [ToEmail]             NVARCHAR(320)        NOT NULL,
        [TemplateKey]         NVARCHAR(100)        NOT NULL,
        [Subject]             NVARCHAR(300)        NULL,

        [Status]              VARCHAR(20)          NOT NULL
            CONSTRAINT [DF_EmailDelivery_Status] DEFAULT ('Pending'),

        [AttemptCount]        INT                  NOT NULL
            CONSTRAINT [DF_EmailDelivery_AttemptCount] DEFAULT (0),

        [ProviderMessageId]   NVARCHAR(200)        NULL,
        [LastAttemptAt]       DATETIME2(3)         NULL,
        [SentAt]              DATETIME2(3)         NULL,
        [NextRetryAt]         DATETIME2(3)         NULL,

        [LastError]           NVARCHAR(2000)       NULL,
        [CorrelationId]       NVARCHAR(100)        NULL,

        [CreatedAt]           DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_EmailDelivery_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt]           DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_EmailDelivery_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [PK_EmailDelivery]
            PRIMARY KEY CLUSTERED ([EmailDeliveryId] ASC),

        CONSTRAINT [UQ_EmailDelivery_MessageId]
            UNIQUE ([MessageId]),

        CONSTRAINT [FK_EmailDelivery_OutboxMessage_MessageId]
            FOREIGN KEY ([MessageId])
            REFERENCES [notifications].[OutboxMessage]([MessageId]),

        CONSTRAINT [FK_EmailDelivery_UserAccount]
            FOREIGN KEY ([UserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [CK_EmailDelivery_Status]
            CHECK ([Status] IN ('Pending', 'Processing', 'Sent', 'Failed', 'DeadLetter', 'Cancelled')),

        CONSTRAINT [CK_EmailDelivery_AttemptCount]
            CHECK ([AttemptCount] >= 0),

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