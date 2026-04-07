/*
  File: db/10_modules/reading/020_procs.sql
  Module: Reading
  Purpose:
  - Create stored procedures for Reading V1 public query facade in CommercialNews.
  - Reading owns no physical truth tables in V1.
  - Reading may own read-composition stored procedures over Content + SEO + Media + Interaction derived stats.

  Support:
      * public article detail by id
      * public article detail by slug
      * public article listing with paging/filter/sort
      * deterministic related articles
      * public article search

  Notes:
  - Reading MUST still enforce Content truth visibility:
      * [content].[Article].[Status] = N'Published'
      * [content].[Article].[IsDeleted] = 0
  - SEO resolves routing truth, but SEO does not decide publication visibility.
  - Media enriches cover/attachments.
  - Interaction enriches reading with derived counters from [interaction].[ArticleInteractionStats].
  - If interaction stats are missing:
      * [Views] = 0
      * [Likes] = 0
      * [CountersPartial] = 1
  - This file is idempotent: safe to re-run.
*/

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

IF SCHEMA_ID(N'content') IS NULL
BEGIN
    THROW 58203, 'Schema [content] does not exist. Run bootstrap scripts first.', 1;
END
GO

IF SCHEMA_ID(N'seo') IS NULL
BEGIN
    THROW 58204, 'Schema [seo] does not exist. Run bootstrap scripts first.', 1;
END
GO

IF SCHEMA_ID(N'media') IS NULL
BEGIN
    THROW 58205, 'Schema [media] does not exist. Run bootstrap scripts first.', 1;
END
GO

IF OBJECT_ID(N'[content].[Article]', N'U') IS NULL
BEGIN
    THROW 58206, 'Table [content].[Article] does not exist. Run content/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[content].[Category]', N'U') IS NULL
BEGIN
    THROW 58207, 'Table [content].[Category] does not exist. Run content/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[content].[Tag]', N'U') IS NULL
BEGIN
    THROW 58208, 'Table [content].[Tag] does not exist. Run content/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[content].[ArticleTag]', N'U') IS NULL
BEGIN
    THROW 58209, 'Table [content].[ArticleTag] does not exist. Run content/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[seo].[SlugRegistry]', N'U') IS NULL
BEGIN
    THROW 58210, 'Table [seo].[SlugRegistry] does not exist. Run seo/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[seo].[SeoMetadata]', N'U') IS NULL
BEGIN
    THROW 58211, 'Table [seo].[SeoMetadata] does not exist. Run seo/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[media].[MediaAsset]', N'U') IS NULL
BEGIN
    THROW 58212, 'Table [media].[MediaAsset] does not exist. Run media/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[media].[ArticleMedia]', N'U') IS NULL
BEGIN
    THROW 58213, 'Table [media].[ArticleMedia] does not exist. Run media/001_tables.sql first.', 1;
END
GO

IF SCHEMA_ID(N'interaction') IS NULL
BEGIN
    THROW 58206, 'Schema [interaction] does not exist. Run bootstrap scripts first.', 1;
END
GO

IF OBJECT_ID(N'[interaction].[ArticleInteractionStats]', N'U') IS NULL
BEGIN
    THROW 58214, 'Table [interaction].[ArticleInteractionStats] does not exist. Run interaction/001_tables.sql first.', 1;
END
GO

/* =========================================================
   PUBLIC ARTICLE DETAIL
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [reading].[Reading_Article_SelectById]
    @ArticleId BIGINT,
    @Scope     VARCHAR(30) = 'public'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 58220, 'ArticleId must be > 0.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Scope, '')))) = 0
        THROW 58221, 'Scope is required.', 1;

    IF @Scope NOT IN ('public')
        THROW 58222, 'Scope is invalid.', 1;

    /* -----------------------------------------------------
       Result set 1: detail core
       ----------------------------------------------------- */
    SELECT TOP (1)
        [a].[ArticleId],
        [a].[Title],
        [a].[Summary],
        [a].[Content] AS [Body],
        [a].[PublishedAt],

        [a].[CategoryId],
        [c].[Name] AS [CategoryName],

        [sr].[Slug],

        COALESCE([sm].[CanonicalUrl], [sr].[CanonicalUrl]) AS [CanonicalUrl],
        [sm].[MetaTitle],
        [sm].[MetaDescription],

        ISNULL([s].[ViewsTotal], 0) AS [Views],
        ISNULL([s].[LikesTotal], 0) AS [Likes],
        CAST(CASE WHEN [s].[ArticleId] IS NULL THEN 1 ELSE 0 END AS BIT) AS [CountersPartial]
    FROM [content].[Article] AS [a]
    LEFT JOIN [content].[Category] AS [c]
        ON [c].[CategoryId] = [a].[CategoryId]
       AND [c].[IsDeleted] = 0
       AND [c].[IsActive] = 1
    LEFT JOIN [seo].[SlugRegistry] AS [sr]
        ON [sr].[ArticleId] = [a].[ArticleId]
       AND [sr].[Scope] = @Scope
       AND [sr].[IsActive] = 1
    LEFT JOIN [seo].[SeoMetadata] AS [sm]
        ON [sm].[ArticleId] = [a].[ArticleId]
    LEFT JOIN [interaction].[ArticleInteractionStats] AS [s]
        ON [s].[ArticleId] = [a].[ArticleId]
    WHERE [a].[ArticleId] = @ArticleId
      AND [a].[Status] = N'Published'
      AND [a].[IsDeleted] = 0;

    /* -----------------------------------------------------
       Result set 2: tags
       ----------------------------------------------------- */
    SELECT
        [t].[TagId],
        [t].[Name]
    FROM [content].[ArticleTag] AS [at]
    INNER JOIN [content].[Tag] AS [t]
        ON [t].[TagId] = [at].[TagId]
       AND [t].[IsDeleted] = 0
       AND [t].[IsActive] = 1
    INNER JOIN [content].[Article] AS [a]
        ON [a].[ArticleId] = [at].[ArticleId]
       AND [a].[Status] = N'Published'
       AND [a].[IsDeleted] = 0
    WHERE [at].[ArticleId] = @ArticleId
    ORDER BY [t].[Name] ASC, [t].[TagId] ASC;

    /* -----------------------------------------------------
       Result set 3: media
       ----------------------------------------------------- */
    SELECT
        [am].[MediaId],
        [ma].[Url],
        COALESCE([am].[AltTextOverride], [ma].[AltText]) AS [Alt],
        [am].[IsPrimary],
        [am].[SortOrder] AS [DisplayOrder]
    FROM [media].[ArticleMedia] AS [am]
    INNER JOIN [media].[MediaAsset] AS [ma]
        ON [ma].[MediaId] = [am].[MediaId]
       AND [ma].[IsDeleted] = 0
    INNER JOIN [content].[Article] AS [a]
        ON [a].[ArticleId] = [am].[ArticleId]
       AND [a].[Status] = N'Published'
       AND [a].[IsDeleted] = 0
    WHERE [am].[ArticleId] = @ArticleId
      AND [am].[IsDeleted] = 0
    ORDER BY [am].[SortOrder] ASC, [am].[ArticleMediaId] ASC;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [reading].[Reading_Article_SelectBySlug]
    @Scope VARCHAR(30),
    @Slug  NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@Scope, '')))) = 0
        THROW 58230, 'Scope is required.', 1;

    IF @Scope NOT IN ('public')
        THROW 58231, 'Scope is invalid.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Slug, N'')))) = 0
        THROW 58232, 'Slug is required.', 1;

    DECLARE @ArticleId BIGINT = NULL;

    SELECT TOP (1)
        @ArticleId = [sr].[ArticleId]
    FROM [seo].[SlugRegistry] AS [sr]
    WHERE [sr].[Scope] = @Scope
      AND [sr].[Slug] = @Slug
      AND [sr].[IsActive] = 1
    ORDER BY [sr].[SlugId] DESC;

    IF @ArticleId IS NULL
    BEGIN
        /* Empty result sets with expected shape */
        SELECT
            CAST(NULL AS BIGINT) AS [ArticleId],
            CAST(NULL AS NVARCHAR(300)) AS [Title],
            CAST(NULL AS NVARCHAR(2000)) AS [Summary],
            CAST(NULL AS NVARCHAR(MAX)) AS [Body],
            CAST(NULL AS DATETIME2(3)) AS [PublishedAt],
            CAST(NULL AS BIGINT) AS [CategoryId],
            CAST(NULL AS NVARCHAR(200)) AS [CategoryName],
            CAST(NULL AS NVARCHAR(200)) AS [Slug],
            CAST(NULL AS NVARCHAR(500)) AS [CanonicalUrl],
            CAST(NULL AS NVARCHAR(300)) AS [MetaTitle],
            CAST(NULL AS NVARCHAR(500)) AS [MetaDescription],
            CAST(NULL AS BIGINT) AS [Views],
            CAST(NULL AS BIGINT) AS [Likes],
            CAST(NULL AS BIT) AS [CountersPartial]
        WHERE 1 = 0;

        SELECT
            CAST(NULL AS BIGINT) AS [TagId],
            CAST(NULL AS NVARCHAR(150)) AS [Name]
        WHERE 1 = 0;

        SELECT
            CAST(NULL AS BIGINT) AS [MediaId],
            CAST(NULL AS NVARCHAR(800)) AS [Url],
            CAST(NULL AS NVARCHAR(300)) AS [Alt],
            CAST(NULL AS BIT) AS [IsPrimary],
            CAST(NULL AS INT) AS [DisplayOrder]
        WHERE 1 = 0;

        RETURN;
    END

    EXEC [reading].[Reading_Article_SelectById]
        @ArticleId = @ArticleId,
        @Scope = @Scope;
END
GO

/* =========================================================
   PUBLIC ARTICLE LIST
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [reading].[Reading_Article_SelectSkipAndTake]
    @Skip               INT = 0,
    @Take               INT = 20,
    @CategoryId         BIGINT = NULL,
    @TagId              BIGINT = NULL,
    @Keyword            NVARCHAR(300) = NULL,
    @SortBy             NVARCHAR(30) = N'PublishedAt',
    @SortDirection      NVARCHAR(4) = N'DESC',
    @Scope              VARCHAR(30) = 'public'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;
    IF @Take > 200 SET @Take = 200;

    IF @SortBy NOT IN (N'PublishedAt', N'Popularity')
        SET @SortBy = N'PublishedAt';

    IF UPPER(@SortDirection) NOT IN (N'ASC', N'DESC')
        SET @SortDirection = N'DESC';

    IF @Scope NOT IN ('public')
        SET @Scope = 'public';

    ;WITH [Filtered] AS
    (
        SELECT
            [a].[ArticleId],
            [a].[Title],
            [a].[Summary],
            [a].[PublishedAt],

            [a].[CategoryId],
            [c].[Name] AS [CategoryName],

            [sr].[Slug],

            [am].[MediaId] AS [CoverMediaId],
            [ma].[Url] AS [CoverUrl],
            COALESCE([am].[AltTextOverride], [ma].[AltText]) AS [CoverAlt],
            [am].[IsPrimary] AS [CoverIsPrimary],
            [am].[SortOrder] AS [CoverDisplayOrder],

            ISNULL([s].[ViewsTotal], 0) AS [Views],
            ISNULL([s].[LikesTotal], 0) AS [Likes],
            CAST(CASE WHEN [s].[ArticleId] IS NULL THEN 1 ELSE 0 END AS BIT) AS [CountersPartial],
            ISNULL([s].[PopularityScore], CAST(0 AS DECIMAL(18,4))) AS [PopularityScore],

            COUNT(1) OVER() AS [TotalCount]
        FROM [content].[Article] AS [a]
        LEFT JOIN [content].[Category] AS [c]
            ON [c].[CategoryId] = [a].[CategoryId]
           AND [c].[IsDeleted] = 0
           AND [c].[IsActive] = 1
        LEFT JOIN [seo].[SlugRegistry] AS [sr]
            ON [sr].[ArticleId] = [a].[ArticleId]
           AND [sr].[Scope] = @Scope
           AND [sr].[IsActive] = 1
        LEFT JOIN [media].[ArticleMedia] AS [am]
            ON [am].[ArticleId] = [a].[ArticleId]
           AND [am].[IsPrimary] = 1
           AND [am].[IsDeleted] = 0
        LEFT JOIN [media].[MediaAsset] AS [ma]
            ON [ma].[MediaId] = [am].[MediaId]
           AND [ma].[IsDeleted] = 0
        LEFT JOIN [interaction].[ArticleInteractionStats] AS [s]
            ON [s].[ArticleId] = [a].[ArticleId]
        WHERE [a].[Status] = N'Published'
          AND [a].[IsDeleted] = 0
          AND (@CategoryId IS NULL OR [a].[CategoryId] = @CategoryId)
          AND
          (
              @TagId IS NULL
              OR EXISTS
              (
                  SELECT 1
                  FROM [content].[ArticleTag] AS [at]
                  INNER JOIN [content].[Tag] AS [t]
                      ON [t].[TagId] = [at].[TagId]
                     AND [t].[IsDeleted] = 0
                     AND [t].[IsActive] = 1
                  WHERE [at].[ArticleId] = [a].[ArticleId]
                    AND [at].[TagId] = @TagId
              )
          )
          AND
          (
              @Keyword IS NULL
              OR [a].[Title] LIKE N'%' + @Keyword + N'%'
              OR [a].[Summary] LIKE N'%' + @Keyword + N'%'
              OR [a].[Content] LIKE N'%' + @Keyword + N'%'
          )
    )
    SELECT
        [ArticleId],
        [Title],
        [Summary],
        [Slug],
        [PublishedAt],
        [CategoryId],
        [CategoryName],
        [CoverMediaId],
        [CoverUrl],
        [CoverAlt],
        [CoverIsPrimary],
        [CoverDisplayOrder],
        [Views],
        [Likes],
        [CountersPartial],
        [TotalCount]
    FROM [Filtered]
    ORDER BY
        CASE WHEN @SortBy = N'PublishedAt' AND @SortDirection = N'ASC'  THEN [PublishedAt] END ASC,
        CASE WHEN @SortBy = N'PublishedAt' AND @SortDirection = N'DESC' THEN [PublishedAt] END DESC,
        CASE WHEN @SortBy = N'Popularity'  AND @SortDirection = N'ASC'  THEN [PopularityScore] END ASC,
        CASE WHEN @SortBy = N'Popularity'  AND @SortDirection = N'DESC' THEN [PopularityScore] END DESC,
        [ArticleId] DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END
GO

/* =========================================================
   PUBLIC RELATED ARTICLES
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [reading].[Reading_Article_SelectRelated]
    @ArticleId BIGINT,
    @Limit     INT = 6,
    @Scope     VARCHAR(30) = 'public'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 58240, 'ArticleId must be > 0.', 1;

    IF @Limit <= 0 SET @Limit = 6;
    IF @Limit > 50 SET @Limit = 50;

    IF @Scope NOT IN ('public')
        SET @Scope = 'public';

    DECLARE @CategoryId BIGINT = NULL;

    SELECT TOP (1)
        @CategoryId = [a].[CategoryId]
    FROM [content].[Article] AS [a]
    WHERE [a].[ArticleId] = @ArticleId
      AND [a].[Status] = N'Published'
      AND [a].[IsDeleted] = 0;

    ;WITH [Candidate] AS
    (
        SELECT
            [a].[ArticleId],
            [a].[Title],
            [a].[Summary],
            [a].[PublishedAt],

            [a].[CategoryId],
            [c].[Name] AS [CategoryName],

            [sr].[Slug],

            [am].[MediaId] AS [CoverMediaId],
            [ma].[Url] AS [CoverUrl],
            COALESCE([am].[AltTextOverride], [ma].[AltText]) AS [CoverAlt],
            [am].[IsPrimary] AS [CoverIsPrimary],
            [am].[SortOrder] AS [CoverDisplayOrder],

            ISNULL([s].[ViewsTotal], 0) AS [Views],
            ISNULL([s].[LikesTotal], 0) AS [Likes],
            CAST(CASE WHEN [s].[ArticleId] IS NULL THEN 1 ELSE 0 END AS BIT) AS [CountersPartial],

            CASE
                WHEN @CategoryId IS NOT NULL AND [a].[CategoryId] = @CategoryId THEN 1
                WHEN EXISTS
                (
                    SELECT 1
                    FROM [content].[ArticleTag] AS [atCandidate]
                    INNER JOIN [content].[Tag] AS [tCandidate]
                        ON [tCandidate].[TagId] = [atCandidate].[TagId]
                       AND [tCandidate].[IsDeleted] = 0
                       AND [tCandidate].[IsActive] = 1
                    WHERE [atCandidate].[ArticleId] = [a].[ArticleId]
                      AND EXISTS
                      (
                          SELECT 1
                          FROM [content].[ArticleTag] AS [atCurrent]
                          INNER JOIN [content].[Tag] AS [tCurrent]
                              ON [tCurrent].[TagId] = [atCurrent].[TagId]
                             AND [tCurrent].[IsDeleted] = 0
                             AND [tCurrent].[IsActive] = 1
                          WHERE [atCurrent].[ArticleId] = @ArticleId
                            AND [atCurrent].[TagId] = [atCandidate].[TagId]
                      )
                ) THEN 2
                ELSE 3
            END AS [MatchRank]
        FROM [content].[Article] AS [a]
        LEFT JOIN [content].[Category] AS [c]
            ON [c].[CategoryId] = [a].[CategoryId]
           AND [c].[IsDeleted] = 0
           AND [c].[IsActive] = 1
        LEFT JOIN [seo].[SlugRegistry] AS [sr]
            ON [sr].[ArticleId] = [a].[ArticleId]
           AND [sr].[Scope] = @Scope
           AND [sr].[IsActive] = 1
        LEFT JOIN [media].[ArticleMedia] AS [am]
            ON [am].[ArticleId] = [a].[ArticleId]
           AND [am].[IsPrimary] = 1
           AND [am].[IsDeleted] = 0
        LEFT JOIN [media].[MediaAsset] AS [ma]
            ON [ma].[MediaId] = [am].[MediaId]
           AND [ma].[IsDeleted] = 0
        LEFT JOIN [interaction].[ArticleInteractionStats] AS [s]
            ON [s].[ArticleId] = [a].[ArticleId]
        WHERE [a].[ArticleId] <> @ArticleId
          AND [a].[Status] = N'Published'
          AND [a].[IsDeleted] = 0
    )
    SELECT TOP (@Limit)
        [ArticleId],
        [Title],
        [Summary],
        [Slug],
        [PublishedAt],
        [CategoryId],
        [CategoryName],
        [CoverMediaId],
        [CoverUrl],
        [CoverAlt],
        [CoverIsPrimary],
        [CoverDisplayOrder],
        [Views],
        [Likes],
        [CountersPartial]
    FROM [Candidate]
    ORDER BY
        [MatchRank] ASC,
        [PublishedAt] DESC,
        [ArticleId] DESC;
END
GO

/* =========================================================
   PUBLIC SEARCH
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [reading].[Reading_Article_Search]
    @Keyword            NVARCHAR(300),
    @Skip               INT = 0,
    @Take               INT = 20,
    @SortBy             NVARCHAR(30) = N'PublishedAt',
    @SortDirection      NVARCHAR(4) = N'DESC',
    @Scope              VARCHAR(30) = 'public'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@Keyword, N'')))) = 0
        THROW 58250, 'Keyword is required.', 1;

    EXEC [reading].[Reading_Article_SelectSkipAndTake]
        @Skip = @Skip,
        @Take = @Take,
        @CategoryId = NULL,
        @TagId = NULL,
        @Keyword = @Keyword,
        @SortBy = @SortBy,
        @SortDirection = @SortDirection,
        @Scope = @Scope;
END
GO