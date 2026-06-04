/*
  File: db/10_modules/audit/020_procs.sql
  Module: Audit
  Purpose:
  - Create stored procedures for Audit V1.
  - Append-only canonical audit evidence operations:
      * AuditLog idempotent insert by MessageId
      * AuditLog reads and investigation queries
      * AuditLog paging and record counts
  - Consumer-side ingestion tracking operations:
      * AuditIngestion upsert / status marking
      * AuditIngestion reads and operational queries
      * AuditIngestion paging and record counts
  - Idempotent: uses CREATE OR ALTER PROCEDURE

  Design principles:
  - AuditLog is append-only: no UPDATE/DELETE procedures in V1.
  - AuditIngestion is operational consumer-side state and may be updated by Audit consumer.
  - MessageId is copied from [outbox].[OutboxMessage].[MessageId].
  - MessageId is the canonical idempotency key.
  - PublicId is the API-facing Audit-owned identifier.
  - Investigation queries are bounded and ordered newest-first by default.
  - AuditIngestion.Status is consumer-side state; do not confuse it with OutboxMessage.Status.
*/

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 56201, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

USE [CommercialNews];
GO

IF SCHEMA_ID(N'audit') IS NULL
BEGIN
    THROW 56202, 'Schema [audit] does not exist. Run bootstrap scripts first.', 1;
END
GO

IF OBJECT_ID(N'[audit].[AuditLog]', N'U') IS NULL
BEGIN
    THROW 56203, 'Table [audit].[AuditLog] does not exist. Run audit/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[audit].[AuditIngestion]', N'U') IS NULL
BEGIN
    THROW 56204, 'Table [audit].[AuditIngestion] does not exist. Run audit/001_tables.sql first.', 1;
END
GO

/* =========================================================
   Shared validation notes
   =========================================================
   Error code range:
   - 56310-56349: AuditLog validation/errors
   - 56350-56389: AuditIngestion validation/errors
========================================================= */

 /* =========================================================
    1) AuditLog — insert
    ========================================================= */

CREATE OR ALTER PROCEDURE [audit].[AuditLog_Insert]
    @PublicId              CHAR(26),
    @MessageId             CHAR(26),

    @EventType             NVARCHAR(200),
    @EventVersion          INT = NULL,
    @SourceModule          NVARCHAR(100),

    @Action                NVARCHAR(120),
    @ActionCategory        NVARCHAR(100) = NULL,

    @AggregateType         NVARCHAR(100) = NULL,
    @AggregateId           NVARCHAR(100) = NULL,
    @AggregatePublicId     CHAR(26) = NULL,
    @AggregateVersion      INT = NULL,

    @ResourceType          NVARCHAR(100),
    @ResourceId            NVARCHAR(100),
    @ResourceDisplayName   NVARCHAR(300) = NULL,

    @ActorInternalId       BIGINT = NULL,
    @ActorUserId           CHAR(26) = NULL,
    @ActorEmail            NVARCHAR(320) = NULL,
    @ActorDisplayName      NVARCHAR(200) = NULL,
    @ActorType             VARCHAR(30) = 'System',

    @Outcome               VARCHAR(30),
    @Severity              VARCHAR(30),
    @RiskLevel             VARCHAR(30),

    @Summary               NVARCHAR(500),
    @Reason                NVARCHAR(500) = NULL,

    @CorrelationId         NVARCHAR(100) = NULL,
    @CausationId           NVARCHAR(100) = NULL,
    @TraceId               NVARCHAR(100) = NULL,

    @IpAddress             NVARCHAR(45) = NULL,
    @UserAgent             NVARCHAR(500) = NULL,

    @SourcePriority        TINYINT = NULL,

    @OccurredAtUtc         DATETIME2(3),

    @MetadataJson          NVARCHAR(MAX) = NULL,
    @HeadersJson           NVARCHAR(MAX) = NULL,
    @SanitizedPayloadJson   NVARCHAR(MAX) = NULL,
    @BeforeJson            NVARCHAR(MAX) = NULL,
    @AfterJson             NVARCHAR(MAX) = NULL,
    @ChangesJson           NVARCHAR(MAX) = NULL,

    @Hash                  CHAR(64) = NULL,
    @PrevHash              CHAR(64) = NULL,

    @IngestedAtUtc         DATETIME2(3) = NULL,
    @CreatedAtUtc          DATETIME2(3) = NULL,

    @AuditLogId            BIGINT OUTPUT,
    @WasInserted           BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    SET @PublicId = LTRIM(RTRIM(@PublicId));
    SET @MessageId = LTRIM(RTRIM(@MessageId));
    SET @IngestedAtUtc = ISNULL(@IngestedAtUtc, @NowUtc);
    SET @CreatedAtUtc = ISNULL(@CreatedAtUtc, @IngestedAtUtc);
    SET @Reason = NULLIF(LTRIM(RTRIM(@Reason)), N'');

    IF NULLIF(@PublicId, '') IS NULL
        THROW 56310, 'Audit public id is required.', 1;

    IF LEN(@PublicId) <> 26
        THROW 56311, 'Audit public id must be 26 characters.', 1;

    IF NULLIF(@MessageId, '') IS NULL
        THROW 56312, 'Audit message id is required.', 1;

    IF LEN(@MessageId) <> 26
        THROW 56313, 'Audit message id must be 26 characters.', 1;

    IF NULLIF(LTRIM(RTRIM(@EventType)), N'') IS NULL
        THROW 56314, 'Audit event type is required.', 1;

    IF NULLIF(LTRIM(RTRIM(@SourceModule)), N'') IS NULL
        THROW 56315, 'Audit source module is required.', 1;

    IF NULLIF(LTRIM(RTRIM(@Action)), N'') IS NULL
        THROW 56316, 'Audit action is required.', 1;

    IF NULLIF(LTRIM(RTRIM(@ResourceType)), N'') IS NULL
        THROW 56317, 'Audit resource type is required.', 1;

    IF NULLIF(LTRIM(RTRIM(@ResourceId)), N'') IS NULL
        THROW 56318, 'Audit resource id is required.', 1;

    IF NULLIF(LTRIM(RTRIM(@Summary)), N'') IS NULL
        THROW 56319, 'Audit summary is required.', 1;

    IF @OccurredAtUtc IS NULL
        THROW 56320, 'Audit occurredAtUtc is required.', 1;

    IF @EventVersion IS NOT NULL AND @EventVersion < 1
        THROW 56321, 'Audit event version must be >= 1.', 1;

    IF @AggregateVersion IS NOT NULL AND @AggregateVersion < 1
        THROW 56322, 'Audit aggregate version must be >= 1.', 1;

    IF @SourcePriority IS NOT NULL AND (@SourcePriority < 1 OR @SourcePriority > 9)
        THROW 56323, 'Audit source priority must be between 1 and 9.', 1;

    IF @ActorType NOT IN ('User', 'Admin', 'Moderator', 'System', 'Worker', 'Anonymous', 'External')
        THROW 56324, 'Audit actor type is invalid.', 1;

    IF @Outcome NOT IN ('Success', 'Failure', 'Denied', 'Ignored')
        THROW 56325, 'Audit outcome is invalid.', 1;

    IF @Severity NOT IN ('Info', 'Warning', 'Error', 'Critical')
        THROW 56326, 'Audit severity is invalid.', 1;

    IF @RiskLevel NOT IN ('Low', 'Medium', 'High', 'Critical')
        THROW 56327, 'Audit risk level is invalid.', 1;

    IF @MetadataJson IS NOT NULL AND ISJSON(@MetadataJson) <> 1
        THROW 56328, 'Audit metadata json is invalid.', 1;

    IF @HeadersJson IS NOT NULL AND ISJSON(@HeadersJson) <> 1
        THROW 56329, 'Audit headers json is invalid.', 1;

    IF @SanitizedPayloadJson IS NOT NULL AND ISJSON(@SanitizedPayloadJson) <> 1
        THROW 56330, 'Audit sanitized payload json is invalid.', 1;

    IF @BeforeJson IS NOT NULL AND ISJSON(@BeforeJson) <> 1
        THROW 56331, 'Audit before json is invalid.', 1;

    IF @AfterJson IS NOT NULL AND ISJSON(@AfterJson) <> 1
        THROW 56332, 'Audit after json is invalid.', 1;

    IF @ChangesJson IS NOT NULL AND ISJSON(@ChangesJson) <> 1
        THROW 56333, 'Audit changes json is invalid.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [audit].[AuditLog]
        WHERE [MessageId] = @MessageId
    )
    BEGIN
        SELECT @AuditLogId = [AuditLogId]
        FROM [audit].[AuditLog]
        WHERE [MessageId] = @MessageId;

        SET @WasInserted = 0;
        RETURN;
    END

    INSERT INTO [audit].[AuditLog]
    (
        [PublicId],
        [MessageId],
        [EventType],
        [EventVersion],
        [SourceModule],
        [Action],
        [ActionCategory],
        [AggregateType],
        [AggregateId],
        [AggregatePublicId],
        [AggregateVersion],
        [ResourceType],
        [ResourceId],
        [ResourceDisplayName],
        [ActorInternalId],
        [ActorUserId],
        [ActorEmail],
        [ActorDisplayName],
        [ActorType],
        [Outcome],
        [Severity],
        [RiskLevel],
        [Summary],
        [Reason],
        [CorrelationId],
        [CausationId],
        [TraceId],
        [IpAddress],
        [UserAgent],
        [SourcePriority],
        [OccurredAtUtc],
        [IngestedAtUtc],
        [CreatedAtUtc],
        [MetadataJson],
        [HeadersJson],
        [SanitizedPayloadJson],
        [BeforeJson],
        [AfterJson],
        [ChangesJson],
        [Hash],
        [PrevHash]
    )
    VALUES
    (
        @PublicId,
        @MessageId,
        @EventType,
        @EventVersion,
        @SourceModule,
        @Action,
        @ActionCategory,
        @AggregateType,
        @AggregateId,
        @AggregatePublicId,
        @AggregateVersion,
        @ResourceType,
        @ResourceId,
        @ResourceDisplayName,
        @ActorInternalId,
        @ActorUserId,
        @ActorEmail,
        @ActorDisplayName,
        @ActorType,
        @Outcome,
        @Severity,
        @RiskLevel,
        @Summary,
        @Reason,
        @CorrelationId,
        @CausationId,
        @TraceId,
        @IpAddress,
        @UserAgent,
        @SourcePriority,
        @OccurredAtUtc,
        @IngestedAtUtc,
        @CreatedAtUtc,
        @MetadataJson,
        @HeadersJson,
        @SanitizedPayloadJson,
        @BeforeJson,
        @AfterJson,
        @ChangesJson,
        @Hash,
        @PrevHash
    );

    SET @AuditLogId = CONVERT(BIGINT, SCOPE_IDENTITY());
    SET @WasInserted = 1;
END;
GO

/* =========================================================
   2) AuditLog — reads
   ========================================================= */

CREATE OR ALTER PROCEDURE [audit].[AuditLog_SelectById]
    @AuditLogId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [AuditLogId],
        [PublicId],
        [MessageId],
        [EventType],
        [EventVersion],
        [SourceModule],
        [Action],
        [ActionCategory],
        [AggregateType],
        [AggregateId],
        [AggregatePublicId],
        [AggregateVersion],
        [ResourceType],
        [ResourceId],
        [ResourceDisplayName],
        [ActorInternalId],
        [ActorUserId],
        [ActorEmail],
        [ActorDisplayName],
        [ActorType],
        [Outcome],
        [Severity],
        [RiskLevel],
        [Summary],
        [Reason],
        [CorrelationId],
        [CausationId],
        [TraceId],
        [IpAddress],
        [UserAgent],
        [SourcePriority],
        [OccurredAtUtc],
        [IngestedAtUtc],
        [CreatedAtUtc],
        [MetadataJson],
        [HeadersJson],
        [SanitizedPayloadJson],
        [BeforeJson],
        [AfterJson],
        [ChangesJson],
        [Hash],
        [PrevHash]
    FROM [audit].[AuditLog]
    WHERE [AuditLogId] = @AuditLogId;
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditLog_SelectByPublicId]
    @PublicId CHAR(26)
AS
BEGIN
    SET NOCOUNT ON;

    SET @PublicId = LTRIM(RTRIM(@PublicId));

    IF NULLIF(@PublicId, '') IS NULL
        THROW 56310, 'Audit public id is required.', 1;

    SELECT
        [AuditLogId],
        [PublicId],
        [MessageId],
        [EventType],
        [EventVersion],
        [SourceModule],
        [Action],
        [ActionCategory],
        [AggregateType],
        [AggregateId],
        [AggregatePublicId],
        [AggregateVersion],
        [ResourceType],
        [ResourceId],
        [ResourceDisplayName],
        [ActorInternalId],
        [ActorUserId],
        [ActorEmail],
        [ActorDisplayName],
        [ActorType],
        [Outcome],
        [Severity],
        [RiskLevel],
        [Summary],
        [Reason],
        [CorrelationId],
        [CausationId],
        [TraceId],
        [IpAddress],
        [UserAgent],
        [SourcePriority],
        [OccurredAtUtc],
        [IngestedAtUtc],
        [CreatedAtUtc],
        [MetadataJson],
        [HeadersJson],
        [SanitizedPayloadJson],
        [BeforeJson],
        [AfterJson],
        [ChangesJson],
        [Hash],
        [PrevHash]
    FROM [audit].[AuditLog]
    WHERE [PublicId] = @PublicId;
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditLog_SelectByMessageId]
    @MessageId CHAR(26)
AS
BEGIN
    SET NOCOUNT ON;

    SET @MessageId = LTRIM(RTRIM(@MessageId));

    IF NULLIF(@MessageId, '') IS NULL
        THROW 56312, 'Audit message id is required.', 1;

    SELECT
        [AuditLogId],
        [PublicId],
        [MessageId],
        [EventType],
        [EventVersion],
        [SourceModule],
        [Action],
        [ActionCategory],
        [AggregateType],
        [AggregateId],
        [AggregatePublicId],
        [AggregateVersion],
        [ResourceType],
        [ResourceId],
        [ResourceDisplayName],
        [ActorInternalId],
        [ActorUserId],
        [ActorEmail],
        [ActorDisplayName],
        [ActorType],
        [Outcome],
        [Severity],
        [RiskLevel],
        [Summary],
        [Reason],
        [CorrelationId],
        [CausationId],
        [TraceId],
        [IpAddress],
        [UserAgent],
        [SourcePriority],
        [OccurredAtUtc],
        [IngestedAtUtc],
        [CreatedAtUtc],
        [MetadataJson],
        [HeadersJson],
        [SanitizedPayloadJson],
        [BeforeJson],
        [AfterJson],
        [ChangesJson],
        [Hash],
        [PrevHash]
    FROM [audit].[AuditLog]
    WHERE [MessageId] = @MessageId;
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditLog_SelectByCorrelationId]
    @CorrelationId NVARCHAR(100),
    @Skip          INT = 0,
    @Take          INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    IF NULLIF(LTRIM(RTRIM(@CorrelationId)), N'') IS NULL
        THROW 56334, 'Correlation id is required.', 1;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;
    IF @Take > 200 SET @Take = 200;

    SELECT
        [AuditLogId],
        [PublicId],
        [MessageId],
        [EventType],
        [EventVersion],
        [SourceModule],
        [Action],
        [ActionCategory],
        [AggregateType],
        [AggregateId],
        [AggregatePublicId],
        [AggregateVersion],
        [ResourceType],
        [ResourceId],
        [ResourceDisplayName],
        [ActorInternalId],
        [ActorUserId],
        [ActorEmail],
        [ActorDisplayName],
        [ActorType],
        [Outcome],
        [Severity],
        [RiskLevel],
        [Summary],
        [Reason],
        [CorrelationId],
        [CausationId],
        [TraceId],
        [IpAddress],
        [UserAgent],
        [SourcePriority],
        [OccurredAtUtc],
        [IngestedAtUtc],
        [CreatedAtUtc],
        [MetadataJson],
        [HeadersJson],
        [SanitizedPayloadJson],
        [BeforeJson],
        [AfterJson],
        [ChangesJson],
        [Hash],
        [PrevHash]
    FROM [audit].[AuditLog]
    WHERE [CorrelationId] = @CorrelationId
    ORDER BY [OccurredAtUtc] DESC, [AuditLogId] DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditLog_SelectByResource]
    @ResourceType NVARCHAR(100),
    @ResourceId   NVARCHAR(100),
    @Skip         INT = 0,
    @Take         INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    IF NULLIF(LTRIM(RTRIM(@ResourceType)), N'') IS NULL
        THROW 56317, 'Audit resource type is required.', 1;

    IF NULLIF(LTRIM(RTRIM(@ResourceId)), N'') IS NULL
        THROW 56318, 'Audit resource id is required.', 1;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;
    IF @Take > 200 SET @Take = 200;

    SELECT
        [AuditLogId],
        [PublicId],
        [MessageId],
        [EventType],
        [EventVersion],
        [SourceModule],
        [Action],
        [ActionCategory],
        [AggregateType],
        [AggregateId],
        [AggregatePublicId],
        [AggregateVersion],
        [ResourceType],
        [ResourceId],
        [ResourceDisplayName],
        [ActorInternalId],
        [ActorUserId],
        [ActorEmail],
        [ActorDisplayName],
        [ActorType],
        [Outcome],
        [Severity],
        [RiskLevel],
        [Summary],
        [Reason],
        [CorrelationId],
        [CausationId],
        [TraceId],
        [IpAddress],
        [UserAgent],
        [SourcePriority],
        [OccurredAtUtc],
        [IngestedAtUtc],
        [CreatedAtUtc],
        [MetadataJson],
        [HeadersJson],
        [SanitizedPayloadJson],
        [BeforeJson],
        [AfterJson],
        [ChangesJson],
        [Hash],
        [PrevHash]
    FROM [audit].[AuditLog]
    WHERE [ResourceType] = @ResourceType
      AND [ResourceId] = @ResourceId
    ORDER BY [OccurredAtUtc] DESC, [AuditLogId] DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditLog_SelectByActorUserId]
    @ActorUserId CHAR(26),
    @Skip        INT = 0,
    @Take        INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    SET @ActorUserId = LTRIM(RTRIM(@ActorUserId));

    IF NULLIF(@ActorUserId, '') IS NULL
        THROW 56335, 'Actor user id is required.', 1;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;
    IF @Take > 200 SET @Take = 200;

    SELECT
        [AuditLogId],
        [PublicId],
        [MessageId],
        [EventType],
        [EventVersion],
        [SourceModule],
        [Action],
        [ActionCategory],
        [AggregateType],
        [AggregateId],
        [AggregatePublicId],
        [AggregateVersion],
        [ResourceType],
        [ResourceId],
        [ResourceDisplayName],
        [ActorInternalId],
        [ActorUserId],
        [ActorEmail],
        [ActorDisplayName],
        [ActorType],
        [Outcome],
        [Severity],
        [RiskLevel],
        [Summary],
        [Reason],
        [CorrelationId],
        [CausationId],
        [TraceId],
        [IpAddress],
        [UserAgent],
        [SourcePriority],
        [OccurredAtUtc],
        [IngestedAtUtc],
        [CreatedAtUtc],
        [MetadataJson],
        [HeadersJson],
        [SanitizedPayloadJson],
        [BeforeJson],
        [AfterJson],
        [ChangesJson],
        [Hash],
        [PrevHash]
    FROM [audit].[AuditLog]
    WHERE [ActorUserId] = @ActorUserId
    ORDER BY [OccurredAtUtc] DESC, [AuditLogId] DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditLog_SelectAll]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [AuditLogId],
        [PublicId],
        [MessageId],
        [EventType],
        [EventVersion],
        [SourceModule],
        [Action],
        [ActionCategory],
        [AggregateType],
        [AggregateId],
        [AggregatePublicId],
        [AggregateVersion],
        [ResourceType],
        [ResourceId],
        [ResourceDisplayName],
        [ActorInternalId],
        [ActorUserId],
        [ActorEmail],
        [ActorDisplayName],
        [ActorType],
        [Outcome],
        [Severity],
        [RiskLevel],
        [Summary],
        [Reason],
        [CorrelationId],
        [CausationId],
        [TraceId],
        [IpAddress],
        [UserAgent],
        [SourcePriority],
        [OccurredAtUtc],
        [IngestedAtUtc],
        [CreatedAtUtc],
        [MetadataJson],
        [HeadersJson],
        [SanitizedPayloadJson],
        [BeforeJson],
        [AfterJson],
        [ChangesJson],
        [Hash],
        [PrevHash]
    FROM [audit].[AuditLog]
    ORDER BY [OccurredAtUtc] DESC, [AuditLogId] DESC;
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditLog_SelectSkipAndTake]
    @Skip INT,
    @Take INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;
    IF @Take > 200 SET @Take = 200;

    SELECT
        [AuditLogId],
        [PublicId],
        [MessageId],
        [EventType],
        [EventVersion],
        [SourceModule],
        [Action],
        [ActionCategory],
        [AggregateType],
        [AggregateId],
        [AggregatePublicId],
        [AggregateVersion],
        [ResourceType],
        [ResourceId],
        [ResourceDisplayName],
        [ActorInternalId],
        [ActorUserId],
        [ActorEmail],
        [ActorDisplayName],
        [ActorType],
        [Outcome],
        [Severity],
        [RiskLevel],
        [Summary],
        [Reason],
        [CorrelationId],
        [CausationId],
        [TraceId],
        [IpAddress],
        [UserAgent],
        [SourcePriority],
        [OccurredAtUtc],
        [IngestedAtUtc],
        [CreatedAtUtc],
        [MetadataJson],
        [HeadersJson],
        [SanitizedPayloadJson],
        [BeforeJson],
        [AfterJson],
        [ChangesJson],
        [Hash],
        [PrevHash]
    FROM [audit].[AuditLog]
    ORDER BY [OccurredAtUtc] DESC, [AuditLogId] DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditLog_SelectSkipAndTakeWhereDynamic]
    @FromOccurredAtUtc  DATETIME2(3) = NULL,
    @ToOccurredAtUtc    DATETIME2(3) = NULL,

    @SourceModule       NVARCHAR(100) = NULL,
    @EventType          NVARCHAR(200) = NULL,
    @Action             NVARCHAR(120) = NULL,
    @ActionCategory     NVARCHAR(100) = NULL,

    @ActorUserId        CHAR(26) = NULL,
    @ActorInternalId    BIGINT = NULL,

    @ResourceType       NVARCHAR(100) = NULL,
    @ResourceId         NVARCHAR(100) = NULL,

    @CorrelationId      NVARCHAR(100) = NULL,
    @MessageId          CHAR(26) = NULL,

    @Outcome            VARCHAR(30) = NULL,
    @Severity           VARCHAR(30) = NULL,
    @RiskLevel          VARCHAR(30) = NULL,

    @SortBy             NVARCHAR(100) = NULL,
    @SortDirection      VARCHAR(4) = NULL,

    @Skip               INT = 0,
    @Take               INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;
    IF @Take > 200 SET @Take = 200;

    IF @FromOccurredAtUtc IS NOT NULL
       AND @ToOccurredAtUtc IS NOT NULL
       AND @FromOccurredAtUtc > @ToOccurredAtUtc
        THROW 56336, 'Audit time range is invalid.', 1;

    IF @ActorUserId IS NOT NULL
       AND NULLIF(LTRIM(RTRIM(@ActorUserId)), '') IS NULL
        SET @ActorUserId = NULL;

    IF @MessageId IS NOT NULL
       AND NULLIF(LTRIM(RTRIM(@MessageId)), '') IS NULL
        SET @MessageId = NULL;

    DECLARE @NormalizedSortBy NVARCHAR(100) = NULLIF(LTRIM(RTRIM(@SortBy)), N'');
    DECLARE @NormalizedSortDirection VARCHAR(4) = LOWER(NULLIF(LTRIM(RTRIM(@SortDirection)), ''));

    SET @NormalizedSortBy = ISNULL(@NormalizedSortBy, N'OccurredAtUtc');
    SET @NormalizedSortDirection = ISNULL(@NormalizedSortDirection, 'desc');

    IF @NormalizedSortBy NOT IN
    (
        N'OccurredAtUtc',
        N'IngestedAtUtc',
        N'CreatedAtUtc',
        N'SourceModule',
        N'EventType',
        N'Action',
        N'ActionCategory',
        N'ResourceType',
        N'ResourceId',
        N'ActorUserId',
        N'ActorInternalId',
        N'Outcome',
        N'Severity',
        N'RiskLevel'
    )
        THROW 56337, 'Audit sort field is invalid.', 1;

    IF @NormalizedSortDirection NOT IN ('asc', 'desc')
        THROW 56338, 'Audit sort direction is invalid.', 1;

    SELECT
        [AuditLogId],
        [PublicId],
        [MessageId],
        [EventType],
        [EventVersion],
        [SourceModule],
        [Action],
        [ActionCategory],
        [AggregateType],
        [AggregateId],
        [AggregatePublicId],
        [AggregateVersion],
        [ResourceType],
        [ResourceId],
        [ResourceDisplayName],
        [ActorInternalId],
        [ActorUserId],
        [ActorEmail],
        [ActorDisplayName],
        [ActorType],
        [Outcome],
        [Severity],
        [RiskLevel],
        [Summary],
        [Reason],
        [CorrelationId],
        [CausationId],
        [TraceId],
        [IpAddress],
        [UserAgent],
        [SourcePriority],
        [OccurredAtUtc],
        [IngestedAtUtc],
        [CreatedAtUtc],
        [MetadataJson],
        [HeadersJson],
        [SanitizedPayloadJson],
        [BeforeJson],
        [AfterJson],
        [ChangesJson],
        [Hash],
        [PrevHash]
    FROM [audit].[AuditLog]
    WHERE
        (@FromOccurredAtUtc IS NULL OR [OccurredAtUtc] >= @FromOccurredAtUtc)
        AND (@ToOccurredAtUtc IS NULL OR [OccurredAtUtc] <= @ToOccurredAtUtc)
        AND (@SourceModule IS NULL OR [SourceModule] = @SourceModule)
        AND (@EventType IS NULL OR [EventType] = @EventType)
        AND (@Action IS NULL OR [Action] = @Action)
        AND (@ActionCategory IS NULL OR [ActionCategory] = @ActionCategory)
        AND (@ActorUserId IS NULL OR [ActorUserId] = LTRIM(RTRIM(@ActorUserId)))
        AND (@ActorInternalId IS NULL OR [ActorInternalId] = @ActorInternalId)
        AND (@ResourceType IS NULL OR [ResourceType] = @ResourceType)
        AND (@ResourceId IS NULL OR [ResourceId] = @ResourceId)
        AND (@CorrelationId IS NULL OR [CorrelationId] = @CorrelationId)
        AND (@MessageId IS NULL OR [MessageId] = LTRIM(RTRIM(@MessageId)))
        AND (@Outcome IS NULL OR [Outcome] = @Outcome)
        AND (@Severity IS NULL OR [Severity] = @Severity)
        AND (@RiskLevel IS NULL OR [RiskLevel] = @RiskLevel)
    ORDER BY
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'OccurredAtUtc' THEN [OccurredAtUtc] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'IngestedAtUtc' THEN [IngestedAtUtc] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'CreatedAtUtc' THEN [CreatedAtUtc] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'ActorInternalId' THEN [ActorInternalId] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'SourceModule' THEN [SourceModule] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'EventType' THEN [EventType] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'Action' THEN [Action] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'ActionCategory' THEN [ActionCategory] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'ResourceType' THEN [ResourceType] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'ResourceId' THEN [ResourceId] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'ActorUserId' THEN [ActorUserId] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'Outcome' THEN [Outcome] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'Severity' THEN [Severity] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'RiskLevel' THEN [RiskLevel] END ASC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'OccurredAtUtc' THEN [OccurredAtUtc] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'IngestedAtUtc' THEN [IngestedAtUtc] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'CreatedAtUtc' THEN [CreatedAtUtc] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'ActorInternalId' THEN [ActorInternalId] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'SourceModule' THEN [SourceModule] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'EventType' THEN [EventType] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'Action' THEN [Action] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'ActionCategory' THEN [ActionCategory] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'ResourceType' THEN [ResourceType] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'ResourceId' THEN [ResourceId] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'ActorUserId' THEN [ActorUserId] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'Outcome' THEN [Outcome] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'Severity' THEN [Severity] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'RiskLevel' THEN [RiskLevel] END DESC,
        [OccurredAtUtc] DESC,
        [AuditLogId] DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditLog_GetRecordCount]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT_BIG(1) AS [RecordCount]
    FROM [audit].[AuditLog];
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditLog_GetRecordCountWhereDynamic]
    @FromOccurredAtUtc  DATETIME2(3) = NULL,
    @ToOccurredAtUtc    DATETIME2(3) = NULL,

    @SourceModule       NVARCHAR(100) = NULL,
    @EventType          NVARCHAR(200) = NULL,
    @Action             NVARCHAR(120) = NULL,
    @ActionCategory     NVARCHAR(100) = NULL,

    @ActorUserId        CHAR(26) = NULL,
    @ActorInternalId    BIGINT = NULL,

    @ResourceType       NVARCHAR(100) = NULL,
    @ResourceId         NVARCHAR(100) = NULL,

    @CorrelationId      NVARCHAR(100) = NULL,
    @MessageId          CHAR(26) = NULL,

    @Outcome            VARCHAR(30) = NULL,
    @Severity           VARCHAR(30) = NULL,
    @RiskLevel          VARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromOccurredAtUtc IS NOT NULL
       AND @ToOccurredAtUtc IS NOT NULL
       AND @FromOccurredAtUtc > @ToOccurredAtUtc
        THROW 56336, 'Audit time range is invalid.', 1;

    IF @ActorUserId IS NOT NULL
       AND NULLIF(LTRIM(RTRIM(@ActorUserId)), '') IS NULL
        SET @ActorUserId = NULL;

    IF @MessageId IS NOT NULL
       AND NULLIF(LTRIM(RTRIM(@MessageId)), '') IS NULL
        SET @MessageId = NULL;

    SELECT COUNT_BIG(1) AS [RecordCount]
    FROM [audit].[AuditLog]
    WHERE
        (@FromOccurredAtUtc IS NULL OR [OccurredAtUtc] >= @FromOccurredAtUtc)
        AND (@ToOccurredAtUtc IS NULL OR [OccurredAtUtc] <= @ToOccurredAtUtc)
        AND (@SourceModule IS NULL OR [SourceModule] = @SourceModule)
        AND (@EventType IS NULL OR [EventType] = @EventType)
        AND (@Action IS NULL OR [Action] = @Action)
        AND (@ActionCategory IS NULL OR [ActionCategory] = @ActionCategory)
        AND (@ActorUserId IS NULL OR [ActorUserId] = LTRIM(RTRIM(@ActorUserId)))
        AND (@ActorInternalId IS NULL OR [ActorInternalId] = @ActorInternalId)
        AND (@ResourceType IS NULL OR [ResourceType] = @ResourceType)
        AND (@ResourceId IS NULL OR [ResourceId] = @ResourceId)
        AND (@CorrelationId IS NULL OR [CorrelationId] = @CorrelationId)
        AND (@MessageId IS NULL OR [MessageId] = LTRIM(RTRIM(@MessageId)))
        AND (@Outcome IS NULL OR [Outcome] = @Outcome)
        AND (@Severity IS NULL OR [Severity] = @Severity)
        AND (@RiskLevel IS NULL OR [RiskLevel] = @RiskLevel);
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditLog_SelectRecentRiskEvents]
    @FromOccurredAtUtc  DATETIME2(3) = NULL,
    @ToOccurredAtUtc    DATETIME2(3) = NULL,
    @SourceModule       NVARCHAR(100) = NULL,
    @RiskLevel          VARCHAR(30) = NULL,
    @Limit              INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    IF @Limit <= 0 SET @Limit = 20;
    IF @Limit > 100 SET @Limit = 100;

    IF @FromOccurredAtUtc IS NOT NULL
       AND @ToOccurredAtUtc IS NOT NULL
       AND @FromOccurredAtUtc > @ToOccurredAtUtc
        THROW 56336, 'Audit time range is invalid.', 1;

    IF @RiskLevel IS NOT NULL
       AND @RiskLevel NOT IN ('Low', 'Medium', 'High', 'Critical')
        THROW 56327, 'Audit risk level is invalid.', 1;

    SELECT TOP (@Limit)
        [AuditLogId],
        [PublicId],
        [MessageId],
        [EventType],
        [EventVersion],
        [SourceModule],
        [Action],
        [ActionCategory],
        [AggregateType],
        [AggregateId],
        [AggregatePublicId],
        [AggregateVersion],
        [ResourceType],
        [ResourceId],
        [ResourceDisplayName],
        [ActorInternalId],
        [ActorUserId],
        [ActorEmail],
        [ActorDisplayName],
        [ActorType],
        [Outcome],
        [Severity],
        [RiskLevel],
        [Summary],
        [Reason],
        [CorrelationId],
        [CausationId],
        [TraceId],
        [IpAddress],
        [UserAgent],
        [SourcePriority],
        [OccurredAtUtc],
        [IngestedAtUtc],
        [CreatedAtUtc],
        [MetadataJson],
        [HeadersJson],
        [SanitizedPayloadJson],
        [BeforeJson],
        [AfterJson],
        [ChangesJson],
        [Hash],
        [PrevHash]
    FROM [audit].[AuditLog]
    WHERE
        (@FromOccurredAtUtc IS NULL OR [OccurredAtUtc] >= @FromOccurredAtUtc)
        AND (@ToOccurredAtUtc IS NULL OR [OccurredAtUtc] <= @ToOccurredAtUtc)
        AND (@SourceModule IS NULL OR [SourceModule] = @SourceModule)
        AND
        (
            (@RiskLevel IS NOT NULL AND [RiskLevel] = @RiskLevel)
            OR (@RiskLevel IS NULL AND [RiskLevel] IN ('High', 'Critical'))
        )
    ORDER BY [OccurredAtUtc] DESC, [AuditLogId] DESC;
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditLog_CountByModule]
    @FromOccurredAtUtc  DATETIME2(3) = NULL,
    @ToOccurredAtUtc    DATETIME2(3) = NULL,
    @SourceModule       NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromOccurredAtUtc IS NOT NULL
       AND @ToOccurredAtUtc IS NOT NULL
       AND @FromOccurredAtUtc > @ToOccurredAtUtc
        THROW 56336, 'Audit time range is invalid.', 1;

    SELECT
        [SourceModule] AS [Value],
        COUNT_BIG(1) AS [Count]
    FROM [audit].[AuditLog]
    WHERE
        (@FromOccurredAtUtc IS NULL OR [OccurredAtUtc] >= @FromOccurredAtUtc)
        AND (@ToOccurredAtUtc IS NULL OR [OccurredAtUtc] <= @ToOccurredAtUtc)
        AND (@SourceModule IS NULL OR [SourceModule] = @SourceModule)
    GROUP BY [SourceModule]
    ORDER BY [Count] DESC, [Value] ASC;
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditLog_CountBySeverity]
    @FromOccurredAtUtc  DATETIME2(3) = NULL,
    @ToOccurredAtUtc    DATETIME2(3) = NULL,
    @SourceModule       NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromOccurredAtUtc IS NOT NULL
       AND @ToOccurredAtUtc IS NOT NULL
       AND @FromOccurredAtUtc > @ToOccurredAtUtc
        THROW 56336, 'Audit time range is invalid.', 1;

    SELECT
        [Severity] AS [Value],
        COUNT_BIG(1) AS [Count]
    FROM [audit].[AuditLog]
    WHERE
        (@FromOccurredAtUtc IS NULL OR [OccurredAtUtc] >= @FromOccurredAtUtc)
        AND (@ToOccurredAtUtc IS NULL OR [OccurredAtUtc] <= @ToOccurredAtUtc)
        AND (@SourceModule IS NULL OR [SourceModule] = @SourceModule)
    GROUP BY [Severity]
    ORDER BY [Count] DESC, [Value] ASC;
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditLog_CountByRiskLevel]
    @FromOccurredAtUtc  DATETIME2(3) = NULL,
    @ToOccurredAtUtc    DATETIME2(3) = NULL,
    @SourceModule       NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromOccurredAtUtc IS NOT NULL
       AND @ToOccurredAtUtc IS NOT NULL
       AND @FromOccurredAtUtc > @ToOccurredAtUtc
        THROW 56336, 'Audit time range is invalid.', 1;

    SELECT
        [RiskLevel] AS [Value],
        COUNT_BIG(1) AS [Count]
    FROM [audit].[AuditLog]
    WHERE
        (@FromOccurredAtUtc IS NULL OR [OccurredAtUtc] >= @FromOccurredAtUtc)
        AND (@ToOccurredAtUtc IS NULL OR [OccurredAtUtc] <= @ToOccurredAtUtc)
        AND (@SourceModule IS NULL OR [SourceModule] = @SourceModule)
    GROUP BY [RiskLevel]
    ORDER BY [Count] DESC, [Value] ASC;
END;
GO

/* =========================================================
   3) AuditIngestion — upsert / mark status
   ========================================================= */

CREATE OR ALTER PROCEDURE [audit].[AuditIngestion_UpsertProcessing]
    @PublicId              CHAR(26),
    @MessageId             CHAR(26),
    @EventType             NVARCHAR(200),

    @AggregateType         NVARCHAR(100) = NULL,
    @AggregateId           NVARCHAR(100) = NULL,
    @AggregatePublicId     CHAR(26) = NULL,
    @AggregateVersion      INT = NULL,

    @CorrelationId         NVARCHAR(100) = NULL,
    @SourcePriority        TINYINT = NULL,

    @SourceOccurredAtUtc   DATETIME2(3),
    @SourcePublishedAtUtc  DATETIME2(3) = NULL,

    @ConsumerName          NVARCHAR(150),

    @AuditIngestionId      BIGINT OUTPUT,
    @WasInserted           BIT OUTPUT,
    @CurrentStatus         VARCHAR(30) = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @PublicId = LTRIM(RTRIM(@PublicId));
    SET @MessageId = LTRIM(RTRIM(@MessageId));

    IF NULLIF(@PublicId, '') IS NULL
        THROW 56350, 'Audit ingestion public id is required.', 1;

    IF LEN(@PublicId) <> 26
        THROW 56351, 'Audit ingestion public id must be 26 characters.', 1;

    IF NULLIF(@MessageId, '') IS NULL
        THROW 56352, 'Audit ingestion message id is required.', 1;

    IF LEN(@MessageId) <> 26
        THROW 56353, 'Audit ingestion message id must be 26 characters.', 1;

    IF NULLIF(LTRIM(RTRIM(@EventType)), N'') IS NULL
        THROW 56354, 'Audit ingestion event type is required.', 1;

    IF NULLIF(LTRIM(RTRIM(@ConsumerName)), N'') IS NULL
        THROW 56355, 'Audit ingestion consumer name is required.', 1;

    IF @SourceOccurredAtUtc IS NULL
        THROW 56356, 'Audit ingestion source occurredAtUtc is required.', 1;

    IF @SourcePublishedAtUtc IS NOT NULL
       AND @SourcePublishedAtUtc < @SourceOccurredAtUtc
        THROW 56357, 'Audit ingestion source publishedAtUtc is invalid.', 1;

    IF @AggregateVersion IS NOT NULL AND @AggregateVersion < 1
        THROW 56358, 'Audit ingestion aggregate version must be >= 1.', 1;

    IF @SourcePriority IS NOT NULL AND (@SourcePriority < 1 OR @SourcePriority > 9)
        THROW 56359, 'Audit ingestion source priority must be between 1 and 9.', 1;

    DECLARE @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    IF EXISTS
    (
        SELECT 1
        FROM [audit].[AuditIngestion]
        WHERE [MessageId] = @MessageId
    )
    BEGIN
        UPDATE [audit].[AuditIngestion]
        SET
            [Status] = 'Processing',
            [AttemptCount] = [AttemptCount] + 1,
            [LastAttemptAtUtc] = @NowUtc,
            [UpdatedAtUtc] = @NowUtc,
            [DeadLetteredAtUtc] = NULL,
            [LastErrorCode] = NULL,
            [LastErrorMessage] = NULL,
            [LastErrorClass] = NULL,
            [ProcessedAtUtc] = NULL
        WHERE [MessageId] = @MessageId
          AND [Status] IN ('Processing', 'Failed');

        SELECT
            @AuditIngestionId = [AuditIngestionId],
            @CurrentStatus = [Status]
        FROM [audit].[AuditIngestion]
        WHERE [MessageId] = @MessageId;

        SET @WasInserted = 0;
        RETURN;
    END

    INSERT INTO [audit].[AuditIngestion]
    (
        [PublicId],
        [MessageId],
        [EventType],
        [AggregateType],
        [AggregateId],
        [AggregatePublicId],
        [AggregateVersion],
        [CorrelationId],
        [SourcePriority],
        [SourceOccurredAtUtc],
        [SourcePublishedAtUtc],
        [ConsumerName],
        [Status],
        [AttemptCount],
        [FirstReceivedAtUtc],
        [LastAttemptAtUtc]
    )
    VALUES
    (
        @PublicId,
        @MessageId,
        @EventType,
        @AggregateType,
        @AggregateId,
        @AggregatePublicId,
        @AggregateVersion,
        @CorrelationId,
        @SourcePriority,
        @SourceOccurredAtUtc,
        @SourcePublishedAtUtc,
        @ConsumerName,
        'Processing',
        1,
        @NowUtc,
        @NowUtc
    );

    SET @AuditIngestionId = CONVERT(BIGINT, SCOPE_IDENTITY());
    SET @WasInserted = 1;
    SET @CurrentStatus = 'Processing';
END;
GO

/* =========================================================
   3.1) AuditIngestion — upsert unsupported event
   ========================================================= */

CREATE OR ALTER PROCEDURE [audit].[AuditIngestion_UpsertUnsupported]
    @PublicId              CHAR(26),
    @MessageId             CHAR(26),
    @EventType             NVARCHAR(200),

    @AggregateType         NVARCHAR(100) = NULL,
    @AggregateId           NVARCHAR(100) = NULL,
    @AggregatePublicId     CHAR(26) = NULL,
    @AggregateVersion      INT = NULL,

    @CorrelationId         NVARCHAR(100) = NULL,
    @SourcePriority        TINYINT = NULL,

    @SourceOccurredAtUtc   DATETIME2(3),
    @SourcePublishedAtUtc  DATETIME2(3) = NULL,

    @ConsumerName          NVARCHAR(150),

    @AuditIngestionId      BIGINT OUTPUT,
    @WasInserted           BIT OUTPUT,
    @CurrentStatus         VARCHAR(30) = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @PublicId = LTRIM(RTRIM(@PublicId));
    SET @MessageId = LTRIM(RTRIM(@MessageId));

    IF NULLIF(@PublicId, '') IS NULL
        THROW 56350, 'Audit ingestion public id is required.', 1;

    IF LEN(@PublicId) <> 26
        THROW 56351, 'Audit ingestion public id must be 26 characters.', 1;

    IF NULLIF(@MessageId, '') IS NULL
        THROW 56352, 'Audit ingestion message id is required.', 1;

    IF LEN(@MessageId) <> 26
        THROW 56353, 'Audit ingestion message id must be 26 characters.', 1;

    IF NULLIF(LTRIM(RTRIM(@EventType)), N'') IS NULL
        THROW 56354, 'Audit ingestion event type is required.', 1;

    IF NULLIF(LTRIM(RTRIM(@ConsumerName)), N'') IS NULL
        THROW 56355, 'Audit ingestion consumer name is required.', 1;

    IF @SourceOccurredAtUtc IS NULL
        THROW 56356, 'Audit ingestion source occurredAtUtc is required.', 1;

    IF @SourcePublishedAtUtc IS NOT NULL
       AND @SourcePublishedAtUtc < @SourceOccurredAtUtc
        THROW 56357, 'Audit ingestion source publishedAtUtc is invalid.', 1;

    IF @AggregateVersion IS NOT NULL AND @AggregateVersion < 1
        THROW 56358, 'Audit ingestion aggregate version must be >= 1.', 1;

    IF @SourcePriority IS NOT NULL AND (@SourcePriority < 1 OR @SourcePriority > 9)
        THROW 56359, 'Audit ingestion source priority must be between 1 and 9.', 1;

    DECLARE @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    IF EXISTS
    (
        SELECT 1
        FROM [audit].[AuditIngestion]
        WHERE [MessageId] = @MessageId
    )
    BEGIN
        SELECT
            @AuditIngestionId = [AuditIngestionId],
            @CurrentStatus = [Status]
        FROM [audit].[AuditIngestion]
        WHERE [MessageId] = @MessageId;

        SET @WasInserted = 0;
        RETURN;
    END

    INSERT INTO [audit].[AuditIngestion]
    (
        [PublicId],
        [MessageId],
        [EventType],
        [AggregateType],
        [AggregateId],
        [AggregatePublicId],
        [AggregateVersion],
        [CorrelationId],
        [SourcePriority],
        [SourceOccurredAtUtc],
        [SourcePublishedAtUtc],
        [ConsumerName],
        [Status],
        [AttemptCount],
        [FirstReceivedAtUtc],
        [LastAttemptAtUtc]
    )
    VALUES
    (
        @PublicId,
        @MessageId,
        @EventType,
        @AggregateType,
        @AggregateId,
        @AggregatePublicId,
        @AggregateVersion,
        @CorrelationId,
        @SourcePriority,
        @SourceOccurredAtUtc,
        @SourcePublishedAtUtc,
        @ConsumerName,
        'Processing',
        1,
        @NowUtc,
        @NowUtc
    );

    SET @AuditIngestionId = CONVERT(BIGINT, SCOPE_IDENTITY());
    SET @WasInserted = 1;
    SET @CurrentStatus = 'Processing';
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditIngestion_MarkSucceeded]
    @MessageId CHAR(26)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @MessageId = LTRIM(RTRIM(@MessageId));

    IF NULLIF(@MessageId, '') IS NULL
        THROW 56352, 'Audit ingestion message id is required.', 1;

    DECLARE @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    UPDATE [audit].[AuditIngestion]
    SET
        [Status] = 'Succeeded',
        [LastAttemptAtUtc] = @NowUtc,
        [ProcessedAtUtc] = @NowUtc,
        [UpdatedAtUtc] = @NowUtc,
        [DeadLetteredAtUtc] = NULL,
        [LastErrorCode] = NULL,
        [LastErrorMessage] = NULL,
        [LastErrorClass] = NULL
    WHERE [MessageId] = @MessageId;
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditIngestion_MarkDuplicate]
    @MessageId CHAR(26)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @MessageId = LTRIM(RTRIM(@MessageId));

    IF NULLIF(@MessageId, '') IS NULL
        THROW 56352, 'Audit ingestion message id is required.', 1;

    DECLARE @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    UPDATE [audit].[AuditIngestion]
    SET
        [Status] = 'Duplicate',
        [LastAttemptAtUtc] = @NowUtc,
        [ProcessedAtUtc] = @NowUtc,
        [UpdatedAtUtc] = @NowUtc,
        [DeadLetteredAtUtc] = NULL,
        [LastErrorCode] = NULL,
        [LastErrorMessage] = NULL,
        [LastErrorClass] = NULL
    WHERE [MessageId] = @MessageId;
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditIngestion_MarkIgnored]
    @MessageId CHAR(26)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @MessageId = LTRIM(RTRIM(@MessageId));

    IF NULLIF(@MessageId, '') IS NULL
        THROW 56352, 'Audit ingestion message id is required.', 1;

    DECLARE @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    UPDATE [audit].[AuditIngestion]
    SET
        [Status] = 'Ignored',
        [LastAttemptAtUtc] = @NowUtc,
        [ProcessedAtUtc] = @NowUtc,
        [UpdatedAtUtc] = @NowUtc,
        [DeadLetteredAtUtc] = NULL,
        [LastErrorCode] = NULL,
        [LastErrorMessage] = NULL,
        [LastErrorClass] = NULL
    WHERE [MessageId] = @MessageId;
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditIngestion_MarkFailed]
    @MessageId          CHAR(26),
    @LastErrorCode      NVARCHAR(100) = NULL,
    @LastErrorMessage   NVARCHAR(2000) = NULL,
    @LastErrorClass     VARCHAR(30) = 'Unknown'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @MessageId = LTRIM(RTRIM(@MessageId));

    IF NULLIF(@MessageId, '') IS NULL
        THROW 56352, 'Audit ingestion message id is required.', 1;

    IF @LastErrorClass IS NOT NULL
       AND @LastErrorClass NOT IN ('Transient', 'Permanent', 'Ambiguous', 'Validation', 'Policy', 'Redaction', 'Unknown')
        THROW 56360, 'Audit ingestion error class is invalid.', 1;

    DECLARE @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    UPDATE [audit].[AuditIngestion]
    SET
        [Status] = 'Failed',
        [LastAttemptAtUtc] = @NowUtc,
        [ProcessedAtUtc] = NULL,
        [UpdatedAtUtc] = @NowUtc,
        [DeadLetteredAtUtc] = NULL,
        [LastErrorCode] = @LastErrorCode,
        [LastErrorMessage] = @LastErrorMessage,
        [LastErrorClass] = ISNULL(@LastErrorClass, 'Unknown')
    WHERE [MessageId] = @MessageId;
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditIngestion_MarkDeadLettered]
    @MessageId          CHAR(26),
    @LastErrorCode      NVARCHAR(100) = NULL,
    @LastErrorMessage   NVARCHAR(2000) = NULL,
    @LastErrorClass     VARCHAR(30) = 'Unknown'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @MessageId = LTRIM(RTRIM(@MessageId));

    IF NULLIF(@MessageId, '') IS NULL
        THROW 56352, 'Audit ingestion message id is required.', 1;

    IF @LastErrorClass IS NOT NULL
       AND @LastErrorClass NOT IN ('Transient', 'Permanent', 'Ambiguous', 'Validation', 'Policy', 'Redaction', 'Unknown')
        THROW 56360, 'Audit ingestion error class is invalid.', 1;

    DECLARE @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    UPDATE [audit].[AuditIngestion]
    SET
        [Status] = 'DeadLettered',
        [LastAttemptAtUtc] = @NowUtc,
        [ProcessedAtUtc] = @NowUtc,
        [DeadLetteredAtUtc] = @NowUtc,
        [UpdatedAtUtc] = @NowUtc,
        [LastErrorCode] = @LastErrorCode,
        [LastErrorMessage] = @LastErrorMessage,
        [LastErrorClass] = ISNULL(@LastErrorClass, 'Unknown')
    WHERE [MessageId] = @MessageId;
END;
GO

/* =========================================================
   4) AuditIngestion — reads
   ========================================================= */

CREATE OR ALTER PROCEDURE [audit].[AuditIngestion_SelectById]
    @AuditIngestionId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [AuditIngestionId],
        [PublicId],
        [MessageId],
        [EventType],
        [AggregateType],
        [AggregateId],
        [AggregatePublicId],
        [AggregateVersion],
        [CorrelationId],
        [SourcePriority],
        [SourceOccurredAtUtc],
        [SourcePublishedAtUtc],
        [ConsumerName],
        [Status],
        [AttemptCount],
        [FirstReceivedAtUtc],
        [LastAttemptAtUtc],
        [ProcessedAtUtc],
        [DeadLetteredAtUtc],
        [LastErrorCode],
        [LastErrorMessage],
        [LastErrorClass],
        [CreatedAtUtc],
        [UpdatedAtUtc]
    FROM [audit].[AuditIngestion]
    WHERE [AuditIngestionId] = @AuditIngestionId;
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditIngestion_SelectByPublicId]
    @PublicId CHAR(26)
AS
BEGIN
    SET NOCOUNT ON;

    SET @PublicId = LTRIM(RTRIM(@PublicId));

    IF NULLIF(@PublicId, '') IS NULL
        THROW 56350, 'Audit ingestion public id is required.', 1;

    SELECT
        [AuditIngestionId],
        [PublicId],
        [MessageId],
        [EventType],
        [AggregateType],
        [AggregateId],
        [AggregatePublicId],
        [AggregateVersion],
        [CorrelationId],
        [SourcePriority],
        [SourceOccurredAtUtc],
        [SourcePublishedAtUtc],
        [ConsumerName],
        [Status],
        [AttemptCount],
        [FirstReceivedAtUtc],
        [LastAttemptAtUtc],
        [ProcessedAtUtc],
        [DeadLetteredAtUtc],
        [LastErrorCode],
        [LastErrorMessage],
        [LastErrorClass],
        [CreatedAtUtc],
        [UpdatedAtUtc]
    FROM [audit].[AuditIngestion]
    WHERE [PublicId] = @PublicId;
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditIngestion_SelectByMessageId]
    @MessageId CHAR(26)
AS
BEGIN
    SET NOCOUNT ON;

    SET @MessageId = LTRIM(RTRIM(@MessageId));

    IF NULLIF(@MessageId, '') IS NULL
        THROW 56352, 'Audit ingestion message id is required.', 1;

    SELECT
        [AuditIngestionId],
        [PublicId],
        [MessageId],
        [EventType],
        [AggregateType],
        [AggregateId],
        [AggregatePublicId],
        [AggregateVersion],
        [CorrelationId],
        [SourcePriority],
        [SourceOccurredAtUtc],
        [SourcePublishedAtUtc],
        [ConsumerName],
        [Status],
        [AttemptCount],
        [FirstReceivedAtUtc],
        [LastAttemptAtUtc],
        [ProcessedAtUtc],
        [DeadLetteredAtUtc],
        [LastErrorCode],
        [LastErrorMessage],
        [LastErrorClass],
        [CreatedAtUtc],
        [UpdatedAtUtc]
    FROM [audit].[AuditIngestion]
    WHERE [MessageId] = @MessageId;
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditIngestion_SelectFailed]
    @Skip INT = 0,
    @Take INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;
    IF @Take > 200 SET @Take = 200;

    SELECT
        [AuditIngestionId],
        [PublicId],
        [MessageId],
        [EventType],
        [AggregateType],
        [AggregateId],
        [AggregatePublicId],
        [AggregateVersion],
        [CorrelationId],
        [SourcePriority],
        [SourceOccurredAtUtc],
        [SourcePublishedAtUtc],
        [ConsumerName],
        [Status],
        [AttemptCount],
        [FirstReceivedAtUtc],
        [LastAttemptAtUtc],
        [ProcessedAtUtc],
        [DeadLetteredAtUtc],
        [LastErrorCode],
        [LastErrorMessage],
        [LastErrorClass],
        [CreatedAtUtc],
        [UpdatedAtUtc]
    FROM [audit].[AuditIngestion]
    WHERE [Status] IN ('Failed', 'DeadLettered')
    ORDER BY [LastAttemptAtUtc] DESC, [AuditIngestionId] DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditIngestion_SelectSkipAndTakeWhereDynamic]
    @FromFirstReceivedAtUtc DATETIME2(3) = NULL,
    @ToFirstReceivedAtUtc   DATETIME2(3) = NULL,

    @Status                 VARCHAR(30) = NULL,
    @MessageId              CHAR(26) = NULL,
    @EventType              NVARCHAR(200) = NULL,
    @AggregateType          NVARCHAR(100) = NULL,
    @AggregateId            NVARCHAR(100) = NULL,
    @AggregatePublicId      CHAR(26) = NULL,
    @CorrelationId          NVARCHAR(100) = NULL,
    @ConsumerName           NVARCHAR(150) = NULL,
    @LastErrorClass         VARCHAR(30) = NULL,

    @SortBy                 NVARCHAR(100) = NULL,
    @SortDirection          VARCHAR(4) = NULL,

    @Skip                   INT = 0,
    @Take                   INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;
    IF @Take > 200 SET @Take = 200;

    IF @FromFirstReceivedAtUtc IS NOT NULL
       AND @ToFirstReceivedAtUtc IS NOT NULL
       AND @FromFirstReceivedAtUtc > @ToFirstReceivedAtUtc
        THROW 56361, 'Audit ingestion time range is invalid.', 1;

    IF @Status IS NOT NULL
       AND @Status NOT IN ('Processing', 'Succeeded', 'Duplicate', 'Ignored', 'Failed', 'DeadLettered')
        THROW 56362, 'Audit ingestion status is invalid.', 1;

    IF @MessageId IS NOT NULL
       AND NULLIF(LTRIM(RTRIM(@MessageId)), '') IS NULL
        SET @MessageId = NULL;

    IF @AggregatePublicId IS NOT NULL
       AND NULLIF(LTRIM(RTRIM(@AggregatePublicId)), '') IS NULL
        SET @AggregatePublicId = NULL;

    IF @LastErrorClass IS NOT NULL
       AND @LastErrorClass NOT IN ('Transient', 'Permanent', 'Ambiguous', 'Validation', 'Policy', 'Redaction', 'Unknown')
        THROW 56360, 'Audit ingestion error class is invalid.', 1;

    DECLARE @NormalizedSortBy NVARCHAR(100) = NULLIF(LTRIM(RTRIM(@SortBy)), N'');
    DECLARE @NormalizedSortDirection VARCHAR(4) = LOWER(NULLIF(LTRIM(RTRIM(@SortDirection)), ''));

    SET @NormalizedSortBy = ISNULL(@NormalizedSortBy, N'FirstReceivedAtUtc');
    SET @NormalizedSortDirection = ISNULL(@NormalizedSortDirection, 'desc');

    IF @NormalizedSortBy NOT IN
    (
        N'SourceOccurredAtUtc',
        N'SourcePublishedAtUtc',
        N'FirstReceivedAtUtc',
        N'LastAttemptAtUtc',
        N'ProcessedAtUtc',
        N'DeadLetteredAtUtc',
        N'CreatedAtUtc',
        N'UpdatedAtUtc',
        N'AttemptCount',
        N'Status',
        N'EventType',
        N'ConsumerName',
        N'LastErrorClass'
    )
        THROW 56363, 'Audit ingestion sort field is invalid.', 1;

    IF @NormalizedSortDirection NOT IN ('asc', 'desc')
        THROW 56364, 'Audit ingestion sort direction is invalid.', 1;

    SELECT
        [AuditIngestionId],
        [PublicId],
        [MessageId],
        [EventType],
        [AggregateType],
        [AggregateId],
        [AggregatePublicId],
        [AggregateVersion],
        [CorrelationId],
        [SourcePriority],
        [SourceOccurredAtUtc],
        [SourcePublishedAtUtc],
        [ConsumerName],
        [Status],
        [AttemptCount],
        [FirstReceivedAtUtc],
        [LastAttemptAtUtc],
        [ProcessedAtUtc],
        [DeadLetteredAtUtc],
        [LastErrorCode],
        [LastErrorMessage],
        [LastErrorClass],
        [CreatedAtUtc],
        [UpdatedAtUtc]
    FROM [audit].[AuditIngestion]
    WHERE
        (@FromFirstReceivedAtUtc IS NULL OR [FirstReceivedAtUtc] >= @FromFirstReceivedAtUtc)
        AND (@ToFirstReceivedAtUtc IS NULL OR [FirstReceivedAtUtc] <= @ToFirstReceivedAtUtc)
        AND (@Status IS NULL OR [Status] = @Status)
        AND (@MessageId IS NULL OR [MessageId] = LTRIM(RTRIM(@MessageId)))
        AND (@EventType IS NULL OR [EventType] = @EventType)
        AND (@AggregateType IS NULL OR [AggregateType] = @AggregateType)
        AND (@AggregateId IS NULL OR [AggregateId] = @AggregateId)
        AND (@AggregatePublicId IS NULL OR [AggregatePublicId] = LTRIM(RTRIM(@AggregatePublicId)))
        AND (@CorrelationId IS NULL OR [CorrelationId] = @CorrelationId)
        AND (@ConsumerName IS NULL OR [ConsumerName] = @ConsumerName)
        AND (@LastErrorClass IS NULL OR [LastErrorClass] = @LastErrorClass)
    ORDER BY
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'SourceOccurredAtUtc' THEN [SourceOccurredAtUtc] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'SourcePublishedAtUtc' THEN [SourcePublishedAtUtc] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'FirstReceivedAtUtc' THEN [FirstReceivedAtUtc] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'LastAttemptAtUtc' THEN [LastAttemptAtUtc] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'ProcessedAtUtc' THEN [ProcessedAtUtc] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'DeadLetteredAtUtc' THEN [DeadLetteredAtUtc] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'CreatedAtUtc' THEN [CreatedAtUtc] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'UpdatedAtUtc' THEN [UpdatedAtUtc] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'AttemptCount' THEN [AttemptCount] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'Status' THEN [Status] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'EventType' THEN [EventType] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'ConsumerName' THEN [ConsumerName] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'LastErrorClass' THEN [LastErrorClass] END ASC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'SourceOccurredAtUtc' THEN [SourceOccurredAtUtc] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'SourcePublishedAtUtc' THEN [SourcePublishedAtUtc] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'FirstReceivedAtUtc' THEN [FirstReceivedAtUtc] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'LastAttemptAtUtc' THEN [LastAttemptAtUtc] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'ProcessedAtUtc' THEN [ProcessedAtUtc] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'DeadLetteredAtUtc' THEN [DeadLetteredAtUtc] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'CreatedAtUtc' THEN [CreatedAtUtc] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'UpdatedAtUtc' THEN [UpdatedAtUtc] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'AttemptCount' THEN [AttemptCount] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'Status' THEN [Status] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'EventType' THEN [EventType] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'ConsumerName' THEN [ConsumerName] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'LastErrorClass' THEN [LastErrorClass] END DESC,
        [FirstReceivedAtUtc] DESC,
        [AuditIngestionId] DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditIngestion_GetRecordCountWhereDynamic]
    @FromFirstReceivedAtUtc DATETIME2(3) = NULL,
    @ToFirstReceivedAtUtc   DATETIME2(3) = NULL,

    @Status                 VARCHAR(30) = NULL,
    @MessageId              CHAR(26) = NULL,
    @EventType              NVARCHAR(200) = NULL,
    @AggregateType          NVARCHAR(100) = NULL,
    @AggregateId            NVARCHAR(100) = NULL,
    @AggregatePublicId      CHAR(26) = NULL,
    @CorrelationId          NVARCHAR(100) = NULL,
    @ConsumerName           NVARCHAR(150) = NULL,
    @LastErrorClass         VARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromFirstReceivedAtUtc IS NOT NULL
       AND @ToFirstReceivedAtUtc IS NOT NULL
       AND @FromFirstReceivedAtUtc > @ToFirstReceivedAtUtc
        THROW 56361, 'Audit ingestion time range is invalid.', 1;

    IF @Status IS NOT NULL
       AND @Status NOT IN ('Processing', 'Succeeded', 'Duplicate', 'Ignored', 'Failed', 'DeadLettered')
        THROW 56362, 'Audit ingestion status is invalid.', 1;

    IF @MessageId IS NOT NULL
       AND NULLIF(LTRIM(RTRIM(@MessageId)), '') IS NULL
        SET @MessageId = NULL;

    IF @AggregatePublicId IS NOT NULL
       AND NULLIF(LTRIM(RTRIM(@AggregatePublicId)), '') IS NULL
        SET @AggregatePublicId = NULL;

    IF @LastErrorClass IS NOT NULL
       AND @LastErrorClass NOT IN ('Transient', 'Permanent', 'Ambiguous', 'Validation', 'Policy', 'Redaction', 'Unknown')
        THROW 56360, 'Audit ingestion error class is invalid.', 1;

    SELECT COUNT_BIG(1) AS [RecordCount]
    FROM [audit].[AuditIngestion]
    WHERE
        (@FromFirstReceivedAtUtc IS NULL OR [FirstReceivedAtUtc] >= @FromFirstReceivedAtUtc)
        AND (@ToFirstReceivedAtUtc IS NULL OR [FirstReceivedAtUtc] <= @ToFirstReceivedAtUtc)
        AND (@Status IS NULL OR [Status] = @Status)
        AND (@MessageId IS NULL OR [MessageId] = LTRIM(RTRIM(@MessageId)))
        AND (@EventType IS NULL OR [EventType] = @EventType)
        AND (@AggregateType IS NULL OR [AggregateType] = @AggregateType)
        AND (@AggregateId IS NULL OR [AggregateId] = @AggregateId)
        AND (@AggregatePublicId IS NULL OR [AggregatePublicId] = LTRIM(RTRIM(@AggregatePublicId)))
        AND (@CorrelationId IS NULL OR [CorrelationId] = @CorrelationId)
        AND (@ConsumerName IS NULL OR [ConsumerName] = @ConsumerName)
        AND (@LastErrorClass IS NULL OR [LastErrorClass] = @LastErrorClass);
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditIngestion_SelectFailedWhereDynamic]
    @FromFirstReceivedAtUtc DATETIME2(3) = NULL,
    @ToFirstReceivedAtUtc   DATETIME2(3) = NULL,

    @EventType              NVARCHAR(200) = NULL,
    @AggregateType          NVARCHAR(100) = NULL,
    @AggregateId            NVARCHAR(100) = NULL,
    @AggregatePublicId      CHAR(26) = NULL,
    @CorrelationId          NVARCHAR(100) = NULL,
    @ConsumerName           NVARCHAR(150) = NULL,
    @LastErrorClass         VARCHAR(30) = NULL,

    @SortBy                 NVARCHAR(100) = NULL,
    @SortDirection          VARCHAR(4) = NULL,

    @Skip                   INT = 0,
    @Take                   INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;
    IF @Take > 200 SET @Take = 200;

    IF @FromFirstReceivedAtUtc IS NOT NULL
       AND @ToFirstReceivedAtUtc IS NOT NULL
       AND @FromFirstReceivedAtUtc > @ToFirstReceivedAtUtc
        THROW 56361, 'Audit ingestion time range is invalid.', 1;

    IF @AggregatePublicId IS NOT NULL
       AND NULLIF(LTRIM(RTRIM(@AggregatePublicId)), '') IS NULL
        SET @AggregatePublicId = NULL;

    IF @LastErrorClass IS NOT NULL
       AND @LastErrorClass NOT IN ('Transient', 'Permanent', 'Ambiguous', 'Validation', 'Policy', 'Redaction', 'Unknown')
        THROW 56360, 'Audit ingestion error class is invalid.', 1;

    DECLARE @NormalizedSortBy NVARCHAR(100) = NULLIF(LTRIM(RTRIM(@SortBy)), N'');
    DECLARE @NormalizedSortDirection VARCHAR(4) = LOWER(NULLIF(LTRIM(RTRIM(@SortDirection)), ''));

    SET @NormalizedSortBy = ISNULL(@NormalizedSortBy, N'LastAttemptAtUtc');
    SET @NormalizedSortDirection = ISNULL(@NormalizedSortDirection, 'desc');

    IF @NormalizedSortBy NOT IN
    (
        N'SourceOccurredAtUtc',
        N'SourcePublishedAtUtc',
        N'FirstReceivedAtUtc',
        N'LastAttemptAtUtc',
        N'CreatedAtUtc',
        N'UpdatedAtUtc',
        N'AttemptCount',
        N'EventType',
        N'ConsumerName',
        N'LastErrorClass'
    )
        THROW 56363, 'Audit ingestion sort field is invalid.', 1;

    IF @NormalizedSortDirection NOT IN ('asc', 'desc')
        THROW 56364, 'Audit ingestion sort direction is invalid.', 1;

    SELECT
        [AuditIngestionId],
        [PublicId],
        [MessageId],
        [EventType],
        [AggregateType],
        [AggregateId],
        [AggregatePublicId],
        [AggregateVersion],
        [CorrelationId],
        [SourcePriority],
        [SourceOccurredAtUtc],
        [SourcePublishedAtUtc],
        [ConsumerName],
        [Status],
        [AttemptCount],
        [FirstReceivedAtUtc],
        [LastAttemptAtUtc],
        [ProcessedAtUtc],
        [DeadLetteredAtUtc],
        [LastErrorCode],
        [LastErrorMessage],
        [LastErrorClass],
        [CreatedAtUtc],
        [UpdatedAtUtc]
    FROM [audit].[AuditIngestion]
    WHERE
        [Status] IN ('Failed', 'DeadLettered')
        AND (@FromFirstReceivedAtUtc IS NULL OR [FirstReceivedAtUtc] >= @FromFirstReceivedAtUtc)
        AND (@ToFirstReceivedAtUtc IS NULL OR [FirstReceivedAtUtc] <= @ToFirstReceivedAtUtc)
        AND (@EventType IS NULL OR [EventType] = @EventType)
        AND (@AggregateType IS NULL OR [AggregateType] = @AggregateType)
        AND (@AggregateId IS NULL OR [AggregateId] = @AggregateId)
        AND (@AggregatePublicId IS NULL OR [AggregatePublicId] = LTRIM(RTRIM(@AggregatePublicId)))
        AND (@CorrelationId IS NULL OR [CorrelationId] = @CorrelationId)
        AND (@ConsumerName IS NULL OR [ConsumerName] = @ConsumerName)
        AND (@LastErrorClass IS NULL OR [LastErrorClass] = @LastErrorClass)
    ORDER BY
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'SourceOccurredAtUtc' THEN [SourceOccurredAtUtc] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'SourcePublishedAtUtc' THEN [SourcePublishedAtUtc] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'FirstReceivedAtUtc' THEN [FirstReceivedAtUtc] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'LastAttemptAtUtc' THEN [LastAttemptAtUtc] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'CreatedAtUtc' THEN [CreatedAtUtc] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'UpdatedAtUtc' THEN [UpdatedAtUtc] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'AttemptCount' THEN [AttemptCount] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'EventType' THEN [EventType] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'ConsumerName' THEN [ConsumerName] END ASC,
        CASE WHEN @NormalizedSortDirection = 'asc' AND @NormalizedSortBy = N'LastErrorClass' THEN [LastErrorClass] END ASC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'SourceOccurredAtUtc' THEN [SourceOccurredAtUtc] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'SourcePublishedAtUtc' THEN [SourcePublishedAtUtc] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'FirstReceivedAtUtc' THEN [FirstReceivedAtUtc] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'LastAttemptAtUtc' THEN [LastAttemptAtUtc] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'CreatedAtUtc' THEN [CreatedAtUtc] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'UpdatedAtUtc' THEN [UpdatedAtUtc] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'AttemptCount' THEN [AttemptCount] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'EventType' THEN [EventType] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'ConsumerName' THEN [ConsumerName] END DESC,
        CASE WHEN @NormalizedSortDirection = 'desc' AND @NormalizedSortBy = N'LastErrorClass' THEN [LastErrorClass] END DESC,
        [LastAttemptAtUtc] DESC,
        [AuditIngestionId] DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditIngestion_GetFailedRecordCountWhereDynamic]
    @FromFirstReceivedAtUtc DATETIME2(3) = NULL,
    @ToFirstReceivedAtUtc   DATETIME2(3) = NULL,

    @EventType              NVARCHAR(200) = NULL,
    @AggregateType          NVARCHAR(100) = NULL,
    @AggregateId            NVARCHAR(100) = NULL,
    @AggregatePublicId      CHAR(26) = NULL,
    @CorrelationId          NVARCHAR(100) = NULL,
    @ConsumerName           NVARCHAR(150) = NULL,
    @LastErrorClass         VARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromFirstReceivedAtUtc IS NOT NULL
       AND @ToFirstReceivedAtUtc IS NOT NULL
       AND @FromFirstReceivedAtUtc > @ToFirstReceivedAtUtc
        THROW 56361, 'Audit ingestion time range is invalid.', 1;

    IF @AggregatePublicId IS NOT NULL
       AND NULLIF(LTRIM(RTRIM(@AggregatePublicId)), '') IS NULL
        SET @AggregatePublicId = NULL;

    IF @LastErrorClass IS NOT NULL
       AND @LastErrorClass NOT IN ('Transient', 'Permanent', 'Ambiguous', 'Validation', 'Policy', 'Redaction', 'Unknown')
        THROW 56360, 'Audit ingestion error class is invalid.', 1;

    SELECT COUNT_BIG(1) AS [RecordCount]
    FROM [audit].[AuditIngestion]
    WHERE
        [Status] IN ('Failed', 'DeadLettered')
        AND (@FromFirstReceivedAtUtc IS NULL OR [FirstReceivedAtUtc] >= @FromFirstReceivedAtUtc)
        AND (@ToFirstReceivedAtUtc IS NULL OR [FirstReceivedAtUtc] <= @ToFirstReceivedAtUtc)
        AND (@EventType IS NULL OR [EventType] = @EventType)
        AND (@AggregateType IS NULL OR [AggregateType] = @AggregateType)
        AND (@AggregateId IS NULL OR [AggregateId] = @AggregateId)
        AND (@AggregatePublicId IS NULL OR [AggregatePublicId] = LTRIM(RTRIM(@AggregatePublicId)))
        AND (@CorrelationId IS NULL OR [CorrelationId] = @CorrelationId)
        AND (@ConsumerName IS NULL OR [ConsumerName] = @ConsumerName)
        AND (@LastErrorClass IS NULL OR [LastErrorClass] = @LastErrorClass);
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditIngestion_CountFailedForDashboard]
    @FromFirstReceivedAtUtc DATETIME2(3) = NULL,
    @ToFirstReceivedAtUtc   DATETIME2(3) = NULL,
    @SourceModule           NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromFirstReceivedAtUtc IS NOT NULL
       AND @ToFirstReceivedAtUtc IS NOT NULL
       AND @FromFirstReceivedAtUtc > @ToFirstReceivedAtUtc
        THROW 56361, 'Audit ingestion time range is invalid.', 1;

    SELECT COUNT_BIG(1) AS [RecordCount]
    FROM [audit].[AuditIngestion]
    WHERE
        [Status] IN ('Failed', 'DeadLettered')
        AND (@FromFirstReceivedAtUtc IS NULL OR [FirstReceivedAtUtc] >= @FromFirstReceivedAtUtc)
        AND (@ToFirstReceivedAtUtc IS NULL OR [FirstReceivedAtUtc] <= @ToFirstReceivedAtUtc)
        AND
        (
            @SourceModule IS NULL
            OR (LOWER(@SourceModule) = N'authorization' AND [EventType] LIKE N'authorization.%')
            OR (LOWER(@SourceModule) = N'identity' AND [EventType] LIKE N'identity.%')
            OR (LOWER(@SourceModule) = N'content' AND [EventType] LIKE N'content.%')
            OR (LOWER(@SourceModule) = N'media' AND [EventType] LIKE N'media.%')
            OR (LOWER(@SourceModule) = N'interaction' AND [EventType] LIKE N'interaction.%')
            OR (LOWER(@SourceModule) = N'seo' AND [EventType] LIKE N'seo.%')
            OR (LOWER(@SourceModule) = N'notifications' AND [EventType] LIKE N'notifications.%')
            OR (LOWER(@SourceModule) = N'audit' AND [EventType] LIKE N'audit.%')
            OR (LOWER(@SourceModule) = N'system' AND [EventType] LIKE N'system.%')
        );
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditIngestion_CountDuplicateForDashboard]
    @FromFirstReceivedAtUtc DATETIME2(3) = NULL,
    @ToFirstReceivedAtUtc   DATETIME2(3) = NULL,
    @SourceModule           NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromFirstReceivedAtUtc IS NOT NULL
       AND @ToFirstReceivedAtUtc IS NOT NULL
       AND @FromFirstReceivedAtUtc > @ToFirstReceivedAtUtc
        THROW 56361, 'Audit ingestion time range is invalid.', 1;

    SELECT COUNT_BIG(1) AS [RecordCount]
    FROM [audit].[AuditIngestion]
    WHERE
        [Status] = 'Duplicate'
        AND (@FromFirstReceivedAtUtc IS NULL OR [FirstReceivedAtUtc] >= @FromFirstReceivedAtUtc)
        AND (@ToFirstReceivedAtUtc IS NULL OR [FirstReceivedAtUtc] <= @ToFirstReceivedAtUtc)
        AND
        (
            @SourceModule IS NULL
            OR (LOWER(@SourceModule) = N'authorization' AND [EventType] LIKE N'authorization.%')
            OR (LOWER(@SourceModule) = N'identity' AND [EventType] LIKE N'identity.%')
            OR (LOWER(@SourceModule) = N'content' AND [EventType] LIKE N'content.%')
            OR (LOWER(@SourceModule) = N'media' AND [EventType] LIKE N'media.%')
            OR (LOWER(@SourceModule) = N'interaction' AND [EventType] LIKE N'interaction.%')
            OR (LOWER(@SourceModule) = N'seo' AND [EventType] LIKE N'seo.%')
            OR (LOWER(@SourceModule) = N'notifications' AND [EventType] LIKE N'notifications.%')
            OR (LOWER(@SourceModule) = N'audit' AND [EventType] LIKE N'audit.%')
            OR (LOWER(@SourceModule) = N'system' AND [EventType] LIKE N'system.%')
        );
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditIngestion_GetOldestFailedIngestionAgeSeconds]
    @NowUtc DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;

    IF @NowUtc IS NULL
        THROW 56365, 'Audit ingestion nowUtc is required.', 1;

    DECLARE @OldestFailureAtUtc DATETIME2(3);

    SELECT @OldestFailureAtUtc = MIN(ISNULL([LastAttemptAtUtc], [FirstReceivedAtUtc]))
    FROM [audit].[AuditIngestion]
    WHERE [Status] IN ('Failed', 'DeadLettered');

    SELECT
        CASE
            WHEN @OldestFailureAtUtc IS NULL THEN NULL
            ELSE DATEDIFF(SECOND, @OldestFailureAtUtc, @NowUtc)
        END AS [OldestFailedIngestionAgeSeconds];
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditIngestion_GetRecordCount]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT_BIG(1) AS [RecordCount]
    FROM [audit].[AuditIngestion];
END;
GO
