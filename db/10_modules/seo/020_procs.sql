/*
  File: db/10_modules/seo/020_procs.sql
  Module: SEO
  Purpose:
  - Create stored procedures for SEO truth model in CommercialNews V1.
  - Support:
      * SeoMetadata reads/writes/upsert
      * SlugRegistry reads/writes/upsert
      * hot-path slug resolve
      * admin aggregate SEO read
      * idempotent Content-event apply without SeoConsumedMessage
      * version-aware stale-event protection
  - Idempotent: safe to re-run.

  Notes:
  - SEO uses ResourceType + ResourcePublicId, not internal Content ArticleId.
  - SEO owns routing truth and metadata truth.
  - SEO does NOT own publication visibility truth.
  - Reading/Public Query must still validate Content truth before serving.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 57201, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

USE [CommercialNews];
GO

IF SCHEMA_ID(N'seo') IS NULL
BEGIN
    THROW 57202, 'Schema [seo] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

IF OBJECT_ID(N'[seo].[SeoMetadata]', N'U') IS NULL
BEGIN
    THROW 57203, 'Table [seo].[SeoMetadata] does not exist. Run seo/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[seo].[SlugRegistry]', N'U') IS NULL
BEGIN
    THROW 57204, 'Table [seo].[SlugRegistry] does not exist. Run seo/001_tables.sql first.', 1;
END
GO

/* =========================================================
   SEO METADATA
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [seo].[Seo_SeoMetadata_Upsert]
    @Scope                  VARCHAR(30) = 'public',
    @ResourceType           VARCHAR(50),
    @ResourcePublicId       CHAR(26),
    @Slug                   NVARCHAR(200) = NULL,
    @CanonicalUrl           NVARCHAR(500) = NULL,
    @MetaTitle              NVARCHAR(300) = NULL,
    @MetaDescription        NVARCHAR(500) = NULL,
    @OgTitle                NVARCHAR(300) = NULL,
    @OgDescription          NVARCHAR(500) = NULL,
    @OgImageUrl             NVARCHAR(800) = NULL,
    @TwitterTitle           NVARCHAR(300) = NULL,
    @TwitterDescription     NVARCHAR(500) = NULL,
    @TwitterImageUrl        NVARCHAR(800) = NULL,
    @Robots                 NVARCHAR(100) = NULL,
    @IsManualOverride       BIT = 1,
    @UpdatedByUserId        BIGINT = NULL,
    @ExpectedVersion        INT = NULL,
    @AffectedRows           INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @AffectedRows = 0;

    IF LEN(LTRIM(RTRIM(ISNULL(@Scope, '')))) = 0
        THROW 57210, 'Scope is required.', 1;

    IF @Scope NOT IN ('public')
        THROW 57211, 'Scope is invalid.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@ResourceType, '')))) = 0
        THROW 57212, 'ResourceType is required.', 1;

    IF @ResourceType NOT IN ('Article')
        THROW 57213, 'ResourceType is invalid.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@ResourcePublicId, '')))) = 0
        THROW 57214, 'ResourcePublicId is required.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [seo].[SeoMetadata]
        WHERE [Scope] = @Scope
          AND [ResourceType] = @ResourceType
          AND [ResourcePublicId] = @ResourcePublicId
    )
    BEGIN
        UPDATE [seo].[SeoMetadata]
        SET
            [Slug] = @Slug,
            [CanonicalUrl] = @CanonicalUrl,
            [MetaTitle] = @MetaTitle,
            [MetaDescription] = @MetaDescription,
            [OgTitle] = @OgTitle,
            [OgDescription] = @OgDescription,
            [OgImageUrl] = @OgImageUrl,
            [TwitterTitle] = @TwitterTitle,
            [TwitterDescription] = @TwitterDescription,
            [TwitterImageUrl] = @TwitterImageUrl,
            [Robots] = @Robots,
            [IsManualOverride] = @IsManualOverride,
            [UpdatedAtUtc] = SYSUTCDATETIME(),
            [UpdatedByUserId] = @UpdatedByUserId,
            [Version] = [Version] + 1
        WHERE [Scope] = @Scope
          AND [ResourceType] = @ResourceType
          AND [ResourcePublicId] = @ResourcePublicId
          AND (@ExpectedVersion IS NULL OR [Version] = @ExpectedVersion);

        SET @AffectedRows = @@ROWCOUNT;

        IF @AffectedRows = 0
            RETURN;
    END
    ELSE
    BEGIN
        INSERT INTO [seo].[SeoMetadata]
        (
            [Scope],
            [ResourceType],
            [ResourcePublicId],
            [Slug],
            [CanonicalUrl],
            [MetaTitle],
            [MetaDescription],
            [OgTitle],
            [OgDescription],
            [OgImageUrl],
            [TwitterTitle],
            [TwitterDescription],
            [TwitterImageUrl],
            [Robots],
            [IsManualOverride],
            [UpdatedByUserId]
        )
        VALUES
        (
            @Scope,
            @ResourceType,
            @ResourcePublicId,
            @Slug,
            @CanonicalUrl,
            @MetaTitle,
            @MetaDescription,
            @OgTitle,
            @OgDescription,
            @OgImageUrl,
            @TwitterTitle,
            @TwitterDescription,
            @TwitterImageUrl,
            @Robots,
            @IsManualOverride,
            @UpdatedByUserId
        );

        SET @AffectedRows = 1;
    END

    SELECT TOP (1) *
    FROM [seo].[SeoMetadata]
    WHERE [Scope] = @Scope
      AND [ResourceType] = @ResourceType
      AND [ResourcePublicId] = @ResourcePublicId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [seo].[Seo_SeoMetadata_ApplyContentDefaults]
    @Scope                      VARCHAR(30) = 'public',
    @ResourceType               VARCHAR(50),
    @ResourcePublicId           CHAR(26),
    @Slug                       NVARCHAR(200) = NULL,
    @CanonicalUrl               NVARCHAR(500) = NULL,
    @MetaTitle                  NVARCHAR(300) = NULL,
    @MetaDescription            NVARCHAR(500) = NULL,
    @OgTitle                    NVARCHAR(300) = NULL,
    @OgDescription              NVARCHAR(500) = NULL,
    @OgImageUrl                 NVARCHAR(800) = NULL,
    @SourceAggregateVersion     BIGINT,
    @LastAppliedMessageId       CHAR(26),
    @LastSyncedAtUtc            DATETIME2(3) = NULL,
    @ApplyResult                VARCHAR(30) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @ApplyResult = 'NotApplied';

    IF @LastSyncedAtUtc IS NULL
        SET @LastSyncedAtUtc = SYSUTCDATETIME();

    IF LEN(LTRIM(RTRIM(ISNULL(@Scope, '')))) = 0
        THROW 57220, 'Scope is required.', 1;

    IF @Scope NOT IN ('public')
        THROW 57221, 'Scope is invalid.', 1;

    IF @ResourceType NOT IN ('Article')
        THROW 57222, 'ResourceType is invalid.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@ResourcePublicId, '')))) = 0
        THROW 57223, 'ResourcePublicId is required.', 1;

    IF @SourceAggregateVersion IS NULL OR @SourceAggregateVersion <= 0
        THROW 57224, 'SourceAggregateVersion must be > 0.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@LastAppliedMessageId, '')))) = 0
        THROW 57225, 'LastAppliedMessageId is required.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [seo].[SeoMetadata]
        WHERE [Scope] = @Scope
          AND [ResourceType] = @ResourceType
          AND [ResourcePublicId] = @ResourcePublicId
    )
    BEGIN
        UPDATE [seo].[SeoMetadata]
        SET
            [Slug] = COALESCE(@Slug, [Slug]),
            [CanonicalUrl] = CASE WHEN [IsManualOverride] = 1 THEN [CanonicalUrl] ELSE @CanonicalUrl END,
            [MetaTitle] = CASE WHEN [IsManualOverride] = 1 THEN [MetaTitle] ELSE @MetaTitle END,
            [MetaDescription] = CASE WHEN [IsManualOverride] = 1 THEN [MetaDescription] ELSE @MetaDescription END,
            [OgTitle] = CASE WHEN [IsManualOverride] = 1 THEN [OgTitle] ELSE @OgTitle END,
            [OgDescription] = CASE WHEN [IsManualOverride] = 1 THEN [OgDescription] ELSE @OgDescription END,
            [OgImageUrl] = CASE WHEN [IsManualOverride] = 1 THEN [OgImageUrl] ELSE @OgImageUrl END,
            [SourceAggregateVersion] = @SourceAggregateVersion,
            [LastAppliedMessageId] = @LastAppliedMessageId,
            [LastSyncedAtUtc] = @LastSyncedAtUtc,
            [UpdatedAtUtc] = SYSUTCDATETIME(),
            [Version] = [Version] + 1
        WHERE [Scope] = @Scope
          AND [ResourceType] = @ResourceType
          AND [ResourcePublicId] = @ResourcePublicId
          AND
          (
              [SourceAggregateVersion] IS NULL
              OR @SourceAggregateVersion > [SourceAggregateVersion]
          );

        IF @@ROWCOUNT = 0
        BEGIN
            SET @ApplyResult = 'StaleIgnored';
            RETURN;
        END

        SET @ApplyResult = 'Applied';
    END
    ELSE
    BEGIN
        INSERT INTO [seo].[SeoMetadata]
        (
            [Scope],
            [ResourceType],
            [ResourcePublicId],
            [Slug],
            [CanonicalUrl],
            [MetaTitle],
            [MetaDescription],
            [OgTitle],
            [OgDescription],
            [OgImageUrl],
            [IsManualOverride],
            [SourceAggregateVersion],
            [LastAppliedMessageId],
            [LastSyncedAtUtc]
        )
        VALUES
        (
            @Scope,
            @ResourceType,
            @ResourcePublicId,
            @Slug,
            @CanonicalUrl,
            @MetaTitle,
            @MetaDescription,
            @OgTitle,
            @OgDescription,
            @OgImageUrl,
            0,
            @SourceAggregateVersion,
            @LastAppliedMessageId,
            @LastSyncedAtUtc
        );

        SET @ApplyResult = 'Applied';
    END

    SELECT TOP (1) *
    FROM [seo].[SeoMetadata]
    WHERE [Scope] = @Scope
      AND [ResourceType] = @ResourceType
      AND [ResourcePublicId] = @ResourcePublicId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [seo].[Seo_SeoMetadata_SelectById]
    @SeoId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1) *
    FROM [seo].[SeoMetadata]
    WHERE [SeoId] = @SeoId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [seo].[Seo_SeoMetadata_SelectByResource]
    @Scope              VARCHAR(30) = 'public',
    @ResourceType       VARCHAR(50),
    @ResourcePublicId   CHAR(26)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1) *
    FROM [seo].[SeoMetadata]
    WHERE [Scope] = @Scope
      AND [ResourceType] = @ResourceType
      AND [ResourcePublicId] = @ResourcePublicId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [seo].[Seo_SeoMetadata_SelectSkipAndTake]
    @Skip               INT = 0,
    @Take               INT = 20,
    @Scope              VARCHAR(30) = NULL,
    @ResourceType       VARCHAR(50) = NULL,
    @ResourcePublicId   CHAR(26) = NULL,
    @IsManualOverride   BIT = NULL,
    @UpdatedByUserId    BIGINT = NULL,
    @Keyword            NVARCHAR(300) = NULL,
    @SortBy             NVARCHAR(30) = N'UpdatedAtUtc',
    @SortDirection      NVARCHAR(4) = N'DESC'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;
    IF @Take > 200 SET @Take = 200;

    IF @SortBy NOT IN (N'UpdatedAtUtc', N'ResourcePublicId', N'SeoId', N'Version')
        SET @SortBy = N'UpdatedAtUtc';

    IF UPPER(@SortDirection) NOT IN (N'ASC', N'DESC')
        SET @SortDirection = N'DESC';

    ;WITH [Filtered] AS
    (
        SELECT
            [s].[SeoId],
            [s].[Scope],
            [s].[ResourceType],
            [s].[ResourcePublicId],
            [s].[Slug],
            [s].[CanonicalUrl],
            [s].[MetaTitle],
            [s].[MetaDescription],
            [s].[OgTitle],
            [s].[OgDescription],
            [s].[OgImageUrl],
            [s].[TwitterTitle],
            [s].[TwitterDescription],
            [s].[TwitterImageUrl],
            [s].[Robots],
            [s].[IsManualOverride],
            [s].[SourceAggregateVersion],
            [s].[LastAppliedMessageId],
            [s].[LastSyncedAtUtc],
            [s].[Version],
            [s].[CreatedAtUtc],
            [s].[UpdatedAtUtc],
            [s].[UpdatedByUserId],
            COUNT(1) OVER() AS [TotalCount]
        FROM [seo].[SeoMetadata] AS [s]
        WHERE (@Scope IS NULL OR [s].[Scope] = @Scope)
          AND (@ResourceType IS NULL OR [s].[ResourceType] = @ResourceType)
          AND (@ResourcePublicId IS NULL OR [s].[ResourcePublicId] = @ResourcePublicId)
          AND (@IsManualOverride IS NULL OR [s].[IsManualOverride] = @IsManualOverride)
          AND (@UpdatedByUserId IS NULL OR [s].[UpdatedByUserId] = @UpdatedByUserId)
          AND
          (
              @Keyword IS NULL
              OR [s].[Slug] LIKE N'%' + @Keyword + N'%'
              OR [s].[CanonicalUrl] LIKE N'%' + @Keyword + N'%'
              OR [s].[MetaTitle] LIKE N'%' + @Keyword + N'%'
              OR [s].[MetaDescription] LIKE N'%' + @Keyword + N'%'
              OR [s].[OgTitle] LIKE N'%' + @Keyword + N'%'
          )
    )
    SELECT *
    FROM [Filtered]
    ORDER BY
        CASE WHEN @SortBy = N'UpdatedAtUtc' AND @SortDirection = N'ASC'  THEN [UpdatedAtUtc] END ASC,
        CASE WHEN @SortBy = N'UpdatedAtUtc' AND @SortDirection = N'DESC' THEN [UpdatedAtUtc] END DESC,
        CASE WHEN @SortBy = N'ResourcePublicId' AND @SortDirection = N'ASC'  THEN [ResourcePublicId] END ASC,
        CASE WHEN @SortBy = N'ResourcePublicId' AND @SortDirection = N'DESC' THEN [ResourcePublicId] END DESC,
        CASE WHEN @SortBy = N'SeoId' AND @SortDirection = N'ASC' THEN [SeoId] END ASC,
        CASE WHEN @SortBy = N'SeoId' AND @SortDirection = N'DESC' THEN [SeoId] END DESC,
        CASE WHEN @SortBy = N'Version' AND @SortDirection = N'ASC' THEN [Version] END ASC,
        CASE WHEN @SortBy = N'Version' AND @SortDirection = N'DESC' THEN [Version] END DESC,
        [SeoId] DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END
GO

/* =========================================================
   SLUG REGISTRY
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [seo].[Seo_SlugRegistry_Upsert]
    @Scope                  VARCHAR(30) = 'public',
    @Slug                   NVARCHAR(200),
    @ResourceType           VARCHAR(50),
    @ResourcePublicId       CHAR(26),
    @CanonicalUrl           NVARCHAR(500) = NULL,
    @IsIndexable            BIT = 0,
    @IsActive               BIT = 1,
    @ActorUserId            BIGINT = NULL,
    @ExpectedVersion        INT = NULL,
    @AffectedRows           INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @AffectedRows = 0;

    IF LEN(LTRIM(RTRIM(ISNULL(@Scope, '')))) = 0
        THROW 57240, 'Scope is required.', 1;

    IF @Scope NOT IN ('public')
        THROW 57241, 'Scope is invalid.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Slug, N'')))) = 0
        THROW 57242, 'Slug is required.', 1;

    IF @ResourceType NOT IN ('Article')
        THROW 57243, 'ResourceType is invalid.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@ResourcePublicId, '')))) = 0
        THROW 57244, 'ResourcePublicId is required.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [seo].[SlugRegistry]
        WHERE [Scope] = @Scope
          AND [ResourceType] = @ResourceType
          AND [ResourcePublicId] = @ResourcePublicId
    )
    BEGIN
        UPDATE [seo].[SlugRegistry]
        SET
            [Slug] = @Slug,
            [CanonicalUrl] = @CanonicalUrl,
            [IsIndexable] = @IsIndexable,
            [IsActive] = @IsActive,
            [UpdatedAtUtc] = SYSUTCDATETIME(),
            [UpdatedByUserId] = @ActorUserId,
            [Version] = [Version] + 1
        WHERE [Scope] = @Scope
          AND [ResourceType] = @ResourceType
          AND [ResourcePublicId] = @ResourcePublicId
          AND (@ExpectedVersion IS NULL OR [Version] = @ExpectedVersion);

        SET @AffectedRows = @@ROWCOUNT;

        IF @AffectedRows = 0
            RETURN;
    END
    ELSE
    BEGIN
        INSERT INTO [seo].[SlugRegistry]
        (
            [Scope],
            [Slug],
            [ResourceType],
            [ResourcePublicId],
            [CanonicalUrl],
            [IsIndexable],
            [IsActive],
            [CreatedByUserId],
            [UpdatedByUserId]
        )
        VALUES
        (
            @Scope,
            @Slug,
            @ResourceType,
            @ResourcePublicId,
            @CanonicalUrl,
            @IsIndexable,
            @IsActive,
            @ActorUserId,
            @ActorUserId
        );

        SET @AffectedRows = 1;
    END

    SELECT TOP (1) *
    FROM [seo].[SlugRegistry]
    WHERE [Scope] = @Scope
      AND [ResourceType] = @ResourceType
      AND [ResourcePublicId] = @ResourcePublicId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [seo].[Seo_SlugRegistry_ApplyContentVisibility]
    @Scope                      VARCHAR(30) = 'public',
    @Slug                       NVARCHAR(200) = NULL,
    @ResourceType               VARCHAR(50),
    @ResourcePublicId           CHAR(26),
    @CanonicalUrl               NVARCHAR(500) = NULL,
    @IsIndexable                BIT,
    @IsActive                   BIT,
    @SourceAggregateVersion     BIGINT,
    @LastAppliedMessageId       CHAR(26),
    @LastSyncedAtUtc            DATETIME2(3) = NULL,
    @ApplyResult                VARCHAR(30) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @ApplyResult = 'NotApplied';

    IF @LastSyncedAtUtc IS NULL
        SET @LastSyncedAtUtc = SYSUTCDATETIME();

    IF @Scope NOT IN ('public')
        THROW 57250, 'Scope is invalid.', 1;

    IF @ResourceType NOT IN ('Article')
        THROW 57251, 'ResourceType is invalid.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@ResourcePublicId, '')))) = 0
        THROW 57252, 'ResourcePublicId is required.', 1;

    IF @SourceAggregateVersion IS NULL OR @SourceAggregateVersion <= 0
        THROW 57253, 'SourceAggregateVersion must be > 0.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@LastAppliedMessageId, '')))) = 0
        THROW 57254, 'LastAppliedMessageId is required.', 1;

    IF @IsActive = 1 AND LEN(LTRIM(RTRIM(ISNULL(@Slug, N'')))) = 0
        THROW 57255, 'Slug is required when activating a route.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [seo].[SlugRegistry]
        WHERE [Scope] = @Scope
          AND [ResourceType] = @ResourceType
          AND [ResourcePublicId] = @ResourcePublicId
    )
    BEGIN
        UPDATE [seo].[SlugRegistry]
        SET
            [Slug] = CASE WHEN @Slug IS NULL THEN [Slug] ELSE @Slug END,
            [CanonicalUrl] = @CanonicalUrl,
            [IsIndexable] = @IsIndexable,
            [IsActive] = @IsActive,
            [SourceAggregateVersion] = @SourceAggregateVersion,
            [LastAppliedMessageId] = @LastAppliedMessageId,
            [LastSyncedAtUtc] = @LastSyncedAtUtc,
            [UpdatedAtUtc] = SYSUTCDATETIME(),
            [Version] = [Version] + 1
        WHERE [Scope] = @Scope
          AND [ResourceType] = @ResourceType
          AND [ResourcePublicId] = @ResourcePublicId
          AND
          (
              [SourceAggregateVersion] IS NULL
              OR @SourceAggregateVersion > [SourceAggregateVersion]
          );

        IF @@ROWCOUNT = 0
        BEGIN
            SET @ApplyResult = 'StaleIgnored';
            RETURN;
        END

        SET @ApplyResult = 'Applied';
    END
    ELSE
    BEGIN
        IF @IsActive = 0
        BEGIN
            SET @ApplyResult = 'NoRouteToDeactivate';
            RETURN;
        END

        INSERT INTO [seo].[SlugRegistry]
        (
            [Scope],
            [Slug],
            [ResourceType],
            [ResourcePublicId],
            [CanonicalUrl],
            [IsIndexable],
            [IsActive],
            [SourceAggregateVersion],
            [LastAppliedMessageId],
            [LastSyncedAtUtc]
        )
        VALUES
        (
            @Scope,
            @Slug,
            @ResourceType,
            @ResourcePublicId,
            @CanonicalUrl,
            @IsIndexable,
            @IsActive,
            @SourceAggregateVersion,
            @LastAppliedMessageId,
            @LastSyncedAtUtc
        );

        SET @ApplyResult = 'Applied';
    END

    SELECT TOP (1) *
    FROM [seo].[SlugRegistry]
    WHERE [Scope] = @Scope
      AND [ResourceType] = @ResourceType
      AND [ResourcePublicId] = @ResourcePublicId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [seo].[Seo_SlugRegistry_DeactivateByResource]
    @Scope                  VARCHAR(30) = 'public',
    @ResourceType           VARCHAR(50),
    @ResourcePublicId       CHAR(26),
    @ActorUserId            BIGINT = NULL,
    @ExpectedVersion        INT = NULL,
    @AffectedRows           INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @AffectedRows = 0;

    UPDATE [seo].[SlugRegistry]
    SET
        [IsActive] = 0,
        [IsIndexable] = 0,
        [UpdatedAtUtc] = SYSUTCDATETIME(),
        [UpdatedByUserId] = @ActorUserId,
        [Version] = [Version] + 1
    WHERE [Scope] = @Scope
      AND [ResourceType] = @ResourceType
      AND [ResourcePublicId] = @ResourcePublicId
      AND [IsActive] = 1
      AND (@ExpectedVersion IS NULL OR [Version] = @ExpectedVersion);

    SET @AffectedRows = @@ROWCOUNT;

    IF @AffectedRows = 0
        RETURN;

    SELECT TOP (1) *
    FROM [seo].[SlugRegistry]
    WHERE [Scope] = @Scope
      AND [ResourceType] = @ResourceType
      AND [ResourcePublicId] = @ResourcePublicId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [seo].[Seo_SlugRegistry_SelectById]
    @SlugId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1) *
    FROM [seo].[SlugRegistry]
    WHERE [SlugId] = @SlugId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [seo].[Seo_SlugRegistry_SelectByResource]
    @Scope              VARCHAR(30) = 'public',
    @ResourceType       VARCHAR(50),
    @ResourcePublicId   CHAR(26),
    @OnlyActive         BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM [seo].[SlugRegistry]
    WHERE [Scope] = @Scope
      AND [ResourceType] = @ResourceType
      AND [ResourcePublicId] = @ResourcePublicId
      AND (@OnlyActive IS NULL OR [IsActive] = @OnlyActive)
    ORDER BY [IsActive] DESC, [UpdatedAtUtc] DESC, [SlugId] DESC;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [seo].[Seo_SlugRegistry_SelectByScopeAndSlug]
    @Scope      VARCHAR(30),
    @Slug       NVARCHAR(200),
    @OnlyActive BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1) *
    FROM [seo].[SlugRegistry]
    WHERE [Scope] = @Scope
      AND [Slug] = @Slug
      AND (@OnlyActive IS NULL OR [IsActive] = @OnlyActive)
    ORDER BY [IsActive] DESC, [SlugId] DESC;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [seo].[Seo_SlugRegistry_SelectSkipAndTake]
    @Skip               INT = 0,
    @Take               INT = 20,
    @Scope              VARCHAR(30) = NULL,
    @ResourceType       VARCHAR(50) = NULL,
    @ResourcePublicId   CHAR(26) = NULL,
    @IsActive           BIT = NULL,
    @IsIndexable        BIT = NULL,
    @Keyword            NVARCHAR(200) = NULL,
    @SortBy             NVARCHAR(30) = N'UpdatedAtUtc',
    @SortDirection      NVARCHAR(4) = N'DESC'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;
    IF @Take > 200 SET @Take = 200;

    IF @SortBy NOT IN (N'UpdatedAtUtc', N'CreatedAtUtc', N'Slug', N'ResourcePublicId', N'Version')
        SET @SortBy = N'UpdatedAtUtc';

    IF UPPER(@SortDirection) NOT IN (N'ASC', N'DESC')
        SET @SortDirection = N'DESC';

    ;WITH [Filtered] AS
    (
        SELECT
            [s].[SlugId],
            [s].[Scope],
            [s].[Slug],
            [s].[ResourceType],
            [s].[ResourcePublicId],
            [s].[CanonicalUrl],
            [s].[IsIndexable],
            [s].[IsActive],
            [s].[SourceAggregateVersion],
            [s].[LastAppliedMessageId],
            [s].[LastSyncedAtUtc],
            [s].[Version],
            [s].[CreatedAtUtc],
            [s].[CreatedByUserId],
            [s].[UpdatedAtUtc],
            [s].[UpdatedByUserId],
            COUNT(1) OVER() AS [TotalCount]
        FROM [seo].[SlugRegistry] AS [s]
        WHERE (@Scope IS NULL OR [s].[Scope] = @Scope)
          AND (@ResourceType IS NULL OR [s].[ResourceType] = @ResourceType)
          AND (@ResourcePublicId IS NULL OR [s].[ResourcePublicId] = @ResourcePublicId)
          AND (@IsActive IS NULL OR [s].[IsActive] = @IsActive)
          AND (@IsIndexable IS NULL OR [s].[IsIndexable] = @IsIndexable)
          AND
          (
              @Keyword IS NULL
              OR [s].[Slug] LIKE N'%' + @Keyword + N'%'
              OR [s].[CanonicalUrl] LIKE N'%' + @Keyword + N'%'
          )
    )
    SELECT *
    FROM [Filtered]
    ORDER BY
        CASE WHEN @SortBy = N'UpdatedAtUtc' AND @SortDirection = N'ASC'  THEN [UpdatedAtUtc] END ASC,
        CASE WHEN @SortBy = N'UpdatedAtUtc' AND @SortDirection = N'DESC' THEN [UpdatedAtUtc] END DESC,
        CASE WHEN @SortBy = N'CreatedAtUtc' AND @SortDirection = N'ASC'  THEN [CreatedAtUtc] END ASC,
        CASE WHEN @SortBy = N'CreatedAtUtc' AND @SortDirection = N'DESC' THEN [CreatedAtUtc] END DESC,
        CASE WHEN @SortBy = N'Slug' AND @SortDirection = N'ASC' THEN [Slug] END ASC,
        CASE WHEN @SortBy = N'Slug' AND @SortDirection = N'DESC' THEN [Slug] END DESC,
        CASE WHEN @SortBy = N'ResourcePublicId' AND @SortDirection = N'ASC' THEN [ResourcePublicId] END ASC,
        CASE WHEN @SortBy = N'ResourcePublicId' AND @SortDirection = N'DESC' THEN [ResourcePublicId] END DESC,
        CASE WHEN @SortBy = N'Version' AND @SortDirection = N'ASC' THEN [Version] END ASC,
        CASE WHEN @SortBy = N'Version' AND @SortDirection = N'DESC' THEN [Version] END DESC,
        [SlugId] DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END
GO

/* =========================================================
   PUBLIC / AGGREGATE READS
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [seo].[Seo_ResolveByScopeAndSlug]
    @Scope VARCHAR(30),
    @Slug  NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@Scope, '')))) = 0
        THROW 57280, 'Scope is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Slug, N'')))) = 0
        THROW 57281, 'Slug is required.', 1;

    SELECT TOP (1)
        [s].[Scope],
        [s].[Slug],
        [s].[ResourceType],
        [s].[ResourcePublicId],
        [s].[CanonicalUrl],
        [s].[IsIndexable],
        CASE
            WHEN [s].[IsActive] = 1 THEN N'Resolved'
            ELSE N'Inactive'
        END AS [Status],
        [s].[SourceAggregateVersion],
        [s].[Version]
    FROM [seo].[SlugRegistry] AS [s]
    WHERE [s].[Scope] = @Scope
      AND [s].[Slug] = @Slug
      AND [s].[IsActive] = 1
    ORDER BY [s].[SlugId] DESC;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [seo].[Seo_SelectMetadataByResource]
    @Scope              VARCHAR(30) = 'public',
    @ResourceType       VARCHAR(50),
    @ResourcePublicId   CHAR(26)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        COALESCE([sm].[Scope], [sr].[Scope]) AS [Scope],
        COALESCE([sm].[ResourceType], [sr].[ResourceType], @ResourceType) AS [ResourceType],
        COALESCE([sm].[ResourcePublicId], [sr].[ResourcePublicId], @ResourcePublicId) AS [ResourcePublicId],
        COALESCE([sm].[Slug], [sr].[Slug]) AS [Slug],
        COALESCE([sm].[CanonicalUrl], [sr].[CanonicalUrl]) AS [CanonicalUrl],
        [sm].[MetaTitle],
        [sm].[MetaDescription],
        [sm].[OgTitle],
        [sm].[OgDescription],
        [sm].[OgImageUrl],
        [sm].[TwitterTitle],
        [sm].[TwitterDescription],
        [sm].[TwitterImageUrl],
        [sm].[Robots],
        [sm].[IsManualOverride],
        COALESCE([sm].[SourceAggregateVersion], [sr].[SourceAggregateVersion]) AS [SourceAggregateVersion],
        COALESCE([sm].[LastAppliedMessageId], [sr].[LastAppliedMessageId]) AS [LastAppliedMessageId],
        COALESCE([sm].[LastSyncedAtUtc], [sr].[LastSyncedAtUtc]) AS [LastSyncedAtUtc],
        CASE
            WHEN [sm].[Version] IS NULL AND [sr].[Version] IS NULL THEN 0
            WHEN [sm].[Version] IS NULL THEN [sr].[Version]
            WHEN [sr].[Version] IS NULL THEN [sm].[Version]
            WHEN [sm].[Version] >= [sr].[Version] THEN [sm].[Version]
            ELSE [sr].[Version]
        END AS [Version]
    FROM
    (
        SELECT
            @Scope AS [Scope],
            @ResourceType AS [ResourceType],
            @ResourcePublicId AS [ResourcePublicId]
    ) AS [r]
    LEFT JOIN [seo].[SeoMetadata] AS [sm]
        ON [sm].[Scope] = [r].[Scope]
       AND [sm].[ResourceType] = [r].[ResourceType]
       AND [sm].[ResourcePublicId] = [r].[ResourcePublicId]
    LEFT JOIN [seo].[SlugRegistry] AS [sr]
        ON [sr].[Scope] = [r].[Scope]
       AND [sr].[ResourceType] = [r].[ResourceType]
       AND [sr].[ResourcePublicId] = [r].[ResourcePublicId]
       AND [sr].[IsActive] = 1
    WHERE [sm].[SeoId] IS NOT NULL
       OR [sr].[SlugId] IS NOT NULL;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [seo].[Seo_SelectArticleSeoByArticlePublicId]
    @ArticlePublicId CHAR(26),
    @Scope           VARCHAR(30) = 'public'
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        COALESCE([sm].[Scope], [sr].[Scope]) AS [Scope],
        COALESCE([sm].[ResourceType], [sr].[ResourceType], 'Article') AS [ResourceType],
        COALESCE([sm].[ResourcePublicId], [sr].[ResourcePublicId], @ArticlePublicId) AS [ResourcePublicId],
        COALESCE([sm].[Slug], [sr].[Slug]) AS [Slug],
        COALESCE([sm].[CanonicalUrl], [sr].[CanonicalUrl]) AS [CanonicalUrl],

        [sm].[MetaTitle],
        [sm].[MetaDescription],
        [sm].[OgTitle],
        [sm].[OgDescription],
        [sm].[OgImageUrl],
        [sm].[TwitterTitle],
        [sm].[TwitterDescription],
        [sm].[TwitterImageUrl],
        [sm].[Robots],
        [sm].[IsManualOverride],

        [sr].[IsIndexable],
        [sr].[IsActive],

        COALESCE([sm].[SourceAggregateVersion], [sr].[SourceAggregateVersion]) AS [SourceAggregateVersion],
        COALESCE([sm].[LastAppliedMessageId], [sr].[LastAppliedMessageId]) AS [LastAppliedMessageId],
        COALESCE([sm].[LastSyncedAtUtc], [sr].[LastSyncedAtUtc]) AS [LastSyncedAtUtc],

        CASE
            WHEN [sm].[Version] IS NULL AND [sr].[Version] IS NULL THEN 0
            WHEN [sm].[Version] IS NULL THEN [sr].[Version]
            WHEN [sr].[Version] IS NULL THEN [sm].[Version]
            WHEN [sm].[Version] >= [sr].[Version] THEN [sm].[Version]
            ELSE [sr].[Version]
        END AS [Version]
    FROM
    (
        SELECT
            @Scope AS [Scope],
            'Article' AS [ResourceType],
            @ArticlePublicId AS [ResourcePublicId]
    ) AS [r]
    LEFT JOIN [seo].[SeoMetadata] AS [sm]
        ON [sm].[Scope] = [r].[Scope]
       AND [sm].[ResourceType] = [r].[ResourceType]
       AND [sm].[ResourcePublicId] = [r].[ResourcePublicId]
    LEFT JOIN [seo].[SlugRegistry] AS [sr]
        ON [sr].[Scope] = [r].[Scope]
       AND [sr].[ResourceType] = [r].[ResourceType]
       AND [sr].[ResourcePublicId] = [r].[ResourcePublicId]
    WHERE [sm].[SeoId] IS NOT NULL
       OR [sr].[SlugId] IS NOT NULL;
END
GO