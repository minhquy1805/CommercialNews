/*
  File: db/10_modules/reading/020_procs.sql
  Module: Reading
  Purpose:
  - Create stored procedures for Reading V1 derived public read projection.
  - Reading owns public serving projections, not source truth.
  - Support:
      * public article detail by public id
      * public article detail by slug
      * public article listing with paging/filter/sort
      * public article search
      * deterministic related articles
      * async projection apply from Content events
      * projection updates from SEO / Media / Identity / Interaction events

  Notes:
  - Public read path starts from [reading].[ArticleReadModel] and composes
    local projections such as [reading].[ArticleInteractionCounterProjection].
  - Source truth remains in Content / SEO / Media / Identity / Interaction.
  - Projection apply must be idempotent and version-aware.
  - SourceVersion prevents stale overwrite for source-versioned projection lanes.
  - InteractionStatsVersion guards Interaction counter snapshots.
  - MessageId supports duplicate tracing / dedupe.
  - This file is idempotent: safe to re-run.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 58201, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

USE [CommercialNews];
GO

IF SCHEMA_ID(N'reading') IS NULL
BEGIN
    THROW 58202, 'Schema [reading] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

IF OBJECT_ID(N'[reading].[ArticleReadModel]', N'U') IS NULL
BEGIN
    THROW 58203, 'Table [reading].[ArticleReadModel] does not exist. Run reading/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[reading].[ArticleReadModelTag]', N'U') IS NULL
BEGIN
    THROW 58204, 'Table [reading].[ArticleReadModelTag] does not exist. Run reading/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[reading].[ArticleReadModelMedia]', N'U') IS NULL
BEGIN
    THROW 58205, 'Table [reading].[ArticleReadModelMedia] does not exist. Run reading/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[reading].[ArticleMediaProjectionState]', N'U') IS NULL
BEGIN
    THROW 58206, 'Table [reading].[ArticleMediaProjectionState] does not exist. Run reading/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[reading].[ArticleSeoRouteProjection]', N'U') IS NULL
BEGIN
    THROW 58207, 'Table [reading].[ArticleSeoRouteProjection] does not exist. Run reading/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[reading].[ArticleSeoMetadataProjection]', N'U') IS NULL
BEGIN
    THROW 58208, 'Table [reading].[ArticleSeoMetadataProjection] does not exist. Run reading/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[reading].[AuthorProfileProjection]', N'U') IS NULL
BEGIN
    THROW 58209, 'Table [reading].[AuthorProfileProjection] does not exist. Run reading/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[reading].[ArticleInteractionCounterProjection]', N'U') IS NULL
BEGIN
    THROW 58200, 'Table [reading].[ArticleInteractionCounterProjection] does not exist. Run reading/001_tables.sql first.', 1;
END
GO

/* =========================================================
   1) PUBLIC ARTICLE DETAIL BY PUBLIC ID
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [reading].[Reading_ArticleReadModel_SelectByPublicId]
    @ArticlePublicId CHAR(26)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ArticlePublicId IS NULL OR LEN(@ArticlePublicId) <> 26
        THROW 58210, 'ArticlePublicId must be a valid 26-character public id.', 1;

    /* Result set 1: detail core */
    SELECT TOP (1)
        [a].[ArticleId],
        [a].[ArticlePublicId],
        [a].[Slug],
        [a].[Title],
        [a].[Summary],
        [a].[Body],

        [a].[CategoryId],
        [a].[CategoryName],

        [a].[AuthorUserId],
        [a].[AuthorDisplayName],

        [a].[CoverMediaId],
        [a].[CoverMediaUrl],
        [a].[CoverAlt],

        [a].[CanonicalUrl],
        [a].[MetaTitle],
        [a].[MetaDescription],
        [a].[OgTitle],
        [a].[OgDescription],
        [a].[OgImageUrl],
        [a].[TwitterTitle],
        [a].[TwitterDescription],
        [a].[TwitterImageUrl],
        [a].[Robots],
        [a].[SeoIsManualOverride],
        [a].[SeoRouteIsActive],
        [a].[SeoIsIndexable],

        [a].[PublishedAtUtc],
        [a].[UpdatedAtUtc],

        COALESCE([i].[ViewCount], 0) AS [ViewCount],
        COALESCE([i].[LikeCount], 0) AS [LikeCount],
        COALESCE([i].[VisibleCommentCount], 0) AS [VisibleCommentCount],
        CASE
            WHEN [i].[ArticlePublicId] IS NULL THEN CAST(1 AS BIT)
            ELSE CAST(0 AS BIT)
        END AS [CountersPartial]
    FROM [reading].[ArticleReadModel] AS [a]
    LEFT JOIN [reading].[ArticleInteractionCounterProjection] AS [i]
        ON [i].[ArticlePublicId] = [a].[ArticlePublicId]
    WHERE [a].[ArticlePublicId] = @ArticlePublicId
      AND [a].[IsPublic] = 1
      AND [a].[Status] = N'Published';

    /* Result set 2: tags */
    SELECT
        [t].[TagId],
        [t].[TagPublicId],
        [t].[Name],
        [t].[Slug]
    FROM [reading].[ArticleReadModelTag] AS [t]
    INNER JOIN [reading].[ArticleReadModel] AS [a]
        ON [a].[ArticleId] = [t].[ArticleId]
       AND [a].[IsPublic] = 1
       AND [a].[Status] = N'Published'
    WHERE [a].[ArticlePublicId] = @ArticlePublicId
    ORDER BY [t].[Name] ASC, [t].[TagId] ASC;

    /* Result set 3: media */
    SELECT
        [m].[MediaId],
        [m].[MediaPublicId],
        [m].[Url],
        [m].[Alt],
        [m].[Caption],
        [m].[MediaType],
        [m].[IsPrimary],
        [m].[SortOrder]
    FROM [reading].[ArticleReadModelMedia] AS [m]
    INNER JOIN [reading].[ArticleReadModel] AS [a]
        ON [a].[ArticleId] = [m].[ArticleId]
       AND [a].[IsPublic] = 1
       AND [a].[Status] = N'Published'
    WHERE [a].[ArticlePublicId] = @ArticlePublicId
    ORDER BY [m].[IsPrimary] DESC, [m].[SortOrder] ASC, [m].[MediaId] ASC;
END
GO

/* =========================================================
   2) PUBLIC ARTICLE DETAIL BY SLUG
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [reading].[Reading_ArticleReadModel_SelectBySlug]
    @Slug NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@Slug, N'')))) = 0
        THROW 58220, 'Slug is required.', 1;

    SET @Slug = LTRIM(RTRIM(@Slug));

    DECLARE @ArticlePublicId CHAR(26);
    DECLARE @ActiveMatchCount INT;

    SELECT
        @ActiveMatchCount = COUNT(1),
        @ArticlePublicId = MAX([ResourcePublicId])
    FROM [reading].[ArticleSeoRouteProjection]
    WHERE [Slug] = @Slug
      AND [Scope] = 'public'
      AND [ResourceType] = 'Article'
      AND [IsActive] = 1;

    /*
      Reading is a derived projection.
      Zero matches or multiple active matches are treated as unsafe.
      SEO truth remains the canonical route authority.
    */
    IF @ActiveMatchCount <> 1
    BEGIN
        /* Empty result set 1: detail core */
        SELECT
            CAST(NULL AS BIGINT) AS [ArticleId],
            CAST(NULL AS CHAR(26)) AS [ArticlePublicId],
            CAST(NULL AS NVARCHAR(200)) AS [Slug],
            CAST(NULL AS NVARCHAR(300)) AS [Title],
            CAST(NULL AS NVARCHAR(1000)) AS [Summary],
            CAST(NULL AS NVARCHAR(MAX)) AS [Body],

            CAST(NULL AS BIGINT) AS [CategoryId],
            CAST(NULL AS NVARCHAR(200)) AS [CategoryName],

            CAST(NULL AS BIGINT) AS [AuthorUserId],
            CAST(NULL AS NVARCHAR(200)) AS [AuthorDisplayName],

            CAST(NULL AS BIGINT) AS [CoverMediaId],
            CAST(NULL AS NVARCHAR(1000)) AS [CoverMediaUrl],
            CAST(NULL AS NVARCHAR(300)) AS [CoverAlt],

            CAST(NULL AS NVARCHAR(500)) AS [CanonicalUrl],
            CAST(NULL AS NVARCHAR(300)) AS [MetaTitle],
            CAST(NULL AS NVARCHAR(500)) AS [MetaDescription],
            CAST(NULL AS NVARCHAR(300)) AS [OgTitle],
            CAST(NULL AS NVARCHAR(500)) AS [OgDescription],
            CAST(NULL AS NVARCHAR(800)) AS [OgImageUrl],
            CAST(NULL AS NVARCHAR(300)) AS [TwitterTitle],
            CAST(NULL AS NVARCHAR(500)) AS [TwitterDescription],
            CAST(NULL AS NVARCHAR(800)) AS [TwitterImageUrl],
            CAST(NULL AS NVARCHAR(100)) AS [Robots],
            CAST(NULL AS BIT) AS [SeoIsManualOverride],
            CAST(NULL AS BIT) AS [SeoRouteIsActive],
            CAST(NULL AS BIT) AS [SeoIsIndexable],

            CAST(NULL AS DATETIME2(3)) AS [PublishedAtUtc],
            CAST(NULL AS DATETIME2(3)) AS [UpdatedAtUtc],

            CAST(NULL AS BIGINT) AS [ViewCount],
            CAST(NULL AS BIGINT) AS [LikeCount],
            CAST(NULL AS BIGINT) AS [VisibleCommentCount]
        WHERE 1 = 0;

        /* Empty result set 2: tags */
        SELECT
            CAST(NULL AS BIGINT) AS [TagId],
            CAST(NULL AS CHAR(26)) AS [TagPublicId],
            CAST(NULL AS NVARCHAR(150)) AS [Name],
            CAST(NULL AS NVARCHAR(200)) AS [Slug]
        WHERE 1 = 0;

        /* Empty result set 3: media */
        SELECT
            CAST(NULL AS BIGINT) AS [MediaId],
            CAST(NULL AS CHAR(26)) AS [MediaPublicId],
            CAST(NULL AS NVARCHAR(1000)) AS [Url],
            CAST(NULL AS NVARCHAR(300)) AS [Alt],
            CAST(NULL AS NVARCHAR(300)) AS [Caption],
            CAST(NULL AS NVARCHAR(50)) AS [MediaType],
            CAST(NULL AS BIT) AS [IsPrimary],
            CAST(NULL AS INT) AS [SortOrder]
        WHERE 1 = 0;

        RETURN;
    END

    EXEC [reading].[Reading_ArticleReadModel_SelectByPublicId]
        @ArticlePublicId = @ArticlePublicId;
END
GO

/* =========================================================
   3) PUBLIC ARTICLE LIST
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [reading].[Reading_ArticleReadModel_SelectSkipAndTake]
    @Skip               INT = 0,
    @Take               INT = 20,
    @CategoryId         BIGINT = NULL,
    @TagId              BIGINT = NULL,
    @Keyword            NVARCHAR(300) = NULL,
    @SortBy             NVARCHAR(30) = N'PublishedAt',
    @SortDirection      NVARCHAR(4) = N'DESC'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;
    IF @Take > 200 SET @Take = 200;

    IF @SortBy <> N'PublishedAt'
        SET @SortBy = N'PublishedAt';

    IF UPPER(@SortDirection) NOT IN (N'ASC', N'DESC')
        SET @SortDirection = N'DESC';

    SET @Keyword = NULLIF(LTRIM(RTRIM(@Keyword)), N'');

    ;WITH [Filtered] AS
    (
        SELECT
            [a].[ArticleId],
            [a].[ArticlePublicId],
            [a].[Slug],
            [a].[Title],
            [a].[Summary],

            [a].[CategoryId],
            [a].[CategoryName],

            [a].[AuthorUserId],
            [a].[AuthorDisplayName],

            [a].[CoverMediaId],
            [a].[CoverMediaUrl],
            [a].[CoverAlt],

            [a].[PublishedAtUtc],
            [a].[UpdatedAtUtc],

            COALESCE([i].[ViewCount], 0) AS [ViewCount],
            COALESCE([i].[LikeCount], 0) AS [LikeCount],
            COALESCE([i].[VisibleCommentCount], 0) AS [VisibleCommentCount],
            CASE
                WHEN [i].[ArticlePublicId] IS NULL THEN CAST(1 AS BIT)
                ELSE CAST(0 AS BIT)
            END AS [CountersPartial],

            COUNT(1) OVER() AS [TotalCount]
        FROM [reading].[ArticleReadModel] AS [a]
        LEFT JOIN [reading].[ArticleInteractionCounterProjection] AS [i]
            ON [i].[ArticlePublicId] = [a].[ArticlePublicId]
        WHERE [a].[IsPublic] = 1
          AND [a].[Status] = N'Published'
          AND (@CategoryId IS NULL OR [a].[CategoryId] = @CategoryId)
          AND
          (
              @TagId IS NULL
              OR EXISTS
              (
                  SELECT 1
                  FROM [reading].[ArticleReadModelTag] AS [t]
                  WHERE [t].[ArticleId] = [a].[ArticleId]
                    AND [t].[TagId] = @TagId
              )
          )
          AND
          (
              @Keyword IS NULL
              OR [a].[Title] LIKE N'%' + @Keyword + N'%'
              OR [a].[Summary] LIKE N'%' + @Keyword + N'%'
              OR [a].[SearchText] LIKE N'%' + @Keyword + N'%'
          )
    )
    SELECT
        [ArticleId],
        [ArticlePublicId],
        [Slug],
        [Title],
        [Summary],

        [CategoryId],
        [CategoryName],

        [AuthorUserId],
        [AuthorDisplayName],

        [CoverMediaId],
        [CoverMediaUrl],
        [CoverAlt],

        [PublishedAtUtc],
        [UpdatedAtUtc],

        [ViewCount],
        [LikeCount],
        [VisibleCommentCount],
        [CountersPartial],

        [TotalCount]
    FROM [Filtered]
    ORDER BY
        CASE WHEN UPPER(@SortDirection) = N'ASC'  THEN [PublishedAtUtc] END ASC,
        CASE WHEN UPPER(@SortDirection) = N'DESC' THEN [PublishedAtUtc] END DESC,
        [ArticleId] DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END
GO

/* =========================================================
   4) PUBLIC SEARCH
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [reading].[Reading_ArticleReadModel_Search]
    @Keyword            NVARCHAR(300),
    @Skip               INT = 0,
    @Take               INT = 20,
    @SortBy             NVARCHAR(30) = N'PublishedAt',
    @SortDirection      NVARCHAR(4) = N'DESC'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@Keyword, N'')))) = 0
        THROW 58230, 'Keyword is required.', 1;

    EXEC [reading].[Reading_ArticleReadModel_SelectSkipAndTake]
        @Skip = @Skip,
        @Take = @Take,
        @CategoryId = NULL,
        @TagId = NULL,
        @Keyword = @Keyword,
        @SortBy = @SortBy,
        @SortDirection = @SortDirection;
END
GO

/* =========================================================
   5) PUBLIC RELATED ARTICLES
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [reading].[Reading_ArticleReadModel_SelectRelated]
    @ArticlePublicId CHAR(26),
    @Limit           INT = 6
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ArticlePublicId IS NULL OR LEN(@ArticlePublicId) <> 26
        THROW 58240, 'ArticlePublicId must be a valid 26-character public id.', 1;

    IF @Limit <= 0 SET @Limit = 6;
    IF @Limit > 50 SET @Limit = 50;

    DECLARE @ArticleId BIGINT;
    DECLARE @CategoryId BIGINT;

    SELECT TOP (1)
        @ArticleId = [ArticleId],
        @CategoryId = [CategoryId]
    FROM [reading].[ArticleReadModel]
    WHERE [ArticlePublicId] = @ArticlePublicId
      AND [IsPublic] = 1
      AND [Status] = N'Published';

    IF @ArticleId IS NULL
    BEGIN
        SELECT
            CAST(NULL AS BIGINT) AS [ArticleId],
            CAST(NULL AS CHAR(26)) AS [ArticlePublicId],
            CAST(NULL AS NVARCHAR(200)) AS [Slug],
            CAST(NULL AS NVARCHAR(300)) AS [Title],
            CAST(NULL AS NVARCHAR(1000)) AS [Summary],

            CAST(NULL AS BIGINT) AS [CategoryId],
            CAST(NULL AS NVARCHAR(200)) AS [CategoryName],

            CAST(NULL AS BIGINT) AS [AuthorUserId],
            CAST(NULL AS NVARCHAR(200)) AS [AuthorDisplayName],

            CAST(NULL AS BIGINT) AS [CoverMediaId],
            CAST(NULL AS NVARCHAR(1000)) AS [CoverMediaUrl],
            CAST(NULL AS NVARCHAR(300)) AS [CoverAlt],

            CAST(NULL AS DATETIME2(3)) AS [PublishedAtUtc],
            CAST(NULL AS DATETIME2(3)) AS [UpdatedAtUtc],

            CAST(NULL AS BIGINT) AS [ViewCount],
            CAST(NULL AS BIGINT) AS [LikeCount],
            CAST(NULL AS BIGINT) AS [VisibleCommentCount],
            CAST(NULL AS BIT) AS [CountersPartial]
        WHERE 1 = 0;

        RETURN;
    END

    ;WITH [Candidate] AS
    (
        SELECT
            [a].[ArticleId],
            [a].[ArticlePublicId],
            [a].[Slug],
            [a].[Title],
            [a].[Summary],

            [a].[CategoryId],
            [a].[CategoryName],

            [a].[AuthorUserId],
            [a].[AuthorDisplayName],

            [a].[CoverMediaId],
            [a].[CoverMediaUrl],
            [a].[CoverAlt],

            [a].[PublishedAtUtc],
            [a].[UpdatedAtUtc],

            COALESCE([i].[ViewCount], 0) AS [ViewCount],
            COALESCE([i].[LikeCount], 0) AS [LikeCount],
            COALESCE([i].[VisibleCommentCount], 0) AS [VisibleCommentCount],
            CASE
                WHEN [i].[ArticlePublicId] IS NULL THEN CAST(1 AS BIT)
                ELSE CAST(0 AS BIT)
            END AS [CountersPartial],

            CASE
                WHEN @CategoryId IS NOT NULL
                 AND [a].[CategoryId] = @CategoryId THEN 1

                WHEN EXISTS
                (
                    SELECT 1
                    FROM [reading].[ArticleReadModelTag] AS [candidateTag]
                    WHERE [candidateTag].[ArticleId] = [a].[ArticleId]
                      AND EXISTS
                      (
                          SELECT 1
                          FROM [reading].[ArticleReadModelTag] AS [currentTag]
                          WHERE [currentTag].[ArticleId] = @ArticleId
                            AND [currentTag].[TagId] = [candidateTag].[TagId]
                      )
                ) THEN 2

                ELSE 3
            END AS [MatchRank]
        FROM [reading].[ArticleReadModel] AS [a]
        LEFT JOIN [reading].[ArticleInteractionCounterProjection] AS [i]
            ON [i].[ArticlePublicId] = [a].[ArticlePublicId]
        WHERE [a].[ArticleId] <> @ArticleId
          AND [a].[IsPublic] = 1
          AND [a].[Status] = N'Published'
    )
    SELECT TOP (@Limit)
        [ArticleId],
        [ArticlePublicId],
        [Slug],
        [Title],
        [Summary],

        [CategoryId],
        [CategoryName],

        [AuthorUserId],
        [AuthorDisplayName],

        [CoverMediaId],
        [CoverMediaUrl],
        [CoverAlt],

        [PublishedAtUtc],
        [UpdatedAtUtc],

        [ViewCount],
        [LikeCount],
        [VisibleCommentCount],
        [CountersPartial]
    FROM [Candidate]
    ORDER BY
        [MatchRank] ASC,
        [PublishedAtUtc] DESC,
        [ArticleId] DESC;
END
GO

/* =========================================================
   6) PROJECTION APPLY FROM CONTENT
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [reading].[Reading_ArticleReadModel_UpsertFromContent]
    @ArticleId                 BIGINT,
    @ArticlePublicId           CHAR(26),

    @Title                     NVARCHAR(300),
    @Summary                   NVARCHAR(1000),
    @Body                      NVARCHAR(MAX),

    @CategoryId                BIGINT = NULL,
    @CategoryName              NVARCHAR(200) = NULL,

    @AuthorUserId              BIGINT = NULL,
    @AuthorDisplayName         NVARCHAR(200) = NULL,
    @CoverMediaId              BIGINT = NULL,

    @Status                    NVARCHAR(30),
    @IsPublic                  BIT,
    @PublishedAtUtc            DATETIME2(3) = NULL,
    @UpdatedAtUtc              DATETIME2(3),

    @SourceVersion             BIGINT,
    @MessageId                 CHAR(26) = NULL,
    @SourceOccurredAtUtc       DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 58250, 'ArticleId must be > 0.', 1;

    IF @ArticlePublicId IS NULL OR LEN(@ArticlePublicId) <> 26
        THROW 58251, 'ArticlePublicId must be a valid 26-character public id.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Title, N'')))) = 0
        THROW 58252, 'Title is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Summary, N'')))) = 0
        THROW 58253, 'Summary is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Body, N'')))) = 0
        THROW 58254, 'Body is required.', 1;

    IF @Status NOT IN (N'Draft', N'Published', N'Archived')
        THROW 58255, 'Status is invalid.', 1;

    IF @SourceVersion IS NULL OR @SourceVersion <= 0
        THROW 58256, 'SourceVersion must be > 0.', 1;

    IF @CoverMediaId IS NOT NULL AND @CoverMediaId <= 0
        THROW 58257, 'CoverMediaId must be > 0 when provided.', 1;

    IF @AuthorUserId IS NOT NULL AND @AuthorUserId <= 0
        THROW 58258, 'AuthorUserId must be > 0 when provided.', 1;

    IF @UpdatedAtUtc IS NULL
        SET @UpdatedAtUtc = SYSUTCDATETIME();

    DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();

    DECLARE @Applied BIT = 0;
    DECLARE @CurrentSourceVersion BIGINT = NULL;
    DECLARE @CurrentPublishedAtUtc DATETIME2(3) = NULL;

    DECLARE @CurrentCoverMediaId BIGINT = NULL;
    DECLARE @CurrentCoverMediaUrl NVARCHAR(1000) = NULL;
    DECLARE @CurrentCoverAlt NVARCHAR(300) = NULL;

    DECLARE @EffectivePublishedAtUtc DATETIME2(3) = NULL;
    DECLARE @EffectiveIsPublic BIT = 0;

    DECLARE @ResolvedCoverMediaId BIGINT = NULL;
    DECLARE @ResolvedCoverMediaUrl NVARCHAR(1000) = NULL;
    DECLARE @ResolvedCoverAlt NVARCHAR(300) = NULL;

    DECLARE @HasSeoRouteProjection BIT = 0;
    DECLARE @ProjectedSlug NVARCHAR(200) = NULL;
    DECLARE @ProjectedCanonicalUrl NVARCHAR(500) = NULL;
    DECLARE @ProjectedSeoRouteIsActive BIT = 0;
    DECLARE @ProjectedSeoIsIndexable BIT = 0;

    DECLARE @HasSeoMetadataProjection BIT = 0;
    DECLARE @ProjectedMetaTitle NVARCHAR(300) = NULL;
    DECLARE @ProjectedMetaDescription NVARCHAR(500) = NULL;
    DECLARE @ProjectedOgTitle NVARCHAR(300) = NULL;
    DECLARE @ProjectedOgDescription NVARCHAR(500) = NULL;
    DECLARE @ProjectedOgImageUrl NVARCHAR(800) = NULL;
    DECLARE @ProjectedTwitterTitle NVARCHAR(300) = NULL;
    DECLARE @ProjectedTwitterDescription NVARCHAR(500) = NULL;
    DECLARE @ProjectedTwitterImageUrl NVARCHAR(800) = NULL;
    DECLARE @ProjectedRobots NVARCHAR(100) = NULL;
    DECLARE @ProjectedSeoIsManualOverride BIT = 0;

    DECLARE @ProjectedAuthorDisplayName NVARCHAR(200) = NULL;

    BEGIN TRANSACTION;

    SELECT
        @CurrentSourceVersion = [SourceVersion],
        @CurrentPublishedAtUtc = [PublishedAtUtc],
        @CurrentCoverMediaId = [CoverMediaId],
        @CurrentCoverMediaUrl = [CoverMediaUrl],
        @CurrentCoverAlt = [CoverAlt]
    FROM [reading].[ArticleReadModel] WITH (UPDLOCK, HOLDLOCK)
    WHERE [ArticleId] = @ArticleId;

    SELECT TOP (1)
        @HasSeoRouteProjection = 1,
        @ProjectedSlug = [Slug],
        @ProjectedCanonicalUrl = [CanonicalUrl],
        @ProjectedSeoRouteIsActive = [IsActive],
        @ProjectedSeoIsIndexable = [IsIndexable]
    FROM [reading].[ArticleSeoRouteProjection] WITH (UPDLOCK, HOLDLOCK)
    WHERE [Scope] = 'public'
      AND [ResourceType] = 'Article'
      AND [ResourcePublicId] = @ArticlePublicId
    ORDER BY [SourceVersion] DESC, [LastSyncedAtUtc] DESC;

    SELECT TOP (1)
        @HasSeoMetadataProjection = 1,
        @ProjectedMetaTitle = [MetaTitle],
        @ProjectedMetaDescription = [MetaDescription],
        @ProjectedOgTitle = [OgTitle],
        @ProjectedOgDescription = [OgDescription],
        @ProjectedOgImageUrl = [OgImageUrl],
        @ProjectedTwitterTitle = [TwitterTitle],
        @ProjectedTwitterDescription = [TwitterDescription],
        @ProjectedTwitterImageUrl = [TwitterImageUrl],
        @ProjectedRobots = [Robots],
        @ProjectedSeoIsManualOverride = [IsManualOverride]
    FROM [reading].[ArticleSeoMetadataProjection] WITH (UPDLOCK, HOLDLOCK)
    WHERE [Scope] = 'public'
      AND [ResourceType] = 'Article'
      AND [ResourcePublicId] = @ArticlePublicId
    ORDER BY [SourceVersion] DESC, [LastSyncedAtUtc] DESC;

    SET @ProjectedAuthorDisplayName = NULLIF(LTRIM(RTRIM(@AuthorDisplayName)), N'');

    IF @AuthorUserId IS NOT NULL
    BEGIN
        SELECT TOP (1)
            @ProjectedAuthorDisplayName = [AuthorDisplayName]
        FROM [reading].[AuthorProfileProjection] WITH (UPDLOCK, HOLDLOCK)
        WHERE [AuthorUserId] = @AuthorUserId;
    END

    SET @EffectivePublishedAtUtc =
        CASE
            WHEN @Status = N'Published'
                THEN COALESCE(@PublishedAtUtc, @CurrentPublishedAtUtc)
            ELSE NULL
        END;

    SET @EffectiveIsPublic =
        CASE
            WHEN @Status = N'Published'
             AND @IsPublic = 1
             AND @EffectivePublishedAtUtc IS NOT NULL THEN 1
            ELSE 0
        END;

    /*
      Cover ownership:
      - Media primary projection is authoritative for public cover rendering.
      - Content CoverMediaId is only a seed/fallback when no media primary is projected.
    */

    SELECT TOP (1)
        @ResolvedCoverMediaId = [MediaId],
        @ResolvedCoverMediaUrl = [Url],
        @ResolvedCoverAlt = [Alt]
    FROM [reading].[ArticleReadModelMedia] WITH (UPDLOCK, HOLDLOCK)
    WHERE [ArticleId] = @ArticleId
      AND [IsPrimary] = 1
    ORDER BY [SortOrder] ASC, [MediaId] ASC;

    IF @ResolvedCoverMediaId IS NULL AND @CoverMediaId IS NOT NULL
    BEGIN
        SELECT TOP (1)
            @ResolvedCoverMediaId = [MediaId],
            @ResolvedCoverMediaUrl = [Url],
            @ResolvedCoverAlt = [Alt]
        FROM [reading].[ArticleReadModelMedia] WITH (UPDLOCK, HOLDLOCK)
        WHERE [ArticleId] = @ArticleId
          AND [MediaId] = @CoverMediaId;
    END

    IF @ResolvedCoverMediaId IS NULL
    BEGIN
        IF @CurrentSourceVersion IS NOT NULL
        BEGIN
            SET @ResolvedCoverMediaId = @CurrentCoverMediaId;
            SET @ResolvedCoverMediaUrl = @CurrentCoverMediaUrl;
            SET @ResolvedCoverAlt = @CurrentCoverAlt;
        END
        ELSE
        BEGIN
            SET @ResolvedCoverMediaId = @CoverMediaId;
            SET @ResolvedCoverMediaUrl = NULL;
            SET @ResolvedCoverAlt = NULL;
        END
    END

    IF @CurrentSourceVersion IS NULL
    BEGIN
        INSERT INTO [reading].[ArticleReadModel]
        (
            [ArticleId],
            [ArticlePublicId],
            [Slug],
            [Title],
            [Summary],
            [Body],
            [CategoryId],
            [CategoryName],
            [AuthorUserId],
            [AuthorDisplayName],
            [CoverMediaId],
            [CoverMediaUrl],
            [CoverAlt],
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
            [SeoIsManualOverride],
            [SeoRouteIsActive],
            [SeoIsIndexable],
            [Status],
            [IsPublic],
            [PublishedAtUtc],
            [UpdatedAtUtc],
            [SearchText],
            [SourceVersion],
            [LastEventMessageId],
            [LastSourceOccurredAtUtc],
            [LastSyncedAtUtc],
            [CreatedAtUtc]
        )
        VALUES
        (
            @ArticleId,
            @ArticlePublicId,
            @ProjectedSlug,
            @Title,
            @Summary,
            @Body,
            @CategoryId,
            @CategoryName,
            @AuthorUserId,
            @ProjectedAuthorDisplayName,
            @ResolvedCoverMediaId,
            @ResolvedCoverMediaUrl,
            @ResolvedCoverAlt,
            @ProjectedCanonicalUrl,
            @ProjectedMetaTitle,
            @ProjectedMetaDescription,
            @ProjectedOgTitle,
            @ProjectedOgDescription,
            @ProjectedOgImageUrl,
            @ProjectedTwitterTitle,
            @ProjectedTwitterDescription,
            @ProjectedTwitterImageUrl,
            @ProjectedRobots,
            @ProjectedSeoIsManualOverride,
            @ProjectedSeoRouteIsActive,
            @ProjectedSeoIsIndexable,
            @Status,
            @EffectiveIsPublic,
            @EffectivePublishedAtUtc,
            @UpdatedAtUtc,
            CONCAT_WS(N' ', @Title, @Summary, @Body, @CategoryName, @ProjectedAuthorDisplayName),
            @SourceVersion,
            @MessageId,
            @SourceOccurredAtUtc,
            @Now,
            @Now
        );

        SET @Applied = 1;
    END
    ELSE IF @SourceVersion > @CurrentSourceVersion
    BEGIN
        UPDATE [reading].[ArticleReadModel]
        SET
            [ArticlePublicId] = @ArticlePublicId,
            [Title] = @Title,
            [Summary] = @Summary,
            [Body] = @Body,
            [CategoryId] = @CategoryId,
            [CategoryName] = @CategoryName,
            [AuthorUserId] = @AuthorUserId,
            [AuthorDisplayName] = @ProjectedAuthorDisplayName,

            [CoverMediaId] = @ResolvedCoverMediaId,
            [CoverMediaUrl] = @ResolvedCoverMediaUrl,
            [CoverAlt] = @ResolvedCoverAlt,

            [Slug] = CASE WHEN @HasSeoRouteProjection = 1 THEN @ProjectedSlug ELSE [Slug] END,
            [CanonicalUrl] = CASE WHEN @HasSeoRouteProjection = 1 THEN @ProjectedCanonicalUrl ELSE [CanonicalUrl] END,
            [SeoRouteIsActive] = CASE WHEN @HasSeoRouteProjection = 1 THEN @ProjectedSeoRouteIsActive ELSE [SeoRouteIsActive] END,
            [SeoIsIndexable] = CASE WHEN @HasSeoRouteProjection = 1 THEN @ProjectedSeoIsIndexable ELSE [SeoIsIndexable] END,

            [MetaTitle] = CASE WHEN @HasSeoMetadataProjection = 1 THEN @ProjectedMetaTitle ELSE [MetaTitle] END,
            [MetaDescription] = CASE WHEN @HasSeoMetadataProjection = 1 THEN @ProjectedMetaDescription ELSE [MetaDescription] END,
            [OgTitle] = CASE WHEN @HasSeoMetadataProjection = 1 THEN @ProjectedOgTitle ELSE [OgTitle] END,
            [OgDescription] = CASE WHEN @HasSeoMetadataProjection = 1 THEN @ProjectedOgDescription ELSE [OgDescription] END,
            [OgImageUrl] = CASE WHEN @HasSeoMetadataProjection = 1 THEN @ProjectedOgImageUrl ELSE [OgImageUrl] END,
            [TwitterTitle] = CASE WHEN @HasSeoMetadataProjection = 1 THEN @ProjectedTwitterTitle ELSE [TwitterTitle] END,
            [TwitterDescription] = CASE WHEN @HasSeoMetadataProjection = 1 THEN @ProjectedTwitterDescription ELSE [TwitterDescription] END,
            [TwitterImageUrl] = CASE WHEN @HasSeoMetadataProjection = 1 THEN @ProjectedTwitterImageUrl ELSE [TwitterImageUrl] END,
            [Robots] = CASE WHEN @HasSeoMetadataProjection = 1 THEN @ProjectedRobots ELSE [Robots] END,
            [SeoIsManualOverride] = CASE WHEN @HasSeoMetadataProjection = 1 THEN @ProjectedSeoIsManualOverride ELSE [SeoIsManualOverride] END,

            [Status] = @Status,
            [IsPublic] = @EffectiveIsPublic,
            [PublishedAtUtc] = @EffectivePublishedAtUtc,
            [UpdatedAtUtc] = @UpdatedAtUtc,
            [SearchText] = CONCAT_WS(N' ', @Title, @Summary, @Body, @CategoryName, @ProjectedAuthorDisplayName),

            [SourceVersion] = @SourceVersion,
            [LastEventMessageId] = @MessageId,
            [LastSourceOccurredAtUtc] = @SourceOccurredAtUtc,
            [LastSyncedAtUtc] = @Now
        WHERE [ArticleId] = @ArticleId
          AND [SourceVersion] < @SourceVersion;

        SET @Applied = CASE WHEN @@ROWCOUNT = 1 THEN 1 ELSE 0 END;
    END

    COMMIT TRANSACTION;

    SELECT
        @Applied AS [Applied],
        CASE
            WHEN @Applied = 1 THEN N'Applied'
            WHEN @CurrentSourceVersion IS NOT NULL
             AND @SourceVersion <= @CurrentSourceVersion THEN N'IgnoredStaleVersion'
            ELSE N'Ignored'
        END AS [Decision],
        @CurrentSourceVersion AS [PreviousSourceVersion],
        @SourceVersion AS [IncomingSourceVersion];
END
GO

/* =========================================================
   7) MARK ARTICLE NOT PUBLIC
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [reading].[Reading_ArticleReadModel_MarkNotPublic]
    @ArticleId                 BIGINT,
    @Status                    NVARCHAR(30),
    @SourceVersion             BIGINT,
    @MessageId                 CHAR(26) = NULL,
    @SourceOccurredAtUtc       DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 58260, 'ArticleId must be > 0.', 1;

    IF @Status NOT IN (N'Draft', N'Published', N'Archived')
        THROW 58261, 'Status is invalid.', 1;

    IF @SourceVersion IS NULL OR @SourceVersion <= 0
        THROW 58262, 'SourceVersion must be > 0.', 1;

    DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();

    UPDATE [reading].[ArticleReadModel]
    SET
        [Status] = @Status,
        [IsPublic] = 0,
        [SourceVersion] = @SourceVersion,
        [LastEventMessageId] = @MessageId,
        [LastSourceOccurredAtUtc] = @SourceOccurredAtUtc,
        [LastSyncedAtUtc] = @Now
    WHERE [ArticleId] = @ArticleId
      AND [SourceVersion] < @SourceVersion;

    SELECT
        CAST(CASE WHEN @@ROWCOUNT = 1 THEN 1 ELSE 0 END AS BIT) AS [Applied],
        CASE
            WHEN @@ROWCOUNT = 1 THEN N'Applied'
            ELSE N'IgnoredStaleOrMissing'
        END AS [Decision],
        @SourceVersion AS [IncomingSourceVersion];
END
GO

/* =========================================================
   8) SEO PROJECTION STATE + ARTICLE SEO FIELDS
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[reading].[Reading_ArticleReadModel_UpdateSeo]', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE [reading].[Reading_ArticleReadModel_UpdateSeo];
END
GO

CREATE OR ALTER PROCEDURE [reading].[Reading_ArticleSeoRouteProjection_ApplyFromSeo]
    @Scope                     VARCHAR(30) = 'public',
    @ResourceType              VARCHAR(50),
    @ResourcePublicId          CHAR(26),
    @Slug                      NVARCHAR(200),
    @CanonicalUrl              NVARCHAR(500) = NULL,
    @IsActive                  BIT,
    @IsIndexable               BIT,
    @SourceVersion             BIGINT,
    @MessageId                 CHAR(26) = NULL,
    @SourceOccurredAtUtc       DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@Scope, '')))) = 0
        THROW 58270, 'Scope is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@ResourceType, '')))) = 0
        THROW 58271, 'ResourceType is required.', 1;

    IF @Scope <> 'public'
        THROW 58275, 'Only public SEO scope is supported by Reading article route projection.', 1;

    IF @ResourceType <> 'Article'
        THROW 58276, 'Only Article SEO resource type is supported by Reading article route projection.', 1;

    IF @ResourcePublicId IS NULL OR LEN(@ResourcePublicId) <> 26
        THROW 58272, 'ResourcePublicId must be a valid 26-character public id.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Slug, N'')))) = 0
        THROW 58273, 'Slug is required.', 1;

    IF @SourceVersion IS NULL OR @SourceVersion <= 0
        THROW 58274, 'SourceVersion must be > 0.', 1;

    IF @IsActive IS NULL
        SET @IsActive = 0;

    IF @IsIndexable IS NULL
        SET @IsIndexable = 0;

    IF @IsActive = 0
        SET @IsIndexable = 0;

    DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();
    DECLARE @Applied BIT = 0;
    DECLARE @CurrentSourceVersion BIGINT = NULL;

    BEGIN TRANSACTION;

    SELECT
        @CurrentSourceVersion = [SourceVersion]
    FROM [reading].[ArticleSeoRouteProjection] WITH (UPDLOCK, HOLDLOCK)
    WHERE [Scope] = @Scope
      AND [ResourceType] = @ResourceType
      AND [ResourcePublicId] = @ResourcePublicId;

    IF @CurrentSourceVersion IS NULL
    BEGIN
        INSERT INTO [reading].[ArticleSeoRouteProjection]
        (
            [Scope],
            [ResourceType],
            [ResourcePublicId],
            [Slug],
            [CanonicalUrl],
            [IsActive],
            [IsIndexable],
            [SourceVersion],
            [LastEventMessageId],
            [LastSourceOccurredAtUtc],
            [LastSyncedAtUtc]
        )
        VALUES
        (
            @Scope,
            @ResourceType,
            @ResourcePublicId,
            LTRIM(RTRIM(@Slug)),
            @CanonicalUrl,
            @IsActive,
            @IsIndexable,
            @SourceVersion,
            @MessageId,
            @SourceOccurredAtUtc,
            @Now
        );

        SET @Applied = 1;
    END
    ELSE IF @SourceVersion > @CurrentSourceVersion
    BEGIN
        UPDATE [reading].[ArticleSeoRouteProjection]
        SET
            [Slug] = LTRIM(RTRIM(@Slug)),
            [CanonicalUrl] = @CanonicalUrl,
            [IsActive] = @IsActive,
            [IsIndexable] = @IsIndexable,
            [SourceVersion] = @SourceVersion,
            [LastEventMessageId] = @MessageId,
            [LastSourceOccurredAtUtc] = @SourceOccurredAtUtc,
            [LastSyncedAtUtc] = @Now
        WHERE [Scope] = @Scope
          AND [ResourceType] = @ResourceType
          AND [ResourcePublicId] = @ResourcePublicId
          AND [SourceVersion] < @SourceVersion;

        SET @Applied = CASE WHEN @@ROWCOUNT = 1 THEN 1 ELSE 0 END;
    END

    IF @Applied = 1
    BEGIN
        /*
        ArticleReadModel checkpoint fields belong to the Content projection stream.
        SEO route checkpoint is stored in ArticleSeoRouteProjection.
        */
        UPDATE [reading].[ArticleReadModel]
        SET
            [Slug] = LTRIM(RTRIM(@Slug)),
            [CanonicalUrl] = @CanonicalUrl,
            [SeoRouteIsActive] = @IsActive,
            [SeoIsIndexable] = @IsIndexable
        WHERE [ArticlePublicId] = @ResourcePublicId;
    END

    COMMIT TRANSACTION;

    SELECT
        @Applied AS [Applied],
        CASE
            WHEN @Applied = 1 THEN N'Applied'
            WHEN @SourceVersion <= @CurrentSourceVersion THEN N'IgnoredStaleVersion'
            ELSE N'Ignored'
        END AS [Decision],
        @CurrentSourceVersion AS [PreviousSourceVersion],
        @SourceVersion AS [IncomingSourceVersion];
END
GO

CREATE OR ALTER PROCEDURE [reading].[Reading_ArticleSeoMetadataProjection_ApplyFromSeo]
    @Scope                     VARCHAR(30) = 'public',
    @ResourceType              VARCHAR(50),
    @ResourcePublicId          CHAR(26),
    @MetaTitle                 NVARCHAR(300) = NULL,
    @MetaDescription           NVARCHAR(500) = NULL,
    @OgTitle                   NVARCHAR(300) = NULL,
    @OgDescription             NVARCHAR(500) = NULL,
    @OgImageUrl                NVARCHAR(800) = NULL,
    @TwitterTitle              NVARCHAR(300) = NULL,
    @TwitterDescription        NVARCHAR(500) = NULL,
    @TwitterImageUrl           NVARCHAR(800) = NULL,
    @Robots                    NVARCHAR(100) = NULL,
    @IsManualOverride          BIT,
    @SourceVersion             BIGINT,
    @MessageId                 CHAR(26) = NULL,
    @SourceOccurredAtUtc       DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@Scope, '')))) = 0
        THROW 58290, 'Scope is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@ResourceType, '')))) = 0
        THROW 58291, 'ResourceType is required.', 1;

    IF @Scope <> 'public'
        THROW 58292, 'Only public SEO scope is supported by Reading article metadata projection.', 1;

    IF @ResourceType <> 'Article'
        THROW 58293, 'Only Article SEO resource type is supported by Reading article metadata projection.', 1;

    IF @ResourcePublicId IS NULL OR LEN(@ResourcePublicId) <> 26
        THROW 58294, 'ResourcePublicId must be a valid 26-character public id.', 1;

    IF @SourceVersion IS NULL OR @SourceVersion <= 0
        THROW 58295, 'SourceVersion must be > 0.', 1;

    IF @IsManualOverride IS NULL
        SET @IsManualOverride = 0;

    DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();
    DECLARE @Applied BIT = 0;
    DECLARE @CurrentSourceVersion BIGINT = NULL;

    BEGIN TRANSACTION;

    SELECT
        @CurrentSourceVersion = [SourceVersion]
    FROM [reading].[ArticleSeoMetadataProjection] WITH (UPDLOCK, HOLDLOCK)
    WHERE [Scope] = @Scope
      AND [ResourceType] = @ResourceType
      AND [ResourcePublicId] = @ResourcePublicId;

    IF @CurrentSourceVersion IS NULL
    BEGIN
        INSERT INTO [reading].[ArticleSeoMetadataProjection]
        (
            [Scope],
            [ResourceType],
            [ResourcePublicId],
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
            [SourceVersion],
            [LastEventMessageId],
            [LastSourceOccurredAtUtc],
            [LastSyncedAtUtc]
        )
        VALUES
        (
            @Scope,
            @ResourceType,
            @ResourcePublicId,
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
            @SourceVersion,
            @MessageId,
            @SourceOccurredAtUtc,
            @Now
        );

        SET @Applied = 1;
    END
    ELSE IF @SourceVersion > @CurrentSourceVersion
    BEGIN
        UPDATE [reading].[ArticleSeoMetadataProjection]
        SET
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
            [SourceVersion] = @SourceVersion,
            [LastEventMessageId] = @MessageId,
            [LastSourceOccurredAtUtc] = @SourceOccurredAtUtc,
            [LastSyncedAtUtc] = @Now
        WHERE [Scope] = @Scope
          AND [ResourceType] = @ResourceType
          AND [ResourcePublicId] = @ResourcePublicId
          AND [SourceVersion] < @SourceVersion;

        SET @Applied = CASE WHEN @@ROWCOUNT = 1 THEN 1 ELSE 0 END;
    END

    IF @Applied = 1
    BEGIN
        /*
        ArticleReadModel checkpoint fields belong to the Content projection stream.
        SEO metadata checkpoint is stored in ArticleSeoMetadataProjection.
        */
        UPDATE [reading].[ArticleReadModel]
        SET
            [MetaTitle] = @MetaTitle,
            [MetaDescription] = @MetaDescription,
            [OgTitle] = @OgTitle,
            [OgDescription] = @OgDescription,
            [OgImageUrl] = @OgImageUrl,
            [TwitterTitle] = @TwitterTitle,
            [TwitterDescription] = @TwitterDescription,
            [TwitterImageUrl] = @TwitterImageUrl,
            [Robots] = @Robots,
            [SeoIsManualOverride] = @IsManualOverride
        WHERE [ArticlePublicId] = @ResourcePublicId;
    END

    COMMIT TRANSACTION;

    SELECT
        @Applied AS [Applied],
        CASE
            WHEN @Applied = 1 THEN N'Applied'
            WHEN @SourceVersion <= @CurrentSourceVersion THEN N'IgnoredStaleVersion'
            ELSE N'Ignored'
        END AS [Decision],
        @CurrentSourceVersion AS [PreviousSourceVersion],
        @SourceVersion AS [IncomingSourceVersion];
END
GO

/* =========================================================
   9) AUTHOR PROFILE PROJECTION FROM IDENTITY
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [reading].[Reading_AuthorProfileProjection_ApplyFromIdentity]
    @AuthorUserId              BIGINT,
    @AuthorUserPublicId        CHAR(26),
    @AuthorDisplayName         NVARCHAR(200) = NULL,
    @AuthorAvatarUrl           NVARCHAR(800) = NULL,
    @SourceVersion             BIGINT,
    @MessageId                 CHAR(26),
    @SourceOccurredAtUtc       DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @AuthorUserId IS NULL OR @AuthorUserId <= 0
        THROW 58380, 'AuthorUserId must be > 0.', 1;

    IF @AuthorUserPublicId IS NULL OR LEN(@AuthorUserPublicId) <> 26
        THROW 58381, 'AuthorUserPublicId must be a valid 26-character public id.', 1;

    IF @SourceVersion IS NULL OR @SourceVersion <= 0
        THROW 58382, 'SourceVersion must be > 0.', 1;

    IF @MessageId IS NULL OR LEN(@MessageId) <> 26
        THROW 58383, 'MessageId must be a valid 26-character id.', 1;

    IF @SourceOccurredAtUtc IS NULL
        THROW 58384, 'SourceOccurredAtUtc is required.', 1;

    SET @AuthorDisplayName = NULLIF(LTRIM(RTRIM(@AuthorDisplayName)), N'');
    SET @AuthorAvatarUrl = NULLIF(LTRIM(RTRIM(@AuthorAvatarUrl)), N'');

    DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();
    DECLARE @Applied BIT = 0;
    DECLARE @CurrentSourceVersion BIGINT = NULL;

    BEGIN TRANSACTION;

    SELECT
        @CurrentSourceVersion = [SourceVersion]
    FROM [reading].[AuthorProfileProjection] WITH (UPDLOCK, HOLDLOCK)
    WHERE [AuthorUserId] = @AuthorUserId;

    IF @CurrentSourceVersion IS NULL
    BEGIN
        INSERT INTO [reading].[AuthorProfileProjection]
        (
            [AuthorUserId],
            [AuthorUserPublicId],
            [AuthorDisplayName],
            [AuthorAvatarUrl],
            [SourceVersion],
            [LastEventMessageId],
            [LastSourceOccurredAtUtc],
            [LastSyncedAtUtc],
            [CreatedAtUtc],
            [UpdatedAtUtc]
        )
        VALUES
        (
            @AuthorUserId,
            @AuthorUserPublicId,
            @AuthorDisplayName,
            @AuthorAvatarUrl,
            @SourceVersion,
            @MessageId,
            @SourceOccurredAtUtc,
            @Now,
            @Now,
            @Now
        );

        SET @Applied = 1;
    END
    ELSE IF @SourceVersion > @CurrentSourceVersion
    BEGIN
        UPDATE [reading].[AuthorProfileProjection]
        SET
            [AuthorUserPublicId] = @AuthorUserPublicId,
            [AuthorDisplayName] = @AuthorDisplayName,
            [AuthorAvatarUrl] = @AuthorAvatarUrl,
            [SourceVersion] = @SourceVersion,
            [LastEventMessageId] = @MessageId,
            [LastSourceOccurredAtUtc] = @SourceOccurredAtUtc,
            [LastSyncedAtUtc] = @Now,
            [UpdatedAtUtc] = @Now
        WHERE [AuthorUserId] = @AuthorUserId
          AND [SourceVersion] < @SourceVersion;

        SET @Applied = CASE WHEN @@ROWCOUNT = 1 THEN 1 ELSE 0 END;
    END

    IF @Applied = 1
    BEGIN
        /*
          ArticleReadModel checkpoint fields belong to the Content stream.
          Identity author checkpoint is stored in AuthorProfileProjection.
        */
        UPDATE [reading].[ArticleReadModel]
        SET
            [AuthorDisplayName] = @AuthorDisplayName,
            [SearchText] = CONCAT_WS(
                N' ',
                [Title],
                [Summary],
                [Body],
                [CategoryName],
                @AuthorDisplayName
            )
        WHERE [AuthorUserId] = @AuthorUserId;
    END

    COMMIT TRANSACTION;

    SELECT
        @Applied AS [Applied],
        CASE
            WHEN @Applied = 1 THEN N'Applied'
            WHEN @CurrentSourceVersion IS NOT NULL
             AND @SourceVersion <= @CurrentSourceVersion THEN N'IgnoredStaleVersion'
            ELSE N'Ignored'
        END AS [Decision],
        @CurrentSourceVersion AS [PreviousSourceVersion],
        @SourceVersion AS [IncomingSourceVersion];
END
GO

/* =========================================================
   10) INTERACTION COUNTER SNAPSHOT PROJECTION
   Applies:
   - interaction.article_counters_projection_published

   Notes:
   - Interaction owns counter truth.
   - Reading stores only local displayed counter snapshots.
   - Snapshot apply is guarded by InteractionStatsVersion.
   - No ArticleReadModel row is required because Interaction snapshot
     may arrive before Content projection.
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[reading].[Reading_ArticleReadModel_UpdateCounters]', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE [reading].[Reading_ArticleReadModel_UpdateCounters];
END
GO

CREATE OR ALTER PROCEDURE [reading].[Reading_ArticleInteractionCounterProjection_ApplyFromInteraction]
    @ArticlePublicId             CHAR(26),
    @ViewCount                   BIGINT,
    @LikeCount                   BIGINT,
    @VisibleCommentCount         BIGINT,
    @InteractionStatsVersion     BIGINT,
    @MessageId                   CHAR(26),
    @SourceOccurredAtUtc         DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ArticlePublicId IS NULL OR LEN(@ArticlePublicId) <> 26
        THROW 58400, 'ArticlePublicId must be a valid 26-character public id.', 1;

    IF @ViewCount IS NULL
        OR @LikeCount IS NULL
        OR @VisibleCommentCount IS NULL
        OR @ViewCount < 0
        OR @LikeCount < 0
        OR @VisibleCommentCount < 0
    BEGIN
        THROW 58401, 'Interaction counters must be non-null and non-negative.', 1;
    END

    IF @InteractionStatsVersion IS NULL OR @InteractionStatsVersion <= 0
        THROW 58402, 'InteractionStatsVersion must be > 0.', 1;

    IF @MessageId IS NULL OR LEN(@MessageId) <> 26
        THROW 58403, 'MessageId must be a valid 26-character id.', 1;

    IF @SourceOccurredAtUtc IS NULL
        THROW 58404, 'SourceOccurredAtUtc is required.', 1;

    DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();
    DECLARE @Applied BIT = 0;
    DECLARE @CurrentInteractionStatsVersion BIGINT = NULL;

    BEGIN TRANSACTION;

    SELECT
        @CurrentInteractionStatsVersion = [InteractionStatsVersion]
    FROM [reading].[ArticleInteractionCounterProjection] WITH (UPDLOCK, HOLDLOCK)
    WHERE [ArticlePublicId] = @ArticlePublicId;

    IF @CurrentInteractionStatsVersion IS NULL
    BEGIN
        INSERT INTO [reading].[ArticleInteractionCounterProjection]
        (
            [ArticlePublicId],
            [ViewCount],
            [LikeCount],
            [VisibleCommentCount],
            [InteractionStatsVersion],
            [LastEventMessageId],
            [LastSourceOccurredAtUtc],
            [LastSyncedAtUtc],
            [CreatedAtUtc],
            [UpdatedAtUtc]
        )
        VALUES
        (
            @ArticlePublicId,
            @ViewCount,
            @LikeCount,
            @VisibleCommentCount,
            @InteractionStatsVersion,
            @MessageId,
            @SourceOccurredAtUtc,
            @Now,
            @Now,
            @Now
        );

        SET @Applied = 1;
    END
    ELSE IF @InteractionStatsVersion > @CurrentInteractionStatsVersion
    BEGIN
        UPDATE [reading].[ArticleInteractionCounterProjection]
        SET
            [ViewCount] = @ViewCount,
            [LikeCount] = @LikeCount,
            [VisibleCommentCount] = @VisibleCommentCount,
            [InteractionStatsVersion] = @InteractionStatsVersion,
            [LastEventMessageId] = @MessageId,
            [LastSourceOccurredAtUtc] = @SourceOccurredAtUtc,
            [LastSyncedAtUtc] = @Now,
            [UpdatedAtUtc] = @Now
        WHERE [ArticlePublicId] = @ArticlePublicId
          AND [InteractionStatsVersion] < @InteractionStatsVersion;

        SET @Applied = CASE WHEN @@ROWCOUNT = 1 THEN 1 ELSE 0 END;
    END

    COMMIT TRANSACTION;

    SELECT
        @Applied AS [Applied],
        CASE
            WHEN @Applied = 1 THEN N'Applied'
            WHEN @CurrentInteractionStatsVersion IS NOT NULL
             AND @InteractionStatsVersion <= @CurrentInteractionStatsVersion
                THEN N'IgnoredDuplicateOrStaleVersion'
            ELSE N'Ignored'
        END AS [Decision],
        @CurrentInteractionStatsVersion AS [PreviousInteractionStatsVersion],
        @InteractionStatsVersion AS [IncomingInteractionStatsVersion];
END
GO

/* =========================================================
   11) MEDIA PROJECTION STATE + ARTICLE MEDIA
   ========================================================= */

IF TYPE_ID(N'[reading].[ArticleMediaOrderListType]') IS NULL
BEGIN
    EXEC(N'
        CREATE TYPE [reading].[ArticleMediaOrderListType] AS TABLE
        (
            [MediaId] BIGINT NOT NULL,
            [SortOrder] INT NOT NULL
        );
    ');
END
GO

CREATE OR ALTER PROCEDURE [reading].[Reading_ArticleReadModelMedia_UpsertFromMediaAttachment]
    @ArticleId                 BIGINT,
    @MediaId                   BIGINT,
    @MediaPublicId             CHAR(26),
    @Url                       NVARCHAR(1000),
    @Alt                       NVARCHAR(300) = NULL,
    @Caption                   NVARCHAR(300) = NULL,
    @MediaType                 NVARCHAR(50),
    @SortOrder                 INT,
    @IsPrimary                 BIT,
    @SourceVersion             BIGINT,
    @MessageId                 CHAR(26) = NULL,
    @SourceOccurredAtUtc       DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 58300, 'ArticleId must be > 0.', 1;

    IF @MediaId IS NULL OR @MediaId <= 0
        THROW 58301, 'MediaId must be > 0.', 1;

    IF @MediaPublicId IS NULL OR LEN(@MediaPublicId) <> 26
        THROW 58302, 'MediaPublicId must be a valid 26-character public id.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Url, N'')))) = 0
        THROW 58303, 'Url is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@MediaType, N'')))) = 0
        THROW 58304, 'MediaType is required.', 1;

    IF @SortOrder IS NULL OR @SortOrder < 0
        THROW 58305, 'SortOrder must be non-negative.', 1;

    IF @SourceVersion IS NULL OR @SourceVersion <= 0
        THROW 58306, 'SourceVersion must be > 0.', 1;

    IF @IsPrimary IS NULL
        THROW 58307, 'IsPrimary is required.', 1;

    DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();
    DECLARE @Applied BIT = 0;
    DECLARE @CurrentSourceVersion BIGINT = NULL;

    BEGIN TRANSACTION;

    SELECT
        @CurrentSourceVersion = [SourceVersion]
    FROM [reading].[ArticleMediaProjectionState] WITH (UPDLOCK, HOLDLOCK)
    WHERE [ArticleId] = @ArticleId;

    IF @CurrentSourceVersion IS NULL
    BEGIN
        INSERT INTO [reading].[ArticleMediaProjectionState]
        (
            [ArticleId],
            [SourceVersion],
            [LastEventMessageId],
            [LastSourceOccurredAtUtc],
            [LastSyncedAtUtc]
        )
        VALUES
        (
            @ArticleId,
            0,
            NULL,
            NULL,
            @Now
        );

        SET @CurrentSourceVersion = 0;
    END

    IF @SourceVersion > @CurrentSourceVersion
    BEGIN
        /*
          If the incoming media becomes primary, demote any existing
          primary media first. This preserves the unique primary-media
          invariant enforced by UX_ArticleReadModelMedia_Article_Primary.
        */
        IF @IsPrimary = 1
        BEGIN
            UPDATE [reading].[ArticleReadModelMedia]
            SET
                [IsPrimary] = 0,
                [SourceVersion] = @SourceVersion,
                [LastSyncedAtUtc] = @Now
            WHERE [ArticleId] = @ArticleId
              AND [MediaId] <> @MediaId
              AND [IsPrimary] = 1;
        END

        UPDATE [reading].[ArticleReadModelMedia]
        SET
            [MediaPublicId] = @MediaPublicId,
            [Url] = @Url,
            [Alt] = @Alt,
            [Caption] = @Caption,
            [MediaType] = @MediaType,
            [SortOrder] = @SortOrder,
            [IsPrimary] = @IsPrimary,
            [SourceVersion] = @SourceVersion,
            [LastSyncedAtUtc] = @Now
        WHERE [ArticleId] = @ArticleId
          AND [MediaId] = @MediaId;

        IF @@ROWCOUNT = 0
        BEGIN
            INSERT INTO [reading].[ArticleReadModelMedia]
            (
                [ArticleId],
                [MediaId],
                [MediaPublicId],
                [Url],
                [Alt],
                [Caption],
                [MediaType],
                [SortOrder],
                [IsPrimary],
                [SourceVersion],
                [LastSyncedAtUtc]
            )
            VALUES
            (
                @ArticleId,
                @MediaId,
                @MediaPublicId,
                @Url,
                @Alt,
                @Caption,
                @MediaType,
                @SortOrder,
                @IsPrimary,
                @SourceVersion,
                @Now
            );
        END

        IF @IsPrimary = 1
        BEGIN
            /*
              ArticleReadModel checkpoint fields belong to the Content stream.
              Media checkpoint is stored in ArticleMediaProjectionState.
            */
            UPDATE [reading].[ArticleReadModel]
            SET
                [CoverMediaId] = @MediaId,
                [CoverMediaUrl] = @Url,
                [CoverAlt] = @Alt
            WHERE [ArticleId] = @ArticleId;
        END

        UPDATE [reading].[ArticleMediaProjectionState]
        SET
            [SourceVersion] = @SourceVersion,
            [LastEventMessageId] = @MessageId,
            [LastSourceOccurredAtUtc] = @SourceOccurredAtUtc,
            [LastSyncedAtUtc] = @Now
        WHERE [ArticleId] = @ArticleId;

        SET @Applied = 1;
    END

    COMMIT TRANSACTION;

    SELECT
        @Applied AS [Applied],
        CASE
            WHEN @Applied = 1 THEN N'Applied'
            WHEN @SourceVersion <= @CurrentSourceVersion THEN N'IgnoredStaleVersion'
            ELSE N'Ignored'
        END AS [Decision],
        @CurrentSourceVersion AS [PreviousSourceVersion],
        @SourceVersion AS [IncomingSourceVersion];
END
GO

CREATE OR ALTER PROCEDURE [reading].[Reading_ArticleReadModelMedia_SetPrimaryFromMedia]
    @ArticleId                 BIGINT,
    @MediaId                   BIGINT,
    @MediaPublicId             CHAR(26),
    @Url                       NVARCHAR(1000),
    @Alt                       NVARCHAR(300) = NULL,
    @Caption                   NVARCHAR(300) = NULL,
    @MediaType                 NVARCHAR(50),
    @SortOrder                 INT,
    @SourceVersion             BIGINT,
    @MessageId                 CHAR(26) = NULL,
    @SourceOccurredAtUtc       DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 58320, 'ArticleId must be > 0.', 1;

    IF @MediaId IS NULL OR @MediaId <= 0
        THROW 58321, 'MediaId must be > 0.', 1;

    IF @MediaPublicId IS NULL OR LEN(@MediaPublicId) <> 26
        THROW 58322, 'MediaPublicId must be a valid 26-character public id.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Url, N'')))) = 0
        THROW 58323, 'Url is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@MediaType, N'')))) = 0
        THROW 58324, 'MediaType is required.', 1;

    IF @SortOrder IS NULL OR @SortOrder < 0
        THROW 58325, 'SortOrder must be non-negative.', 1;

    IF @SourceVersion IS NULL OR @SourceVersion <= 0
        THROW 58326, 'SourceVersion must be > 0.', 1;

    DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();
    DECLARE @Applied BIT = 0;
    DECLARE @CurrentSourceVersion BIGINT = NULL;

    BEGIN TRANSACTION;

    SELECT @CurrentSourceVersion = [SourceVersion]
    FROM [reading].[ArticleMediaProjectionState] WITH (UPDLOCK, HOLDLOCK)
    WHERE [ArticleId] = @ArticleId;

    IF @CurrentSourceVersion IS NULL
    BEGIN
        INSERT INTO [reading].[ArticleMediaProjectionState]
        (
            [ArticleId],
            [SourceVersion],
            [LastEventMessageId],
            [LastSourceOccurredAtUtc],
            [LastSyncedAtUtc]
        )
        VALUES
        (
            @ArticleId,
            0,
            NULL,
            NULL,
            @Now
        );

        SET @CurrentSourceVersion = 0;
    END

    IF @SourceVersion > @CurrentSourceVersion
    BEGIN
        UPDATE [reading].[ArticleReadModelMedia]
        SET
            [IsPrimary] = 0,
            [SourceVersion] = @SourceVersion,
            [LastSyncedAtUtc] = @Now
        WHERE [ArticleId] = @ArticleId
          AND [MediaId] <> @MediaId
          AND [IsPrimary] = 1;

        UPDATE [reading].[ArticleReadModelMedia]
        SET
            [MediaPublicId] = @MediaPublicId,
            [Url] = @Url,
            [Alt] = @Alt,
            [Caption] = @Caption,
            [MediaType] = @MediaType,
            [SortOrder] = @SortOrder,
            [IsPrimary] = 1,
            [SourceVersion] = @SourceVersion,
            [LastSyncedAtUtc] = @Now
        WHERE [ArticleId] = @ArticleId
          AND [MediaId] = @MediaId;

        IF @@ROWCOUNT = 0
        BEGIN
            INSERT INTO [reading].[ArticleReadModelMedia]
            (
                [ArticleId],
                [MediaId],
                [MediaPublicId],
                [Url],
                [Alt],
                [Caption],
                [MediaType],
                [SortOrder],
                [IsPrimary],
                [SourceVersion],
                [LastSyncedAtUtc]
            )
            VALUES
            (
                @ArticleId,
                @MediaId,
                @MediaPublicId,
                @Url,
                @Alt,
                @Caption,
                @MediaType,
                @SortOrder,
                1,
                @SourceVersion,
                @Now
            );
        END

        /*
        ArticleReadModel checkpoint fields belong to Content.
        Media checkpoint is stored in ArticleMediaProjectionState.
        */
        UPDATE [reading].[ArticleReadModel]
        SET
            [CoverMediaId] = @MediaId,
            [CoverMediaUrl] = @Url,
            [CoverAlt] = @Alt
        WHERE [ArticleId] = @ArticleId;

        UPDATE [reading].[ArticleMediaProjectionState]
        SET
            [SourceVersion] = @SourceVersion,
            [LastEventMessageId] = @MessageId,
            [LastSourceOccurredAtUtc] = @SourceOccurredAtUtc,
            [LastSyncedAtUtc] = @Now
        WHERE [ArticleId] = @ArticleId;

        SET @Applied = 1;
    END

    COMMIT TRANSACTION;

    SELECT
        @Applied AS [Applied],
        CASE
            WHEN @Applied = 1 THEN N'Applied'
            WHEN @SourceVersion <= @CurrentSourceVersion THEN N'IgnoredStaleVersion'
            ELSE N'Ignored'
        END AS [Decision],
        @CurrentSourceVersion AS [PreviousSourceVersion],
        @SourceVersion AS [IncomingSourceVersion];
END
GO

CREATE OR ALTER PROCEDURE [reading].[Reading_ArticleReadModelMedia_ReorderFromMedia]
    @ArticleId                 BIGINT,
    @Orders                    [reading].[ArticleMediaOrderListType] READONLY,
    @SourceVersion             BIGINT,
    @MessageId                 CHAR(26) = NULL,
    @SourceOccurredAtUtc       DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 58340, 'ArticleId must be > 0.', 1;

    IF @SourceVersion IS NULL OR @SourceVersion <= 0
        THROW 58341, 'SourceVersion must be > 0.', 1;

    IF NOT EXISTS (SELECT 1 FROM @Orders)
        THROW 58342, 'Orders are required.', 1;

    IF EXISTS (SELECT 1 FROM @Orders WHERE [MediaId] <= 0 OR [SortOrder] < 0)
        THROW 58343, 'Orders contain invalid values.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM @Orders
        GROUP BY [MediaId]
        HAVING COUNT(1) > 1
    )
        THROW 58344, 'Orders contain duplicate media ids.', 1;

    DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();
    DECLARE @Applied BIT = 0;
    DECLARE @CurrentSourceVersion BIGINT = NULL;

    BEGIN TRANSACTION;

    SELECT @CurrentSourceVersion = [SourceVersion]
    FROM [reading].[ArticleMediaProjectionState] WITH (UPDLOCK, HOLDLOCK)
    WHERE [ArticleId] = @ArticleId;

    IF @CurrentSourceVersion IS NULL
    BEGIN
        INSERT INTO [reading].[ArticleMediaProjectionState]
        (
            [ArticleId],
            [SourceVersion],
            [LastEventMessageId],
            [LastSourceOccurredAtUtc],
            [LastSyncedAtUtc]
        )
        VALUES
        (
            @ArticleId,
            0,
            NULL,
            NULL,
            @Now
        );

        SET @CurrentSourceVersion = 0;
    END

    IF @SourceVersion > @CurrentSourceVersion
    BEGIN
        UPDATE ARM
        SET
            [SortOrder] = O.[SortOrder],
            [SourceVersion] = @SourceVersion,
            [LastSyncedAtUtc] = @Now
        FROM [reading].[ArticleReadModelMedia] ARM
        INNER JOIN @Orders O
            ON O.[MediaId] = ARM.[MediaId]
        WHERE ARM.[ArticleId] = @ArticleId;

        UPDATE [reading].[ArticleMediaProjectionState]
        SET
            [SourceVersion] = @SourceVersion,
            [LastEventMessageId] = @MessageId,
            [LastSourceOccurredAtUtc] = @SourceOccurredAtUtc,
            [LastSyncedAtUtc] = @Now
        WHERE [ArticleId] = @ArticleId;

        SET @Applied = 1;
    END

    COMMIT TRANSACTION;

    SELECT
        @Applied AS [Applied],
        CASE
            WHEN @Applied = 1 THEN N'Applied'
            WHEN @SourceVersion <= @CurrentSourceVersion THEN N'IgnoredStaleVersion'
            ELSE N'Ignored'
        END AS [Decision],
        @CurrentSourceVersion AS [PreviousSourceVersion],
        @SourceVersion AS [IncomingSourceVersion];
END
GO

CREATE OR ALTER PROCEDURE [reading].[Reading_ArticleReadModelMedia_DetachFromMedia]
    @ArticleId                 BIGINT,
    @MediaId                   BIGINT,
    @PrimaryCleared            BIT,
    @SourceVersion             BIGINT,
    @MessageId                 CHAR(26) = NULL,
    @SourceOccurredAtUtc       DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 58360, 'ArticleId must be > 0.', 1;

    IF @MediaId IS NULL OR @MediaId <= 0
        THROW 58361, 'MediaId must be > 0.', 1;

    IF @SourceVersion IS NULL OR @SourceVersion <= 0
        THROW 58362, 'SourceVersion must be > 0.', 1;

    DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();
    DECLARE @Applied BIT = 0;
    DECLARE @CurrentSourceVersion BIGINT = NULL;

    BEGIN TRANSACTION;

    SELECT
        @CurrentSourceVersion = [SourceVersion]
    FROM [reading].[ArticleMediaProjectionState] WITH (UPDLOCK, HOLDLOCK)
    WHERE [ArticleId] = @ArticleId;

    IF @CurrentSourceVersion IS NULL
    BEGIN
        INSERT INTO [reading].[ArticleMediaProjectionState]
        (
            [ArticleId],
            [SourceVersion],
            [LastEventMessageId],
            [LastSourceOccurredAtUtc],
            [LastSyncedAtUtc]
        )
        VALUES
        (
            @ArticleId,
            0,
            NULL,
            NULL,
            @Now
        );

        SET @CurrentSourceVersion = 0;
    END

    IF @SourceVersion > @CurrentSourceVersion
    BEGIN
        DELETE FROM [reading].[ArticleReadModelMedia]
        WHERE [ArticleId] = @ArticleId
          AND [MediaId] = @MediaId;

        IF @PrimaryCleared = 1
        BEGIN
            /*
            ArticleReadModel checkpoint fields belong to Content.
            Media checkpoint is stored in ArticleMediaProjectionState.
            */
            UPDATE [reading].[ArticleReadModel]
            SET
                [CoverMediaId] = NULL,
                [CoverMediaUrl] = NULL,
                [CoverAlt] = NULL
            WHERE [ArticleId] = @ArticleId
            AND [CoverMediaId] = @MediaId;
        END

        UPDATE [reading].[ArticleMediaProjectionState]
        SET
            [SourceVersion] = @SourceVersion,
            [LastEventMessageId] = @MessageId,
            [LastSourceOccurredAtUtc] = @SourceOccurredAtUtc,
            [LastSyncedAtUtc] = @Now
        WHERE [ArticleId] = @ArticleId;

        SET @Applied = 1;
    END

    COMMIT TRANSACTION;

    SELECT
        @Applied AS [Applied],
        CASE
            WHEN @Applied = 1 THEN N'Applied'
            WHEN @SourceVersion <= @CurrentSourceVersion THEN N'IgnoredStaleVersion'
            ELSE N'Ignored'
        END AS [Decision],
        @CurrentSourceVersion AS [PreviousSourceVersion],
        @SourceVersion AS [IncomingSourceVersion];
END
GO
