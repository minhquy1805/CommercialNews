/*
  File: db/10_modules/content/020_procs.sql
  Module: Content
  Purpose:
  - Create stored procedures for Content truth model in CommercialNews V1.
  - Support:
      * Category CRUD (soft delete / restore)
      * Tag CRUD (soft delete / restore)
      * Article CRUD + lifecycle transitions without Article restore in V1
      * ArticleTag attach / detach / query
      * ArticleRevision insert / query
      * ArticleLifecycleEvent insert / query
      * admin paging/filtering for articles
  - Idempotent: safe to re-run.

  Notes:
  - Truth remains in [content] tables.
  - Public read model / projections / cache / search are outside this file.
  - App layer should still orchestrate transaction boundaries where multiple procs
    must succeed atomically (for example: update article + insert revision + insert lifecycle event).
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 54201, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

USE [CommercialNews];
GO

IF SCHEMA_ID(N'content') IS NULL
BEGIN
    THROW 54202, 'Schema [content] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

/* =========================================================
   CATEGORY
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_Category_Insert]
    @PublicId            CHAR(26),
    @ParentCategoryId    BIGINT = NULL,
    @Name                NVARCHAR(200),
    @NameNormalized      NVARCHAR(200),
    @Description         NVARCHAR(1000) = NULL,
    @IsActive            BIT = 1,
    @DisplayOrder        INT = 0,
    @CreatedByUserId     BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@PublicId, '')))) = 0
        THROW 54210, 'Category PublicId is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Name, N'')))) = 0
        THROW 54211, 'Category Name is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@NameNormalized, N'')))) = 0
        THROW 54212, 'Category NameNormalized is required.', 1;

    IF @DisplayOrder < 0
        THROW 54213, 'Category DisplayOrder must be >= 0.', 1;

    IF @ParentCategoryId IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1
           FROM [content].[Category]
           WHERE [CategoryId] = @ParentCategoryId
             AND [IsDeleted] = 0
       )
        THROW 54214, 'Parent category does not exist or was deleted.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [content].[Category]
        WHERE [NameNormalized] = @NameNormalized
    )
        THROW 54215, 'Category NameNormalized already exists.', 1;

    INSERT INTO [content].[Category]
    (
        [PublicId],
        [ParentCategoryId],
        [Name],
        [NameNormalized],
        [Description],
        [IsActive],
        [DisplayOrder],
        [CreatedByUserId],
        [UpdatedByUserId]
    )
    VALUES
    (
        @PublicId,
        @ParentCategoryId,
        @Name,
        @NameNormalized,
        @Description,
        @IsActive,
        @DisplayOrder,
        @CreatedByUserId,
        @CreatedByUserId
    );

    SELECT TOP (1) *
    FROM [content].[Category]
    WHERE [CategoryId] = SCOPE_IDENTITY();
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_Category_Update]
    @CategoryId          BIGINT,
    @ParentCategoryId    BIGINT = NULL,
    @Name                NVARCHAR(200),
    @NameNormalized      NVARCHAR(200),
    @Description         NVARCHAR(1000) = NULL,
    @IsActive            BIT,
    @DisplayOrder        INT,
    @UpdatedByUserId     BIGINT = NULL,
    @ExpectedVersion     BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @CategoryId IS NULL OR @CategoryId <= 0
        THROW 54220, 'CategoryId must be > 0.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Name, N'')))) = 0
        THROW 54221, 'Category Name is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@NameNormalized, N'')))) = 0
        THROW 54222, 'Category NameNormalized is required.', 1;

    IF @DisplayOrder < 0
        THROW 54223, 'Category DisplayOrder must be >= 0.', 1;

    IF @ParentCategoryId = @CategoryId
        THROW 54224, 'Category cannot be its own parent.', 1;

    IF @ParentCategoryId IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1
           FROM [content].[Category]
           WHERE [CategoryId] = @ParentCategoryId
             AND [IsDeleted] = 0
       )
        THROW 54225, 'Parent category does not exist or was deleted.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [content].[Category]
        WHERE [NameNormalized] = @NameNormalized
          AND [CategoryId] <> @CategoryId
    )
        THROW 54226, 'Category NameNormalized already exists.', 1;

    UPDATE [content].[Category]
    SET
        [ParentCategoryId] = @ParentCategoryId,
        [Name] = @Name,
        [NameNormalized] = @NameNormalized,
        [Description] = @Description,
        [IsActive] = @IsActive,
        [DisplayOrder] = @DisplayOrder,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedByUserId] = @UpdatedByUserId,
        [Version] = [Version] + 1
    WHERE [CategoryId] = @CategoryId
      AND [IsDeleted] = 0
      AND [Version] = @ExpectedVersion;

    IF @@ROWCOUNT = 0
        THROW 54227, 'Category update failed. Record not found, deleted, or version mismatch.', 1;

    SELECT TOP (1) *
    FROM [content].[Category]
    WHERE [CategoryId] = @CategoryId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_Category_SoftDelete]
    @CategoryId          BIGINT,
    @DeletedByUserId     BIGINT = NULL,
    @ExpectedVersion     BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF EXISTS
    (
        SELECT 1
        FROM [content].[Article]
        WHERE [CategoryId] = @CategoryId
          AND [IsDeleted] = 0
    )
        THROW 54230, 'Cannot delete category because active articles still reference it.', 1;

    UPDATE [content].[Category]
    SET
        [IsDeleted] = 1,
        [IsActive] = 0,
        [DeletedAt] = SYSUTCDATETIME(),
        [DeletedByUserId] = @DeletedByUserId,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedByUserId] = @DeletedByUserId,
        [Version] = [Version] + 1
    WHERE [CategoryId] = @CategoryId
    AND [IsDeleted] = 0
    AND [Version] = @ExpectedVersion;

    IF @@ROWCOUNT = 0
        THROW 54231, 'Category delete failed. Record not found, already deleted, or version mismatch.', 1;

    SELECT TOP (1) *
    FROM [content].[Category]
    WHERE [CategoryId] = @CategoryId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_Category_Restore]
    @CategoryId          BIGINT,
    @UpdatedByUserId     BIGINT = NULL,
    @ExpectedVersion     BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [content].[Category]
    SET
        [IsDeleted] = 0,
        [IsActive] = 1,
        [DeletedAt] = NULL,
        [DeletedByUserId] = NULL,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedByUserId] = @UpdatedByUserId,
        [Version] = [Version] + 1
    WHERE [CategoryId] = @CategoryId
    AND [IsDeleted] = 1
    AND [Version] = @ExpectedVersion;

    IF @@ROWCOUNT = 0
        THROW 54232, 'Category restore failed. Record not found, not deleted, or version mismatch.', 1;

    SELECT TOP (1) *
    FROM [content].[Category]
    WHERE [CategoryId] = @CategoryId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_Category_SelectById]
    @CategoryId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1) *
    FROM [content].[Category]
    WHERE [CategoryId] = @CategoryId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_Category_SelectAll]
    @IncludeDeleted BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM [content].[Category]
    WHERE (@IncludeDeleted = 1 OR [IsDeleted] = 0)
    ORDER BY [DisplayOrder] ASC, [Name] ASC, [CategoryId] ASC;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_Category_SelectSkipAndTake]
    @Skip               INT = 0,
    @Take               INT = 20,
    @Keyword            NVARCHAR(200) = NULL,
    @ParentCategoryId   BIGINT = NULL,
    @IsActive           BIT = NULL,
    @IsDeleted          BIT = 0,
    @SortBy             NVARCHAR(30) = N'DisplayOrder',
    @SortDirection      NVARCHAR(4) = N'ASC'
AS
BEGIN
    SET NOCOUNT ON;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;
    IF @Take > 200 SET @Take = 200;

    IF @SortBy NOT IN (N'CreatedAt', N'UpdatedAt', N'Name', N'DisplayOrder')
        SET @SortBy = N'DisplayOrder';

    IF UPPER(@SortDirection) NOT IN (N'ASC', N'DESC')
        SET @SortDirection = N'ASC';

    ;WITH [Filtered] AS
    (
        SELECT
            [c].[CategoryId],
            [c].[PublicId],
            [c].[ParentCategoryId],
            [c].[Name],
            [c].[NameNormalized],
            [c].[Description],
            [c].[IsActive],
            [c].[DisplayOrder],
            [c].[CreatedAt],
            [c].[UpdatedAt],
            [c].[CreatedByUserId],
            [c].[UpdatedByUserId],
            [c].[IsDeleted],
            [c].[DeletedAt],
            [c].[DeletedByUserId],
            [c].[Version],
            COUNT(1) OVER() AS [TotalCount]
        FROM [content].[Category] AS [c]
        WHERE [c].[IsDeleted] = @IsDeleted
          AND (@ParentCategoryId IS NULL OR [c].[ParentCategoryId] = @ParentCategoryId)
          AND (@IsActive IS NULL OR [c].[IsActive] = @IsActive)
          AND
          (
              @Keyword IS NULL
              OR [c].[Name] LIKE N'%' + @Keyword + N'%'
              OR [c].[Description] LIKE N'%' + @Keyword + N'%'
          )
    )
    SELECT *
    FROM [Filtered]
    ORDER BY
        CASE WHEN @SortBy = N'CreatedAt'    AND @SortDirection = N'ASC'  THEN [CreatedAt] END ASC,
        CASE WHEN @SortBy = N'CreatedAt'    AND @SortDirection = N'DESC' THEN [CreatedAt] END DESC,
        CASE WHEN @SortBy = N'UpdatedAt'    AND @SortDirection = N'ASC'  THEN [UpdatedAt] END ASC,
        CASE WHEN @SortBy = N'UpdatedAt'    AND @SortDirection = N'DESC' THEN [UpdatedAt] END DESC,
        CASE WHEN @SortBy = N'Name'         AND @SortDirection = N'ASC'  THEN [Name] END ASC,
        CASE WHEN @SortBy = N'Name'         AND @SortDirection = N'DESC' THEN [Name] END DESC,
        CASE WHEN @SortBy = N'DisplayOrder' AND @SortDirection = N'ASC'  THEN [DisplayOrder] END ASC,
        CASE WHEN @SortBy = N'DisplayOrder' AND @SortDirection = N'DESC' THEN [DisplayOrder] END DESC,
        [CategoryId] DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END
GO

/* =========================================================
   TAG
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_Tag_Insert]
    @PublicId            CHAR(26),
    @Name                NVARCHAR(150),
    @NameNormalized      NVARCHAR(150),
    @Description         NVARCHAR(500) = NULL,
    @IsActive            BIT = 1,
    @CreatedByUserId     BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@PublicId, '')))) = 0
        THROW 54240, 'Tag PublicId is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Name, N'')))) = 0
        THROW 54241, 'Tag Name is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@NameNormalized, N'')))) = 0
        THROW 54242, 'Tag NameNormalized is required.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [content].[Tag]
        WHERE [NameNormalized] = @NameNormalized
    )
        THROW 54243, 'Tag NameNormalized already exists.', 1;

    INSERT INTO [content].[Tag]
    (
        [PublicId],
        [Name],
        [NameNormalized],
        [Description],
        [IsActive],
        [CreatedByUserId],
        [UpdatedByUserId]
    )
    VALUES
    (
        @PublicId,
        @Name,
        @NameNormalized,
        @Description,
        @IsActive,
        @CreatedByUserId,
        @CreatedByUserId
    );

    SELECT TOP (1) *
    FROM [content].[Tag]
    WHERE [TagId] = SCOPE_IDENTITY();
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_Tag_Update]
    @TagId               BIGINT,
    @Name                NVARCHAR(150),
    @NameNormalized      NVARCHAR(150),
    @Description         NVARCHAR(500) = NULL,
    @IsActive            BIT,
    @UpdatedByUserId     BIGINT = NULL,
    @ExpectedVersion     BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @TagId IS NULL OR @TagId <= 0
        THROW 54250, 'TagId must be > 0.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Name, N'')))) = 0
        THROW 54251, 'Tag Name is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@NameNormalized, N'')))) = 0
        THROW 54252, 'Tag NameNormalized is required.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [content].[Tag]
        WHERE [NameNormalized] = @NameNormalized
          AND [TagId] <> @TagId
    )
        THROW 54253, 'Tag NameNormalized already exists.', 1;

    UPDATE [content].[Tag]
    SET
        [Name] = @Name,
        [NameNormalized] = @NameNormalized,
        [Description] = @Description,
        [IsActive] = @IsActive,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedByUserId] = @UpdatedByUserId,
        [Version] = [Version] + 1
    WHERE [TagId] = @TagId
      AND [IsDeleted] = 0
      AND [Version] = @ExpectedVersion;

    IF @@ROWCOUNT = 0
        THROW 54254, 'Tag update failed. Record not found, deleted, or version mismatch.', 1;

    SELECT TOP (1) *
    FROM [content].[Tag]
    WHERE [TagId] = @TagId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_Tag_SoftDelete]
    @TagId               BIGINT,
    @DeletedByUserId     BIGINT = NULL,
    @ExpectedVersion     BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [content].[Tag]
    SET
        [IsDeleted] = 1,
        [IsActive] = 0,
        [DeletedAt] = SYSUTCDATETIME(),
        [DeletedByUserId] = @DeletedByUserId,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedByUserId] = @DeletedByUserId,
        [Version] = [Version] + 1
    WHERE [TagId] = @TagId
    AND [IsDeleted] = 0
    AND [Version] = @ExpectedVersion;

    IF @@ROWCOUNT = 0
        THROW 54255, 'Tag delete failed. Record not found, already deleted, or version mismatch.', 1;

    SELECT TOP (1) *
    FROM [content].[Tag]
    WHERE [TagId] = @TagId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_Tag_Restore]
    @TagId               BIGINT,
    @UpdatedByUserId     BIGINT = NULL,
    @ExpectedVersion     BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [content].[Tag]
    SET
        [IsDeleted] = 0,
        [IsActive] = 1,
        [DeletedAt] = NULL,
        [DeletedByUserId] = NULL,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedByUserId] = @UpdatedByUserId,
        [Version] = [Version] + 1
    WHERE [TagId] = @TagId
    AND [IsDeleted] = 1
    AND [Version] = @ExpectedVersion;

    IF @@ROWCOUNT = 0
        THROW 54256, 'Tag restore failed. Record not found, not deleted, or version mismatch.', 1;

    SELECT TOP (1) *
    FROM [content].[Tag]
    WHERE [TagId] = @TagId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_Tag_SelectById]
    @TagId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1) *
    FROM [content].[Tag]
    WHERE [TagId] = @TagId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_Tag_SelectAll]
    @IncludeDeleted BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM [content].[Tag]
    WHERE (@IncludeDeleted = 1 OR [IsDeleted] = 0)
    ORDER BY [Name] ASC, [TagId] ASC;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_Tag_SelectSkipAndTake]
    @Skip               INT = 0,
    @Take               INT = 20,
    @Keyword            NVARCHAR(150) = NULL,
    @IsActive           BIT = NULL,
    @IsDeleted          BIT = 0,
    @SortBy             NVARCHAR(30) = N'Name',
    @SortDirection      NVARCHAR(4) = N'ASC'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Skip < 0
        SET @Skip = 0;

    IF @Take <= 0
        SET @Take = 20;

    IF @Take > 200
        SET @Take = 200;

    IF @SortBy NOT IN (N'Name', N'CreatedAt', N'UpdatedAt')
        SET @SortBy = N'Name';

    IF UPPER(@SortDirection) NOT IN (N'ASC', N'DESC')
        SET @SortDirection = N'ASC';

    ;WITH [Filtered] AS
    (
        SELECT
            [t].[TagId],
            [t].[PublicId],
            [t].[Name],
            [t].[NameNormalized],
            [t].[Description],
            [t].[IsActive],
            [t].[CreatedAt],
            [t].[UpdatedAt],
            [t].[CreatedByUserId],
            [t].[UpdatedByUserId],
            [t].[IsDeleted],
            [t].[DeletedAt],
            [t].[DeletedByUserId],
            [t].[Version],
            COUNT(1) OVER() AS [TotalCount]
        FROM [content].[Tag] AS [t]
        WHERE [t].[IsDeleted] = @IsDeleted
          AND (@IsActive IS NULL OR [t].[IsActive] = @IsActive)
          AND
          (
              @Keyword IS NULL
              OR [t].[Name] LIKE N'%' + @Keyword + N'%'
              OR [t].[NameNormalized] LIKE N'%' + @Keyword + N'%'
          )
    )
    SELECT *
    FROM [Filtered]
    ORDER BY
        CASE WHEN @SortBy = N'Name'      AND @SortDirection = N'ASC'  THEN [Name] END ASC,
        CASE WHEN @SortBy = N'Name'      AND @SortDirection = N'DESC' THEN [Name] END DESC,
        CASE WHEN @SortBy = N'CreatedAt' AND @SortDirection = N'ASC'  THEN [CreatedAt] END ASC,
        CASE WHEN @SortBy = N'CreatedAt' AND @SortDirection = N'DESC' THEN [CreatedAt] END DESC,
        CASE WHEN @SortBy = N'UpdatedAt' AND @SortDirection = N'ASC'  THEN [UpdatedAt] END ASC,
        CASE WHEN @SortBy = N'UpdatedAt' AND @SortDirection = N'DESC' THEN [UpdatedAt] END DESC,
        [TagId] DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END
GO

/* =========================================================
   ARTICLE
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_Article_Insert]
    @ArticlePublicId     CHAR(26),
    @CategoryId          BIGINT,
    @AuthorUserId        BIGINT,
    @Title               NVARCHAR(300),
    @Summary             NVARCHAR(1000),
    @Body                NVARCHAR(MAX),
    @CoverMediaId        BIGINT = NULL,
    @CreatedByUserId     BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@ArticlePublicId, '')))) <> 26
        THROW 54260, 'ArticlePublicId must be a 26-character ULID.', 1;

    IF @AuthorUserId IS NULL OR @AuthorUserId <= 0
        THROW 54261, 'AuthorUserId must be > 0.', 1;

    IF @CreatedByUserId IS NULL OR @CreatedByUserId <= 0
        THROW 54268, 'CreatedByUserId must be > 0.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Title, N'')))) = 0
        THROW 54262, 'Article Title is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Summary, N'')))) = 0
        THROW 54263, 'Article Summary is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Body, N'')))) = 0
        THROW 54264, 'Article Body is required.', 1;

    IF @CategoryId IS NULL OR @CategoryId <= 0
        THROW 54265, 'CategoryId must be > 0.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [content].[Category]
        WHERE [CategoryId] = @CategoryId
          AND [IsDeleted] = 0
          AND [IsActive] = 1
    )
        THROW 54266, 'Category does not exist, is deleted, or inactive.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [content].[Article]
        WHERE [ArticlePublicId] = @ArticlePublicId
    )
        THROW 54267, 'ArticlePublicId already exists.', 1;

    INSERT INTO [content].[Article]
    (
        [ArticlePublicId],
        [CategoryId],
        [AuthorUserId],
        [Title],
        [Summary],
        [Body],
        [Status],
        [PublishedAt],
        [UnpublishedAt],
        [ArchivedAt],
        [CoverMediaId],
        [CreatedByUserId],
        [UpdatedByUserId]
    )
    VALUES
    (
        @ArticlePublicId,
        @CategoryId,
        @AuthorUserId,
        @Title,
        @Summary,
        @Body,
        N'Draft',
        NULL,
        NULL,
        NULL,
        @CoverMediaId,
        @CreatedByUserId,
        @CreatedByUserId
    );

    SELECT TOP (1) *
    FROM [content].[Article]
    WHERE [ArticleId] = SCOPE_IDENTITY();
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_Article_Update]
    @ArticleId           BIGINT,
    @CategoryId          BIGINT,
    @Title               NVARCHAR(300),
    @Summary             NVARCHAR(1000),
    @Body                NVARCHAR(MAX),
    @CoverMediaId        BIGINT = NULL,
    @UpdatedByUserId     BIGINT = NULL,
    @ExpectedVersion     BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 54270, 'ArticleId must be > 0.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Title, N'')))) = 0
        THROW 54271, 'Article Title is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Summary, N'')))) = 0
        THROW 54272, 'Article Summary is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Body, N'')))) = 0
        THROW 54273, 'Article Body is required.', 1;

    IF @CategoryId IS NULL OR @CategoryId <= 0
        THROW 54274, 'CategoryId must be > 0.', 1;

    IF NOT EXISTS
       (
           SELECT 1
           FROM [content].[Category]
           WHERE [CategoryId] = @CategoryId
             AND [IsDeleted] = 0
             AND [IsActive] = 1
       )
        THROW 54275, 'Category does not exist, is deleted, or inactive.', 1;

    UPDATE [content].[Article]
    SET
        [CategoryId] = @CategoryId,
        [Title] = @Title,
        [Summary] = @Summary,
        [Body] = @Body,
        [CoverMediaId] = @CoverMediaId,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedByUserId] = @UpdatedByUserId,
        [Version] = [Version] + 1
    WHERE [ArticleId] = @ArticleId
      AND [IsDeleted] = 0
      AND [Status] = N'Draft'
      AND [Version] = @ExpectedVersion;

    IF @@ROWCOUNT = 0
        THROW 54276, 'Article update failed. Record not found, deleted, not draft, or version mismatch.', 1;

    SELECT TOP (1) *
    FROM [content].[Article]
    WHERE [ArticleId] = @ArticleId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_Article_SelectById]
    @ArticleId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1) *
    FROM [content].[Article]
    WHERE [ArticleId] = @ArticleId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_Article_SelectByPublicId]
    @ArticlePublicId CHAR(26)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1) *
    FROM [content].[Article]
    WHERE [ArticlePublicId] = @ArticlePublicId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_Article_SelectSkipAndTake]
    @Skip               INT = 0,
    @Take               INT = 20,
    @Keyword            NVARCHAR(300) = NULL,
    @Status             NVARCHAR(30) = NULL,
    @CategoryId         BIGINT = NULL,
    @AuthorUserId       BIGINT = NULL,
    @IsDeleted          BIT = 0,
    @SortBy             NVARCHAR(30) = N'UpdatedAt',
    @SortDirection      NVARCHAR(4) = N'DESC'
AS
BEGIN
    SET NOCOUNT ON;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;
    IF @Take > 200 SET @Take = 200;

    IF @SortBy NOT IN (N'CreatedAt', N'UpdatedAt', N'PublishedAt', N'Title')
        SET @SortBy = N'UpdatedAt';

    IF UPPER(@SortDirection) NOT IN (N'ASC', N'DESC')
        SET @SortDirection = N'DESC';

    ;WITH [Filtered] AS
    (
        SELECT
            [a].[ArticleId],
            [a].[ArticlePublicId],
            [a].[CategoryId],
            [a].[AuthorUserId],
            [a].[Title],
            [a].[Summary],
            [a].[Body],
            [a].[Status],
            [a].[PublishedAt],
            [a].[UnpublishedAt],
            [a].[ArchivedAt],
            [a].[CoverMediaId],
            [a].[CreatedAt],
            [a].[UpdatedAt],
            [a].[CreatedByUserId],
            [a].[UpdatedByUserId],
            [a].[IsDeleted],
            [a].[DeletedAt],
            [a].[DeletedByUserId],
            [a].[Version],
            COUNT(1) OVER() AS [TotalCount]
        FROM [content].[Article] AS [a]
        WHERE [a].[IsDeleted] = @IsDeleted
          AND (@Status IS NULL OR [a].[Status] = @Status)
          AND (@CategoryId IS NULL OR [a].[CategoryId] = @CategoryId)
          AND (@AuthorUserId IS NULL OR [a].[AuthorUserId] = @AuthorUserId)
          AND
          (
              @Keyword IS NULL
              OR [a].[Title] LIKE N'%' + @Keyword + N'%'
              OR [a].[Summary] LIKE N'%' + @Keyword + N'%'
          )
    )
    SELECT *
    FROM [Filtered]
    ORDER BY
        CASE WHEN @SortBy = N'CreatedAt'   AND @SortDirection = N'ASC'  THEN [CreatedAt] END ASC,
        CASE WHEN @SortBy = N'CreatedAt'   AND @SortDirection = N'DESC' THEN [CreatedAt] END DESC,
        CASE WHEN @SortBy = N'UpdatedAt'   AND @SortDirection = N'ASC'  THEN [UpdatedAt] END ASC,
        CASE WHEN @SortBy = N'UpdatedAt'   AND @SortDirection = N'DESC' THEN [UpdatedAt] END DESC,
        CASE WHEN @SortBy = N'PublishedAt' AND @SortDirection = N'ASC'  THEN [PublishedAt] END ASC,
        CASE WHEN @SortBy = N'PublishedAt' AND @SortDirection = N'DESC' THEN [PublishedAt] END DESC,
        CASE WHEN @SortBy = N'Title'       AND @SortDirection = N'ASC'  THEN [Title] END ASC,
        CASE WHEN @SortBy = N'Title'       AND @SortDirection = N'DESC' THEN [Title] END DESC,
        [ArticleId] DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_Article_Publish]
    @ArticleId           BIGINT,
    @ActorUserId         BIGINT = NULL,
    @ExpectedVersion     BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF EXISTS
    (
        SELECT 1
        FROM [content].[Article]
        WHERE [ArticleId] = @ArticleId
          AND [IsDeleted] = 1
    )
        THROW 54280, 'Cannot publish deleted article.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [content].[Article]
        WHERE [ArticleId] = @ArticleId
          AND [Status] = N'Archived'
          AND [IsDeleted] = 0
    )
        THROW 54281, 'Cannot publish archived article.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [content].[Article]
        WHERE [ArticleId] = @ArticleId
          AND [IsDeleted] = 0
          AND
          (
              LEN(LTRIM(RTRIM(ISNULL([Title], N'')))) = 0
              OR LEN(LTRIM(RTRIM(ISNULL([Summary], N'')))) = 0
              OR LEN(LTRIM(RTRIM(ISNULL([Body], N'')))) = 0
          )
    )
        THROW 54282, 'Cannot publish article without title/summary/body.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [content].[Article] AS [a]
        INNER JOIN [content].[Category] AS [c]
            ON [c].[CategoryId] = [a].[CategoryId]
        WHERE [a].[ArticleId] = @ArticleId
        AND [a].[IsDeleted] = 0
        AND [c].[IsDeleted] = 0
        AND [c].[IsActive] = 1
    )
        THROW 54269, 'Cannot publish article because category is deleted or inactive.', 1;

    UPDATE [content].[Article]
    SET
        [Status] = N'Published',
        [PublishedAt] = SYSUTCDATETIME(),
        [ArchivedAt] = NULL,
        [UnpublishedAt] = NULL,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedByUserId] = @ActorUserId,
        [Version] = [Version] + 1
    WHERE [ArticleId] = @ArticleId
      AND [IsDeleted] = 0
      AND [Version] = @ExpectedVersion
      AND [Status] = N'Draft';

    IF @@ROWCOUNT = 0
        THROW 54283, 'Article publish failed. Record not found, not draft, deleted, or version mismatch.', 1;

    SELECT TOP (1) *
    FROM [content].[Article]
    WHERE [ArticleId] = @ArticleId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_Article_Unpublish]
    @ArticleId           BIGINT,
    @ActorUserId         BIGINT = NULL,
    @ExpectedVersion     BIGINT,
    @Reason              NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 54284, 'ArticleId must be > 0.', 1;

    IF @ExpectedVersion IS NULL OR @ExpectedVersion <= 0
        THROW 54285, 'ExpectedVersion must be > 0.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Reason, N'')))) = 0
        THROW 54286, 'Reason is required for Unpublish.', 1;

    UPDATE [content].[Article]
    SET
        [Status] = N'Draft',
        [UnpublishedAt] = SYSUTCDATETIME(),
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedByUserId] = @ActorUserId,
        [Version] = [Version] + 1
    WHERE [ArticleId] = @ArticleId
      AND [IsDeleted] = 0
      AND [Version] = @ExpectedVersion
      AND [Status] = N'Published';

    IF @@ROWCOUNT = 0
        THROW 54287, 'Article unpublish failed. Record not found, not published, deleted, or version mismatch.', 1;

    SELECT TOP (1) *
    FROM [content].[Article]
    WHERE [ArticleId] = @ArticleId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_Article_Archive]
    @ArticleId           BIGINT,
    @ActorUserId         BIGINT = NULL,
    @ExpectedVersion     BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [content].[Article]
    SET
        [Status] = N'Archived',
        [ArchivedAt] = SYSUTCDATETIME(),
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedByUserId] = @ActorUserId,
        [Version] = [Version] + 1
    WHERE [ArticleId] = @ArticleId
      AND [IsDeleted] = 0
      AND [Version] = @ExpectedVersion
      AND [Status] IN (N'Draft', N'Published');

    IF @@ROWCOUNT = 0
        THROW 54288, 'Article archive failed. Record not found, not draft/published, deleted, or version mismatch.', 1;

    SELECT TOP (1) *
    FROM [content].[Article]
    WHERE [ArticleId] = @ArticleId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_Article_SoftDelete]
    @ArticleId           BIGINT,
    @DeletedByUserId     BIGINT = NULL,
    @ExpectedVersion     BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [content].[Article]
    SET
        [IsDeleted] = 1,
        [DeletedAt] = SYSUTCDATETIME(),
        [DeletedByUserId] = @DeletedByUserId,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedByUserId] = @DeletedByUserId,
        [Version] = [Version] + 1
    WHERE [ArticleId] = @ArticleId
      AND [IsDeleted] = 0
      AND [Version] = @ExpectedVersion;

    IF @@ROWCOUNT = 0
        THROW 54289, 'Article soft-delete failed. Record not found, already soft-deleted, or version mismatch.', 1;

    SELECT TOP (1) *
    FROM [content].[Article]
    WHERE [ArticleId] = @ArticleId;
END
GO

/* =========================================================
   ARTICLE TAG
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_ArticleTag_Insert]
    @ArticleId           BIGINT,
    @TagId               BIGINT,
    @AttachedByUserId    BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 54290, 'ArticleId must be > 0.', 1;

    IF @TagId IS NULL OR @TagId <= 0
        THROW 54291, 'TagId must be > 0.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [content].[Article]
        WHERE [ArticleId] = @ArticleId
          AND [IsDeleted] = 0
          AND [Status] = N'Draft'
    )
        THROW 54292, 'Article does not exist, was deleted, or is not draft.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [content].[Tag]
        WHERE [TagId] = @TagId
          AND [IsDeleted] = 0
          AND [IsActive] = 1
    )
        THROW 54293, 'Tag does not exist, is deleted, or inactive.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [content].[ArticleTag]
        WHERE [ArticleId] = @ArticleId
          AND [TagId] = @TagId
    )
        THROW 54294, 'ArticleTag already exists.', 1;

    INSERT INTO [content].[ArticleTag]
    (
        [ArticleId],
        [TagId],
        [AttachedByUserId]
    )
    VALUES
    (
        @ArticleId,
        @TagId,
        @AttachedByUserId
    );

    SELECT *
    FROM [content].[ArticleTag]
    WHERE [ArticleId] = @ArticleId
      AND [TagId] = @TagId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_ArticleTag_DeleteByArticleIdAndTagId]
    @ArticleId BIGINT,
    @TagId     BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 54295, 'ArticleId must be > 0.', 1;

    IF @TagId IS NULL OR @TagId <= 0
        THROW 54296, 'TagId must be > 0.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [content].[Article]
        WHERE [ArticleId] = @ArticleId
        AND [IsDeleted] = 0
        AND [Status] = N'Draft'
    )
        THROW 54297, 'Article does not exist, was deleted, or is not draft.', 1;

    DELETE FROM [content].[ArticleTag]
    WHERE [ArticleId] = @ArticleId
      AND [TagId] = @TagId;

    SELECT CAST(@@ROWCOUNT AS INT) AS [RowsAffected];
END
GO

CREATE OR ALTER PROCEDURE [content].[Content_ArticleTag_DeleteAllByArticleId]
    @ArticleId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 54298, 'ArticleId must be > 0.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [content].[Article]
        WHERE [ArticleId] = @ArticleId
        AND [IsDeleted] = 0
        AND [Status] = N'Draft'
    )
        THROW 54299, 'Article does not exist, was deleted, or is not draft.', 1;

    DELETE FROM [content].[ArticleTag]
    WHERE [ArticleId] = @ArticleId;

    SELECT CAST(@@ROWCOUNT AS INT) AS [RowsAffected];
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_ArticleTag_SelectByArticleId]
    @ArticleId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [at].[ArticleId],
        [at].[TagId],
        [at].[AttachedAt],
        [at].[AttachedByUserId],
        [t].[PublicId] AS [TagPublicId],
        [t].[Name],
        [t].[NameNormalized],
        [t].[Description],
        [t].[IsActive],
        [t].[IsDeleted]
    FROM [content].[ArticleTag] AS [at]
    INNER JOIN [content].[Tag] AS [t]
        ON [t].[TagId] = [at].[TagId]
    WHERE [at].[ArticleId] = @ArticleId
    ORDER BY [t].[Name] ASC, [at].[TagId] ASC;
END
GO

/* =========================================================
   ARTICLE REVISION
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_ArticleRevision_Insert]
    @ArticleId               BIGINT,
    @EditedByUserId          BIGINT,
    @ArticleVersion          BIGINT = NULL,
    @CorrelationId           NVARCHAR(100) = NULL,
    @ChangeSummary           NVARCHAR(300) = NULL,
    @OldTitle                NVARCHAR(300) = NULL,
    @OldSummary              NVARCHAR(1000) = NULL,
    @OldBody                 NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [content].[Article]
        WHERE [ArticleId] = @ArticleId
    )
        THROW 54300, 'Article does not exist for revision.', 1;

    IF @EditedByUserId IS NULL OR @EditedByUserId <= 0
        THROW 54301, 'EditedByUserId must be > 0.', 1;

    IF @ArticleVersion IS NOT NULL AND @ArticleVersion <= 0
        THROW 54302, 'ArticleVersion must be > 0 when provided.', 1;

    IF @OldTitle IS NULL
       AND @OldSummary IS NULL
       AND @OldBody IS NULL
        THROW 54303, 'ArticleRevision requires at least one previous value.', 1;

    INSERT INTO [content].[ArticleRevision]
    (
        [ArticleId],
        [EditedByUserId],
        [ArticleVersion],
        [CorrelationId],
        [ChangeSummary],
        [OldTitle],
        [OldSummary],
        [OldBody]
    )
    VALUES
    (
        @ArticleId,
        @EditedByUserId,
        @ArticleVersion,
        @CorrelationId,
        @ChangeSummary,
        @OldTitle,
        @OldSummary,
        @OldBody
    );

    SELECT TOP (1) *
    FROM [content].[ArticleRevision]
    WHERE [RevisionId] = SCOPE_IDENTITY();
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_ArticleRevision_SelectByArticleId]
    @ArticleId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM [content].[ArticleRevision]
    WHERE [ArticleId] = @ArticleId
    ORDER BY [EditedAt] DESC, [RevisionId] DESC;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_ArticleRevision_SelectById]
    @ArticleId BIGINT,
    @RevisionId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SELECT TOP (1) *
    FROM [content].[ArticleRevision]
    WHERE [ArticleId] = @ArticleId
      AND [RevisionId] = @RevisionId;
END
GO

/* =========================================================
   ARTICLE LIFECYCLE EVENT
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_ArticleLifecycleEvent_Insert]
    @ArticleId           BIGINT,
    @ArticleVersion      BIGINT,
    @ActionType          NVARCHAR(30),
    @FromStatus          NVARCHAR(30) = NULL,
    @ToStatus            NVARCHAR(30) = NULL,
    @Reason              NVARCHAR(500) = NULL,
    @ActorUserId         BIGINT,
    @CorrelationId       NVARCHAR(100) = NULL,
    @MetadataJson        NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [content].[Article]
        WHERE [ArticleId] = @ArticleId
    )
        THROW 54310, 'Article does not exist for lifecycle event.', 1;

    IF @ArticleVersion IS NULL OR @ArticleVersion <= 0
        THROW 54315, 'ArticleVersion must be > 0.', 1;

    IF @ActorUserId IS NULL OR @ActorUserId <= 0
        THROW 54316, 'ActorUserId must be > 0.', 1;

    IF @ActionType NOT IN (N'Publish', N'Unpublish', N'Archive', N'SoftDelete')
        THROW 54311, 'ActionType is invalid.', 1;

    IF @FromStatus IS NOT NULL
       AND @FromStatus NOT IN (N'Draft', N'Published', N'Archived')
        THROW 54312, 'FromStatus is invalid.', 1;

    IF @ToStatus IS NOT NULL
       AND @ToStatus NOT IN (N'Draft', N'Published', N'Archived')
        THROW 54313, 'ToStatus is invalid.', 1;

    IF @ActionType = N'Unpublish'
       AND LEN(LTRIM(RTRIM(ISNULL(@Reason, N'')))) = 0
        THROW 54314, 'Reason is required for Unpublish.', 1;

    INSERT INTO [content].[ArticleLifecycleEvent]
    (
        [ArticleId],
        [ArticleVersion],
        [ActionType],
        [FromStatus],
        [ToStatus],
        [Reason],
        [ActorUserId],
        [CorrelationId],
        [MetadataJson]
    )
    VALUES
    (
        @ArticleId,
        @ArticleVersion,
        @ActionType,
        @FromStatus,
        @ToStatus,
        @Reason,
        @ActorUserId,
        @CorrelationId,
        @MetadataJson
    );

    SELECT TOP (1) *
    FROM [content].[ArticleLifecycleEvent]
    WHERE [EventId] = SCOPE_IDENTITY();
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_ArticleLifecycleEvent_SelectByArticleId]
    @ArticleId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM [content].[ArticleLifecycleEvent]
    WHERE [ArticleId] = @ArticleId
    ORDER BY [OccurredAt] DESC, [EventId] DESC;
END
GO
