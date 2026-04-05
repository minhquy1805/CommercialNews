/*
  File: db/10_modules/seo/020_procs.sql
  Module: SEO
  Purpose:
  - Create stored procedures for SEO truth model in CommercialNews V1.
  - Support:
      * SeoMetadata CRUD-ish reads/writes
      * SlugRegistry CRUD-ish reads/writes
      * route activation / deactivation
      * hot-path slug resolve
      * admin paging / filtering
      * article SEO aggregate read
  - Idempotent: safe to re-run.

  Notes:
  - Truth remains in [seo] tables.
  - Public read model / projections / cache / search are outside this file.
  - App layer should still orchestrate transaction boundaries where multiple procs
    must succeed atomically (for example: slug switch + metadata update + outbox write).
  - SEO owns routing truth and SEO metadata truth.
  - SEO does NOT own publication visibility truth.
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

/* =========================================================
   SEO METADATA
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [seo].[Seo_SeoMetadata_Insert]
    @ArticleId              BIGINT,
    @CanonicalUrl           NVARCHAR(500) = NULL,
    @MetaTitle              NVARCHAR(300) = NULL,
    @MetaDescription        NVARCHAR(500) = NULL,
    @OgTitle                NVARCHAR(300) = NULL,
    @OgDescription          NVARCHAR(500) = NULL,
    @OgImageUrl             NVARCHAR(800) = NULL,
    @TwitterTitle           NVARCHAR(300) = NULL,
    @TwitterDescription     NVARCHAR(500) = NULL,
    @TwitterImageUrl        NVARCHAR(800) = NULL,
    @UpdatedByUserId        BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 57210, 'ArticleId must be > 0.', 1;

    IF [content].[Article] IS NULL
        PRINT N''; -- no-op; keeps parser calm in some editors

    IF NOT EXISTS
    (
        SELECT 1
        FROM [content].[Article]
        WHERE [ArticleId] = @ArticleId
          AND [IsDeleted] = 0
    )
        THROW 57211, 'Article does not exist or was deleted.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [seo].[SeoMetadata]
        WHERE [ArticleId] = @ArticleId
    )
        THROW 57212, 'SeoMetadata already exists for this article.', 1;

    INSERT INTO [seo].[SeoMetadata]
    (
        [ArticleId],
        [CanonicalUrl],
        [MetaTitle],
        [MetaDescription],
        [OgTitle],
        [OgDescription],
        [OgImageUrl],
        [TwitterTitle],
        [TwitterDescription],
        [TwitterImageUrl],
        [UpdatedByUserId]
    )
    VALUES
    (
        @ArticleId,
        @CanonicalUrl,
        @MetaTitle,
        @MetaDescription,
        @OgTitle,
        @OgDescription,
        @OgImageUrl,
        @TwitterTitle,
        @TwitterDescription,
        @TwitterImageUrl,
        @UpdatedByUserId
    );

    SELECT TOP (1) *
    FROM [seo].[SeoMetadata]
    WHERE [SeoId] = SCOPE_IDENTITY();
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [seo].[Seo_SeoMetadata_Update]
    @SeoId                  BIGINT,
    @CanonicalUrl           NVARCHAR(500) = NULL,
    @MetaTitle              NVARCHAR(300) = NULL,
    @MetaDescription        NVARCHAR(500) = NULL,
    @OgTitle                NVARCHAR(300) = NULL,
    @OgDescription          NVARCHAR(500) = NULL,
    @OgImageUrl             NVARCHAR(800) = NULL,
    @TwitterTitle           NVARCHAR(300) = NULL,
    @TwitterDescription     NVARCHAR(500) = NULL,
    @TwitterImageUrl        NVARCHAR(800) = NULL,
    @UpdatedByUserId        BIGINT = NULL,
    @ExpectedVersion        INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @SeoId IS NULL OR @SeoId <= 0
        THROW 57220, 'SeoId must be > 0.', 1;

    UPDATE [seo].[SeoMetadata]
    SET
        [CanonicalUrl] = @CanonicalUrl,
        [MetaTitle] = @MetaTitle,
        [MetaDescription] = @MetaDescription,
        [OgTitle] = @OgTitle,
        [OgDescription] = @OgDescription,
        [OgImageUrl] = @OgImageUrl,
        [TwitterTitle] = @TwitterTitle,
        [TwitterDescription] = @TwitterDescription,
        [TwitterImageUrl] = @TwitterImageUrl,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedByUserId] = @UpdatedByUserId,
        [Version] = [Version] + 1
    WHERE [SeoId] = @SeoId
      AND [Version] = @ExpectedVersion;

    IF @@ROWCOUNT = 0
        THROW 57221, 'SeoMetadata update failed. Record not found or version mismatch.', 1;

    SELECT TOP (1) *
    FROM [seo].[SeoMetadata]
    WHERE [SeoId] = @SeoId;
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

CREATE OR ALTER PROCEDURE [seo].[Seo_SeoMetadata_SelectByArticleId]
    @ArticleId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1) *
    FROM [seo].[SeoMetadata]
    WHERE [ArticleId] = @ArticleId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [seo].[Seo_SeoMetadata_SelectAll]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM [seo].[SeoMetadata]
    ORDER BY [UpdatedAt] DESC, [SeoId] DESC;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [seo].[Seo_SeoMetadata_SelectSkipAndTake]
    @Skip               INT = 0,
    @Take               INT = 20,
    @ArticleId          BIGINT = NULL,
    @UpdatedByUserId    BIGINT = NULL,
    @Keyword            NVARCHAR(300) = NULL,
    @SortBy             NVARCHAR(30) = N'UpdatedAt',
    @SortDirection      NVARCHAR(4) = N'DESC'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;
    IF @Take > 200 SET @Take = 200;

    IF @SortBy NOT IN (N'UpdatedAt', N'ArticleId', N'SeoId')
        SET @SortBy = N'UpdatedAt';

    IF UPPER(@SortDirection) NOT IN (N'ASC', N'DESC')
        SET @SortDirection = N'DESC';

    ;WITH [Filtered] AS
    (
        SELECT
            [s].[SeoId],
            [s].[ArticleId],
            [s].[CanonicalUrl],
            [s].[MetaTitle],
            [s].[MetaDescription],
            [s].[OgTitle],
            [s].[OgDescription],
            [s].[OgImageUrl],
            [s].[TwitterTitle],
            [s].[TwitterDescription],
            [s].[TwitterImageUrl],
            [s].[Version],
            [s].[UpdatedAt],
            [s].[UpdatedByUserId],
            COUNT(1) OVER() AS [TotalCount]
        FROM [seo].[SeoMetadata] AS [s]
        WHERE (@ArticleId IS NULL OR [s].[ArticleId] = @ArticleId)
          AND (@UpdatedByUserId IS NULL OR [s].[UpdatedByUserId] = @UpdatedByUserId)
          AND
          (
              @Keyword IS NULL
              OR [s].[CanonicalUrl] LIKE N'%' + @Keyword + N'%'
              OR [s].[MetaTitle] LIKE N'%' + @Keyword + N'%'
              OR [s].[MetaDescription] LIKE N'%' + @Keyword + N'%'
              OR [s].[OgTitle] LIKE N'%' + @Keyword + N'%'
          )
    )
    SELECT *
    FROM [Filtered]
    ORDER BY
        CASE WHEN @SortBy = N'UpdatedAt' AND @SortDirection = N'ASC'  THEN [UpdatedAt] END ASC,
        CASE WHEN @SortBy = N'UpdatedAt' AND @SortDirection = N'DESC' THEN [UpdatedAt] END DESC,
        CASE WHEN @SortBy = N'ArticleId' AND @SortDirection = N'ASC'  THEN [ArticleId] END ASC,
        CASE WHEN @SortBy = N'ArticleId' AND @SortDirection = N'DESC' THEN [ArticleId] END DESC,
        CASE WHEN @SortBy = N'SeoId'     AND @SortDirection = N'ASC'  THEN [SeoId] END ASC,
        CASE WHEN @SortBy = N'SeoId'     AND @SortDirection = N'DESC' THEN [SeoId] END DESC,
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

CREATE OR ALTER PROCEDURE [seo].[Seo_SlugRegistry_Insert]
    @ArticleId              BIGINT,
    @Slug                   NVARCHAR(200),
    @Scope                  VARCHAR(30) = 'public',
    @CanonicalUrl           NVARCHAR(500) = NULL,
    @IsIndexable            BIT = 0,
    @IsActive               BIT = 1,
    @CreatedByUserId        BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 57240, 'ArticleId must be > 0.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Slug, N'')))) = 0
        THROW 57241, 'Slug is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Scope, '')))) = 0
        THROW 57242, 'Scope is required.', 1;

    IF @Scope NOT IN ('public')
        THROW 57243, 'Scope is invalid.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [content].[Article]
        WHERE [ArticleId] = @ArticleId
          AND [IsDeleted] = 0
    )
        THROW 57244, 'Article does not exist or was deleted.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [seo].[SlugRegistry]
        WHERE [Scope] = @Scope
          AND [Slug] = @Slug
    )
        THROW 57245, 'Slug already exists in this scope.', 1;

    IF @IsActive = 1
       AND EXISTS
       (
           SELECT 1
           FROM [seo].[SlugRegistry]
           WHERE [ArticleId] = @ArticleId
             AND [Scope] = @Scope
             AND [IsActive] = 1
       )
        THROW 57246, 'An active slug already exists for this article in this scope.', 1;

    INSERT INTO [seo].[SlugRegistry]
    (
        [ArticleId],
        [Slug],
        [Scope],
        [CanonicalUrl],
        [IsIndexable],
        [IsActive],
        [CreatedByUserId],
        [UpdatedByUserId]
    )
    VALUES
    (
        @ArticleId,
        @Slug,
        @Scope,
        @CanonicalUrl,
        @IsIndexable,
        @IsActive,
        @CreatedByUserId,
        @CreatedByUserId
    );

    SELECT TOP (1) *
    FROM [seo].[SlugRegistry]
    WHERE [SlugId] = SCOPE_IDENTITY();
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [seo].[Seo_SlugRegistry_Update]
    @SlugId                 BIGINT,
    @Slug                   NVARCHAR(200),
    @Scope                  VARCHAR(30),
    @CanonicalUrl           NVARCHAR(500) = NULL,
    @IsIndexable            BIT,
    @IsActive               BIT,
    @UpdatedByUserId        BIGINT = NULL,
    @ExpectedVersion        INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @SlugId IS NULL OR @SlugId <= 0
        THROW 57250, 'SlugId must be > 0.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Slug, N'')))) = 0
        THROW 57251, 'Slug is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Scope, '')))) = 0
        THROW 57252, 'Scope is required.', 1;

    IF @Scope NOT IN ('public')
        THROW 57253, 'Scope is invalid.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [seo].[SlugRegistry]
        WHERE [Scope] = @Scope
          AND [Slug] = @Slug
          AND [SlugId] <> @SlugId
    )
        THROW 57254, 'Slug already exists in this scope.', 1;

    IF @IsActive = 1
       AND EXISTS
       (
           SELECT 1
           FROM [seo].[SlugRegistry] AS [s]
           WHERE [s].[SlugId] <> @SlugId
             AND [s].[IsActive] = 1
             AND [s].[Scope] = @Scope
             AND [s].[ArticleId] =
                 (
                     SELECT TOP (1) [x].[ArticleId]
                     FROM [seo].[SlugRegistry] AS [x]
                     WHERE [x].[SlugId] = @SlugId
                 )
       )
        THROW 57255, 'Another active slug already exists for this article in this scope.', 1;

    UPDATE [seo].[SlugRegistry]
    SET
        [Slug] = @Slug,
        [Scope] = @Scope,
        [CanonicalUrl] = @CanonicalUrl,
        [IsIndexable] = @IsIndexable,
        [IsActive] = @IsActive,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedByUserId] = @UpdatedByUserId,
        [Version] = [Version] + 1
    WHERE [SlugId] = @SlugId
      AND [Version] = @ExpectedVersion;

    IF @@ROWCOUNT = 0
        THROW 57256, 'SlugRegistry update failed. Record not found or version mismatch.', 1;

    SELECT TOP (1) *
    FROM [seo].[SlugRegistry]
    WHERE [SlugId] = @SlugId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [seo].[Seo_SlugRegistry_Activate]
    @SlugId                 BIGINT,
    @UpdatedByUserId        BIGINT = NULL,
    @ExpectedVersion        INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ArticleId BIGINT;
    DECLARE @Scope VARCHAR(30);

    SELECT
        @ArticleId = [ArticleId],
        @Scope = [Scope]
    FROM [seo].[SlugRegistry]
    WHERE [SlugId] = @SlugId;

    IF @ArticleId IS NULL
        THROW 57260, 'SlugRegistry record not found.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [seo].[SlugRegistry]
        WHERE [ArticleId] = @ArticleId
          AND [Scope] = @Scope
          AND [IsActive] = 1
          AND [SlugId] <> @SlugId
    )
        THROW 57261, 'Another active slug already exists for this article in this scope.', 1;

    UPDATE [seo].[SlugRegistry]
    SET
        [IsActive] = 1,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedByUserId] = @UpdatedByUserId,
        [Version] = [Version] + 1
    WHERE [SlugId] = @SlugId
      AND [IsActive] = 0
      AND [Version] = @ExpectedVersion;

    IF @@ROWCOUNT = 0
        THROW 57262, 'Slug activate failed. Record not found, already active, or version mismatch.', 1;

    SELECT TOP (1) *
    FROM [seo].[SlugRegistry]
    WHERE [SlugId] = @SlugId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [seo].[Seo_SlugRegistry_Deactivate]
    @SlugId                 BIGINT,
    @UpdatedByUserId        BIGINT = NULL,
    @ExpectedVersion        INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [seo].[SlugRegistry]
    SET
        [IsActive] = 0,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedByUserId] = @UpdatedByUserId,
        [Version] = [Version] + 1
    WHERE [SlugId] = @SlugId
      AND [IsActive] = 1
      AND [Version] = @ExpectedVersion;

    IF @@ROWCOUNT = 0
        THROW 57270, 'Slug deactivate failed. Record not found, already inactive, or version mismatch.', 1;

    SELECT TOP (1) *
    FROM [seo].[SlugRegistry]
    WHERE [SlugId] = @SlugId;
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

CREATE OR ALTER PROCEDURE [seo].[Seo_SlugRegistry_SelectByArticleId]
    @ArticleId BIGINT,
    @Scope     VARCHAR(30) = NULL,
    @OnlyActive BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM [seo].[SlugRegistry]
    WHERE [ArticleId] = @ArticleId
      AND (@Scope IS NULL OR [Scope] = @Scope)
      AND (@OnlyActive IS NULL OR [IsActive] = @OnlyActive)
    ORDER BY [IsActive] DESC, [UpdatedAt] DESC, [SlugId] DESC;
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

CREATE OR ALTER PROCEDURE [seo].[Seo_SlugRegistry_SelectAll]
    @OnlyActive BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM [seo].[SlugRegistry]
    WHERE (@OnlyActive IS NULL OR [IsActive] = @OnlyActive)
    ORDER BY [UpdatedAt] DESC, [SlugId] DESC;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [seo].[Seo_SlugRegistry_SelectSkipAndTake]
    @Skip               INT = 0,
    @Take               INT = 20,
    @ArticleId          BIGINT = NULL,
    @Scope              VARCHAR(30) = NULL,
    @IsActive           BIT = NULL,
    @IsIndexable        BIT = NULL,
    @Keyword            NVARCHAR(200) = NULL,
    @SortBy             NVARCHAR(30) = N'UpdatedAt',
    @SortDirection      NVARCHAR(4) = N'DESC'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;
    IF @Take > 200 SET @Take = 200;

    IF @SortBy NOT IN (N'UpdatedAt', N'CreatedAt', N'Slug', N'ArticleId')
        SET @SortBy = N'UpdatedAt';

    IF UPPER(@SortDirection) NOT IN (N'ASC', N'DESC')
        SET @SortDirection = N'DESC';

    ;WITH [Filtered] AS
    (
        SELECT
            [s].[SlugId],
            [s].[ArticleId],
            [s].[Slug],
            [s].[Scope],
            [s].[CanonicalUrl],
            [s].[IsIndexable],
            [s].[IsActive],
            [s].[Version],
            [s].[CreatedAt],
            [s].[CreatedByUserId],
            [s].[UpdatedAt],
            [s].[UpdatedByUserId],
            COUNT(1) OVER() AS [TotalCount]
        FROM [seo].[SlugRegistry] AS [s]
        WHERE (@ArticleId IS NULL OR [s].[ArticleId] = @ArticleId)
          AND (@Scope IS NULL OR [s].[Scope] = @Scope)
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
        CASE WHEN @SortBy = N'UpdatedAt' AND @SortDirection = N'ASC'  THEN [UpdatedAt] END ASC,
        CASE WHEN @SortBy = N'UpdatedAt' AND @SortDirection = N'DESC' THEN [UpdatedAt] END DESC,
        CASE WHEN @SortBy = N'CreatedAt' AND @SortDirection = N'ASC'  THEN [CreatedAt] END ASC,
        CASE WHEN @SortBy = N'CreatedAt' AND @SortDirection = N'DESC' THEN [CreatedAt] END DESC,
        CASE WHEN @SortBy = N'Slug'      AND @SortDirection = N'ASC'  THEN [Slug] END ASC,
        CASE WHEN @SortBy = N'Slug'      AND @SortDirection = N'DESC' THEN [Slug] END DESC,
        CASE WHEN @SortBy = N'ArticleId' AND @SortDirection = N'ASC'  THEN [ArticleId] END ASC,
        CASE WHEN @SortBy = N'ArticleId' AND @SortDirection = N'DESC' THEN [ArticleId] END DESC,
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
        CAST(N'Article' AS NVARCHAR(30)) AS [ResourceType],
        [s].[ArticleId] AS [ResourceId],
        [s].[CanonicalUrl],
        [s].[IsIndexable],
        CASE
            WHEN [s].[IsActive] = 1 THEN N'Resolved'
            ELSE N'Inactive'
        END AS [Status],
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

CREATE OR ALTER PROCEDURE [seo].[Seo_SelectMetadataByArticleId]
    @ArticleId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        CAST(N'Article' AS NVARCHAR(30)) AS [ResourceType],
        [a].[ArticleId] AS [ResourceId],
        [sr].[Slug],
        COALESCE([sm].[CanonicalUrl], [sr].[CanonicalUrl]) AS [CanonicalUrl],
        [sm].[MetaTitle],
        [sm].[MetaDescription],
        [sm].[OgTitle],
        [sm].[OgDescription],
        [sm].[OgImageUrl],
        COALESCE([sm].[Version], [sr].[Version]) AS [Version]
    FROM [content].[Article] AS [a]
    LEFT JOIN [seo].[SeoMetadata] AS [sm]
        ON [sm].[ArticleId] = [a].[ArticleId]
    LEFT JOIN [seo].[SlugRegistry] AS [sr]
        ON [sr].[ArticleId] = [a].[ArticleId]
       AND [sr].[IsActive] = 1
       AND [sr].[Scope] = 'public'
    WHERE [a].[ArticleId] = @ArticleId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [seo].[Seo_SelectArticleSeoByArticleId]
    @ArticleId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        [a].[ArticleId],
        [sr].[Scope],
        [sr].[Slug],
        COALESCE([sm].[CanonicalUrl], [sr].[CanonicalUrl]) AS [CanonicalUrl],
        [sm].[MetaTitle],
        [sm].[MetaDescription],
        [sm].[OgTitle],
        [sm].[OgDescription],
        [sm].[OgImageUrl],
        [sm].[TwitterTitle],
        [sm].[TwitterDescription],
        [sm].[TwitterImageUrl],
        CASE
            WHEN [sm].[Version] IS NULL AND [sr].[Version] IS NULL THEN 0
            WHEN [sm].[Version] IS NULL THEN [sr].[Version]
            WHEN [sr].[Version] IS NULL THEN [sm].[Version]
            WHEN [sm].[Version] >= [sr].[Version] THEN [sm].[Version]
            ELSE [sr].[Version]
        END AS [Version],
        [sr].[IsIndexable],
        [sr].[IsActive]
    FROM [content].[Article] AS [a]
    LEFT JOIN [seo].[SeoMetadata] AS [sm]
        ON [sm].[ArticleId] = [a].[ArticleId]
    LEFT JOIN [seo].[SlugRegistry] AS [sr]
        ON [sr].[ArticleId] = [a].[ArticleId]
       AND [sr].[IsActive] = 1
       AND [sr].[Scope] = 'public'
    WHERE [a].[ArticleId] = @ArticleId;
END
GO