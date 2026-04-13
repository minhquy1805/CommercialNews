/*
  File: db/10_modules/audit/020_procs.sql
  Module: Audit
  Purpose:
  - Create stored procedures for Audit V1.
  - Append-only canonical audit evidence operations for:
      * AuditLog idempotent insert
      * AuditLog reads and investigation queries
      * AuditLog paging and record counts
  - Idempotent: uses CREATE OR ALTER PROCEDURE

  Notes:
  - Audit is append-only: no UPDATE/DELETE procedures in V1.
  - Insert is idempotent on [AuditEventId].
  - Investigation queries are bounded and ordered newest-first by default.
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

/* =========================================================
   AuditLog
   ========================================================= */

CREATE OR ALTER PROCEDURE [audit].[AuditLog_Insert]
    @AuditEventId      UNIQUEIDENTIFIER,
    @ActorUserId       BIGINT = NULL,
    @Action            NVARCHAR(120),
    @ResourceType      NVARCHAR(60),
    @ResourceId        NVARCHAR(100),
    @Outcome           NVARCHAR(30) = NULL,
    @Summary           NVARCHAR(300),
    @Reason            NVARCHAR(500) = NULL,
    @OccurredAt        DATETIME2(3),
    @CorrelationId     NVARCHAR(100) = NULL,
    @IpAddress         NVARCHAR(45) = NULL,
    @UserAgent         NVARCHAR(300) = NULL,
    @OldValuesJson     NVARCHAR(MAX) = NULL,
    @NewValuesJson     NVARCHAR(MAX) = NULL,
    @MetadataJson      NVARCHAR(MAX) = NULL,
    @AuditId           BIGINT OUTPUT,
    @WasInserted       BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @AuditEventId IS NULL
        THROW 56310, 'Audit event id is required.', 1;

    IF NULLIF(LTRIM(RTRIM(@Action)), N'') IS NULL
        THROW 56311, 'Audit action is required.', 1;

    IF NULLIF(LTRIM(RTRIM(@ResourceType)), N'') IS NULL
        THROW 56312, 'Audit resource type is required.', 1;

    IF NULLIF(LTRIM(RTRIM(@ResourceId)), N'') IS NULL
        THROW 56313, 'Audit resource id is required.', 1;

    IF NULLIF(LTRIM(RTRIM(@Summary)), N'') IS NULL
        THROW 56314, 'Audit summary is required.', 1;

    IF @Outcome IS NOT NULL
       AND @Outcome NOT IN (N'Success', N'Failure')
        THROW 56315, 'Audit outcome must be Success, Failure, or NULL.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [audit].[AuditLog]
        WHERE [AuditEventId] = @AuditEventId
    )
    BEGIN
        SELECT @AuditId = [AuditId]
        FROM [audit].[AuditLog]
        WHERE [AuditEventId] = @AuditEventId;

        SET @WasInserted = 0;
        RETURN;
    END

    INSERT INTO [audit].[AuditLog]
    (
        [AuditEventId],
        [ActorUserId],
        [Action],
        [ResourceType],
        [ResourceId],
        [Outcome],
        [Summary],
        [Reason],
        [OccurredAt],
        [CorrelationId],
        [IpAddress],
        [UserAgent],
        [OldValuesJson],
        [NewValuesJson],
        [MetadataJson]
    )
    VALUES
    (
        @AuditEventId,
        @ActorUserId,
        @Action,
        @ResourceType,
        @ResourceId,
        @Outcome,
        @Summary,
        @Reason,
        @OccurredAt,
        @CorrelationId,
        @IpAddress,
        @UserAgent,
        @OldValuesJson,
        @NewValuesJson,
        @MetadataJson
    );

    SET @AuditId = SCOPE_IDENTITY();
    SET @WasInserted = 1;
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditLog_SelectById]
    @AuditId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [AuditId],
        [AuditEventId],
        [ActorUserId],
        [Action],
        [ResourceType],
        [ResourceId],
        [Outcome],
        [Summary],
        [Reason],
        [OccurredAt],
        [CorrelationId],
        [IpAddress],
        [UserAgent],
        [OldValuesJson],
        [NewValuesJson],
        [MetadataJson]
    FROM [audit].[AuditLog]
    WHERE [AuditId] = @AuditId;
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditLog_SelectByAuditEventId]
    @AuditEventId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [AuditId],
        [AuditEventId],
        [ActorUserId],
        [Action],
        [ResourceType],
        [ResourceId],
        [Outcome],
        [Summary],
        [Reason],
        [OccurredAt],
        [CorrelationId],
        [IpAddress],
        [UserAgent],
        [OldValuesJson],
        [NewValuesJson],
        [MetadataJson]
    FROM [audit].[AuditLog]
    WHERE [AuditEventId] = @AuditEventId;
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditLog_SelectAll]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [AuditId],
        [AuditEventId],
        [ActorUserId],
        [Action],
        [ResourceType],
        [ResourceId],
        [Outcome],
        [Summary],
        [Reason],
        [OccurredAt],
        [CorrelationId],
        [IpAddress],
        [UserAgent],
        [OldValuesJson],
        [NewValuesJson],
        [MetadataJson]
    FROM [audit].[AuditLog]
    ORDER BY [OccurredAt] DESC, [AuditId] DESC;
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

    SELECT
        [AuditId],
        [AuditEventId],
        [ActorUserId],
        [Action],
        [ResourceType],
        [ResourceId],
        [Outcome],
        [Summary],
        [Reason],
        [OccurredAt],
        [CorrelationId],
        [IpAddress],
        [UserAgent],
        [OldValuesJson],
        [NewValuesJson],
        [MetadataJson]
    FROM [audit].[AuditLog]
    ORDER BY [OccurredAt] DESC, [AuditId] DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE [audit].[AuditLog_SelectSkipAndTakeWhereDynamic]
    @FromOccurredAt     DATETIME2(3) = NULL,
    @ToOccurredAt       DATETIME2(3) = NULL,
    @ActorUserId        BIGINT = NULL,
    @Action             NVARCHAR(120) = NULL,
    @ResourceType       NVARCHAR(60) = NULL,
    @ResourceId         NVARCHAR(100) = NULL,
    @CorrelationId      NVARCHAR(100) = NULL,
    @AuditEventId       UNIQUEIDENTIFIER = NULL,
    @Outcome            NVARCHAR(30) = NULL,
    @Skip               INT = 0,
    @Take               INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;

    IF @FromOccurredAt IS NOT NULL
       AND @ToOccurredAt IS NOT NULL
       AND @FromOccurredAt > @ToOccurredAt
        THROW 56316, 'Audit time range is invalid.', 1;

    SELECT
        [AuditId],
        [AuditEventId],
        [ActorUserId],
        [Action],
        [ResourceType],
        [ResourceId],
        [Outcome],
        [Summary],
        [Reason],
        [OccurredAt],
        [CorrelationId],
        [IpAddress],
        [UserAgent],
        [OldValuesJson],
        [NewValuesJson],
        [MetadataJson]
    FROM [audit].[AuditLog]
    WHERE
        (@FromOccurredAt IS NULL OR [OccurredAt] >= @FromOccurredAt)
        AND (@ToOccurredAt IS NULL OR [OccurredAt] <= @ToOccurredAt)
        AND (@ActorUserId IS NULL OR [ActorUserId] = @ActorUserId)
        AND (@Action IS NULL OR [Action] = @Action)
        AND (@ResourceType IS NULL OR [ResourceType] = @ResourceType)
        AND (@ResourceId IS NULL OR [ResourceId] = @ResourceId)
        AND (@CorrelationId IS NULL OR [CorrelationId] = @CorrelationId)
        AND (@AuditEventId IS NULL OR [AuditEventId] = @AuditEventId)
        AND (@Outcome IS NULL OR [Outcome] = @Outcome)
    ORDER BY [OccurredAt] DESC, [AuditId] DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
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
        THROW 56317, 'Correlation id is required.', 1;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;

    SELECT
        [AuditId],
        [AuditEventId],
        [ActorUserId],
        [Action],
        [ResourceType],
        [ResourceId],
        [Outcome],
        [Summary],
        [Reason],
        [OccurredAt],
        [CorrelationId],
        [IpAddress],
        [UserAgent],
        [OldValuesJson],
        [NewValuesJson],
        [MetadataJson]
    FROM [audit].[AuditLog]
    WHERE [CorrelationId] = @CorrelationId
    ORDER BY [OccurredAt] DESC, [AuditId] DESC
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
    @FromOccurredAt     DATETIME2(3) = NULL,
    @ToOccurredAt       DATETIME2(3) = NULL,
    @ActorUserId        BIGINT = NULL,
    @Action             NVARCHAR(120) = NULL,
    @ResourceType       NVARCHAR(60) = NULL,
    @ResourceId         NVARCHAR(100) = NULL,
    @CorrelationId      NVARCHAR(100) = NULL,
    @AuditEventId       UNIQUEIDENTIFIER = NULL,
    @Outcome            NVARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromOccurredAt IS NOT NULL
       AND @ToOccurredAt IS NOT NULL
       AND @FromOccurredAt > @ToOccurredAt
        THROW 56316, 'Audit time range is invalid.', 1;

    SELECT COUNT_BIG(1) AS [RecordCount]
    FROM [audit].[AuditLog]
    WHERE
        (@FromOccurredAt IS NULL OR [OccurredAt] >= @FromOccurredAt)
        AND (@ToOccurredAt IS NULL OR [OccurredAt] <= @ToOccurredAt)
        AND (@ActorUserId IS NULL OR [ActorUserId] = @ActorUserId)
        AND (@Action IS NULL OR [Action] = @Action)
        AND (@ResourceType IS NULL OR [ResourceType] = @ResourceType)
        AND (@ResourceId IS NULL OR [ResourceId] = @ResourceId)
        AND (@CorrelationId IS NULL OR [CorrelationId] = @CorrelationId)
        AND (@AuditEventId IS NULL OR [AuditEventId] = @AuditEventId)
        AND (@Outcome IS NULL OR [Outcome] = @Outcome);
END;
GO