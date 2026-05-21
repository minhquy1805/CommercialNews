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
      * projection updates from SEO / Media / Interaction events

  Notes:
  - Public read path reads from [reading].[ArticleReadModel].
  - Source truth remains in Content / SEO / Media / Interaction.
  - Projection apply must be idempotent and version-aware.
  - SourceVersion prevents stale overwrite.
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

        [a].[PublishedAtUtc],
        [a].[UpdatedAtUtc],

        [a].[ViewCount],
        [a].[LikeCount],
        [a].[CommentCount],
        [a].[PopularityScore]
    FROM [reading].[ArticleReadModel] AS [a]
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
        [m].[Url],
        [m].[Alt],
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
    @Slug NVARCHAR(300)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@Slug, N'')))) = 0
        THROW 58220, 'Slug is required.', 1;

    DECLARE @ArticlePublicId CHAR(26);

    SELECT TOP (1)
        @ArticlePublicId = [ArticlePublicId]
    FROM [reading].[ArticleReadModel]
    WHERE [Slug] = @Slug
      AND [IsPublic] = 1
      AND [Status] = N'Published'
    ORDER BY [PublishedAtUtc] DESC, [ArticleId] DESC;

    IF @ArticlePublicId IS NULL
    BEGIN
        /* Empty result set 1 */
        SELECT
            CAST(NULL AS BIGINT) AS [ArticleId],
            CAST(NULL AS CHAR(26)) AS [ArticlePublicId],
            CAST(NULL AS NVARCHAR(300)) AS [Slug],
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
            CAST(NULL AS NVARCHAR(1000)) AS [CanonicalUrl],
            CAST(NULL AS NVARCHAR(300)) AS [MetaTitle],
            CAST(NULL AS NVARCHAR(500)) AS [MetaDescription],
            CAST(NULL AS DATETIME2(3)) AS [PublishedAtUtc],
            CAST(NULL AS DATETIME2(3)) AS [UpdatedAtUtc],
            CAST(NULL AS BIGINT) AS [ViewCount],
            CAST(NULL AS BIGINT) AS [LikeCount],
            CAST(NULL AS BIGINT) AS [CommentCount],
            CAST(NULL AS FLOAT) AS [PopularityScore]
        WHERE 1 = 0;

        /* Empty result set 2 */
        SELECT
            CAST(NULL AS BIGINT) AS [TagId],
            CAST(NULL AS CHAR(26)) AS [TagPublicId],
            CAST(NULL AS NVARCHAR(150)) AS [Name],
            CAST(NULL AS NVARCHAR(200)) AS [Slug]
        WHERE 1 = 0;

        /* Empty result set 3 */
        SELECT
            CAST(NULL AS BIGINT) AS [MediaId],
            CAST(NULL AS NVARCHAR(1000)) AS [Url],
            CAST(NULL AS NVARCHAR(300)) AS [Alt],
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

    IF @SortBy NOT IN (N'PublishedAt', N'Popularity')
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

            [a].[ViewCount],
            [a].[LikeCount],
            [a].[CommentCount],
            [a].[PopularityScore],

            COUNT(1) OVER() AS [TotalCount]
        FROM [reading].[ArticleReadModel] AS [a]
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
        [CommentCount],
        [PopularityScore],

        [TotalCount]
    FROM [Filtered]
    ORDER BY
        CASE WHEN @SortBy = N'PublishedAt' AND UPPER(@SortDirection) = N'ASC'  THEN [PublishedAtUtc] END ASC,
        CASE WHEN @SortBy = N'PublishedAt' AND UPPER(@SortDirection) = N'DESC' THEN [PublishedAtUtc] END DESC,
        CASE WHEN @SortBy = N'Popularity'  AND UPPER(@SortDirection) = N'ASC'  THEN [PopularityScore] END ASC,
        CASE WHEN @SortBy = N'Popularity'  AND UPPER(@SortDirection) = N'DESC' THEN [PopularityScore] END DESC,
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
            CAST(NULL AS NVARCHAR(300)) AS [Slug],
            CAST(NULL AS NVARCHAR(300)) AS [Title],
            CAST(NULL AS NVARCHAR(1000)) AS [Summary],
            CAST(NULL AS BIGINT) AS [CategoryId],
            CAST(NULL AS NVARCHAR(200)) AS [CategoryName],
            CAST(NULL AS BIGINT) AS [CoverMediaId],
            CAST(NULL AS NVARCHAR(1000)) AS [CoverMediaUrl],
            CAST(NULL AS NVARCHAR(300)) AS [CoverAlt],
            CAST(NULL AS DATETIME2(3)) AS [PublishedAtUtc],
            CAST(NULL AS BIGINT) AS [ViewCount],
            CAST(NULL AS BIGINT) AS [LikeCount],
            CAST(NULL AS BIGINT) AS [CommentCount],
            CAST(NULL AS FLOAT) AS [PopularityScore]
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

            [a].[CoverMediaId],
            [a].[CoverMediaUrl],
            [a].[CoverAlt],

            [a].[PublishedAtUtc],

            [a].[ViewCount],
            [a].[LikeCount],
            [a].[CommentCount],
            [a].[PopularityScore],

            CASE
                WHEN @CategoryId IS NOT NULL AND [a].[CategoryId] = @CategoryId THEN 1
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

        [CoverMediaId],
        [CoverMediaUrl],
        [CoverAlt],

        [PublishedAtUtc],

        [ViewCount],
        [LikeCount],
        [CommentCount],
        [PopularityScore]
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

    IF @UpdatedAtUtc IS NULL
        SET @UpdatedAtUtc = SYSUTCDATETIME();

    DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();
    DECLARE @Applied BIT = 0;
    DECLARE @CurrentSourceVersion BIGINT = NULL;
    DECLARE @CurrentPublishedAtUtc DATETIME2(3) = NULL;
    DECLARE @EffectivePublishedAtUtc DATETIME2(3) = NULL;
    DECLARE @EffectiveIsPublic BIT = 0;

    BEGIN TRANSACTION;

    SELECT
        @CurrentSourceVersion = [SourceVersion],
        @CurrentPublishedAtUtc = [PublishedAtUtc]
    FROM [reading].[ArticleReadModel] WITH (UPDLOCK, HOLDLOCK)
    WHERE [ArticleId] = @ArticleId;

    SET @EffectivePublishedAtUtc =
        CASE
            WHEN @Status = N'Published' THEN COALESCE(@PublishedAtUtc, @CurrentPublishedAtUtc)
            ELSE NULL
        END;

    SET @EffectiveIsPublic =
        CASE
            WHEN @Status = N'Published'
             AND @IsPublic = 1
             AND @EffectivePublishedAtUtc IS NOT NULL THEN 1
            ELSE 0
        END;

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
            [Status],
            [IsPublic],
            [PublishedAtUtc],
            [UpdatedAtUtc],
            [SearchText],
            [ViewCount],
            [LikeCount],
            [CommentCount],
            [PopularityScore],
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
            NULL,
            @Title,
            @Summary,
            @Body,
            @CategoryId,
            @CategoryName,
            @AuthorUserId,
            @AuthorDisplayName,
            NULL,
            NULL,
            NULL,
            NULL,
            NULL,
            NULL,
            @Status,
            @EffectiveIsPublic,
            @EffectivePublishedAtUtc,
            @UpdatedAtUtc,
            CONCAT_WS(N' ', @Title, @Summary, @Body, @CategoryName, @AuthorDisplayName),
            0,
            0,
            0,
            NULL,
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
            [AuthorDisplayName] = @AuthorDisplayName,
            [Status] = @Status,
            [IsPublic] = @EffectiveIsPublic,
            [PublishedAtUtc] = @EffectivePublishedAtUtc,
            [UpdatedAtUtc] = @UpdatedAtUtc,
            [SearchText] = CONCAT_WS(N' ', @Title, @Summary, @Body, @CategoryName, @AuthorDisplayName),
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
            WHEN @CurrentSourceVersion IS NOT NULL AND @SourceVersion <= @CurrentSourceVersion THEN N'IgnoredStaleVersion'
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
   8) UPDATE SEO PROJECTION
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [reading].[Reading_ArticleReadModel_UpdateSeo]
    @ArticleId                 BIGINT,
    @Slug                      NVARCHAR(300) = NULL,
    @CanonicalUrl              NVARCHAR(1000) = NULL,
    @MetaTitle                 NVARCHAR(300) = NULL,
    @MetaDescription           NVARCHAR(500) = NULL,
    @MessageId                 CHAR(26) = NULL,
    @SourceOccurredAtUtc       DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 58270, 'ArticleId must be > 0.', 1;

    UPDATE [reading].[ArticleReadModel]
    SET
        [Slug] = NULLIF(LTRIM(RTRIM(@Slug)), N''),
        [CanonicalUrl] = @CanonicalUrl,
        [MetaTitle] = @MetaTitle,
        [MetaDescription] = @MetaDescription,
        [LastEventMessageId] = @MessageId,
        [LastSourceOccurredAtUtc] = @SourceOccurredAtUtc,
        [LastSyncedAtUtc] = SYSUTCDATETIME()
    WHERE [ArticleId] = @ArticleId;

    SELECT
        CAST(CASE WHEN @@ROWCOUNT = 1 THEN 1 ELSE 0 END AS BIT) AS [Applied],
        CASE WHEN @@ROWCOUNT = 1 THEN N'Applied' ELSE N'IgnoredMissingArticle' END AS [Decision];
END
GO

/* =========================================================
   9) UPDATE COUNTERS
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [reading].[Reading_ArticleReadModel_UpdateCounters]
    @ArticleId                 BIGINT,
    @ViewCount                 BIGINT = 0,
    @LikeCount                 BIGINT = 0,
    @CommentCount              BIGINT = 0,
    @PopularityScore           FLOAT = NULL,
    @MessageId                 CHAR(26) = NULL,
    @SourceOccurredAtUtc       DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 58280, 'ArticleId must be > 0.', 1;

    IF @ViewCount < 0 OR @LikeCount < 0 OR @CommentCount < 0
        THROW 58281, 'Counters must be non-negative.', 1;

    UPDATE [reading].[ArticleReadModel]
    SET
        [ViewCount] = @ViewCount,
        [LikeCount] = @LikeCount,
        [CommentCount] = @CommentCount,
        [PopularityScore] = @PopularityScore,
        [LastEventMessageId] = @MessageId,
        [LastSourceOccurredAtUtc] = @SourceOccurredAtUtc,
        [LastSyncedAtUtc] = SYSUTCDATETIME()
    WHERE [ArticleId] = @ArticleId;

    SELECT
        CAST(CASE WHEN @@ROWCOUNT = 1 THEN 1 ELSE 0 END AS BIT) AS [Applied],
        CASE WHEN @@ROWCOUNT = 1 THEN N'Applied' ELSE N'IgnoredMissingArticle' END AS [Decision];
END
GO
