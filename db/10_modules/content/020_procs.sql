/*
  File: db/10_modules/content/020_procs.sql
  Module: Content
  Purpose:
  - Create stored procedures for Content truth model in CommercialNews V1.
  - Support:
      * Category CRUD (soft delete / restore)
      * Tag CRUD (soft delete / restore)
      * Article CRUD + lifecycle transitions
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

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

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
    @ExpectedVersion     INT
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
    @ExpectedVersion     INT
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
    @ExpectedVersion     INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [content].[Category]
    SET
        [IsDeleted] = 0,
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
    @ExpectedVersion     INT
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
    @ExpectedVersion     INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [content].[Tag]
    SET
        [IsDeleted] = 1,
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
    @ExpectedVersion     INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [content].[Tag]
    SET
        [IsDeleted] = 0,
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

/* =========================================================
   ARTICLE
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_Article_Insert]
    @PublicId            CHAR(26),
    @CategoryId          BIGINT = NULL,
    @AuthorUserId        BIGINT,
    @Title               NVARCHAR(300),
    @Summary             NVARCHAR(2000) = NULL,
    @Content             NVARCHAR(MAX),
    @Status              NVARCHAR(30) = N'Draft',
    @PublishedAt         DATETIME2(3) = NULL,
    @UnpublishedAt       DATETIME2(3) = NULL,
    @ArchivedAt          DATETIME2(3) = NULL,
    @CoverMediaId        BIGINT = NULL,
    @CreatedByUserId     BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@PublicId, '')))) = 0
        THROW 54260, 'Article PublicId is required.', 1;

    IF @AuthorUserId IS NULL OR @AuthorUserId <= 0
        THROW 54261, 'AuthorUserId must be > 0.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Title, N'')))) = 0
        THROW 54262, 'Article Title is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Content, N'')))) = 0
        THROW 54263, 'Article Content is required.', 1;

    IF @Status NOT IN (N'Draft', N'Published', N'Archived')
        THROW 54264, 'Article Status must be Draft, Published, or Archived.', 1;

    IF @CategoryId IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1
           FROM [content].[Category]
           WHERE [CategoryId] = @CategoryId
             AND [IsDeleted] = 0
             AND [IsActive] = 1
       )
        THROW 54265, 'Category does not exist, is deleted, or inactive.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [content].[Article]
        WHERE [PublicId] = @PublicId
    )
        THROW 54266, 'Article PublicId already exists.', 1;

    IF @Status = N'Published' AND @PublishedAt IS NULL
        SET @PublishedAt = SYSUTCDATETIME();

    IF @Status = N'Archived' AND @ArchivedAt IS NULL
        SET @ArchivedAt = SYSUTCDATETIME();

    INSERT INTO [content].[Article]
    (
        [PublicId],
        [CategoryId],
        [AuthorUserId],
        [Title],
        [Summary],
        [Content],
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
        @PublicId,
        @CategoryId,
        @AuthorUserId,
        @Title,
        @Summary,
        @Content,
        @Status,
        @PublishedAt,
        @UnpublishedAt,
        @ArchivedAt,
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
    @CategoryId          BIGINT = NULL,
    @Title               NVARCHAR(300),
    @Summary             NVARCHAR(2000) = NULL,
    @Content             NVARCHAR(MAX),
    @CoverMediaId        BIGINT = NULL,
    @UpdatedByUserId     BIGINT = NULL,
    @ExpectedVersion     INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 54270, 'ArticleId must be > 0.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Title, N'')))) = 0
        THROW 54271, 'Article Title is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Content, N'')))) = 0
        THROW 54272, 'Article Content is required.', 1;

    IF @CategoryId IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1
           FROM [content].[Category]
           WHERE [CategoryId] = @CategoryId
             AND [IsDeleted] = 0
             AND [IsActive] = 1
       )
        THROW 54273, 'Category does not exist, is deleted, or inactive.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [content].[Article]
        WHERE [ArticleId] = @ArticleId
          AND [IsDeleted] = 0
          AND [Status] = N'Archived'
    )
        THROW 54274, 'Cannot update archived article. Restore/unarchive first.', 1;

    UPDATE [content].[Article]
    SET
        [CategoryId] = @CategoryId,
        [Title] = @Title,
        [Summary] = @Summary,
        [Content] = @Content,
        [CoverMediaId] = @CoverMediaId,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedByUserId] = @UpdatedByUserId,
        [Version] = [Version] + 1
    WHERE [ArticleId] = @ArticleId
      AND [IsDeleted] = 0
      AND [Version] = @ExpectedVersion;

    IF @@ROWCOUNT = 0
        THROW 54275, 'Article update failed. Record not found, deleted, archived, or version mismatch.', 1;

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
    @PublicId CHAR(26)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1) *
    FROM [content].[Article]
    WHERE [PublicId] = @PublicId;
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
            [a].[PublicId],
            [a].[CategoryId],
            [a].[AuthorUserId],
            [a].[Title],
            [a].[Summary],
            [a].[Content],
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
    @ExpectedVersion     INT
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
          AND (LEN(LTRIM(RTRIM(ISNULL([Title], N'')))) = 0 OR LEN(LTRIM(RTRIM(ISNULL([Content], N'')))) = 0)
    )
        THROW 54282, 'Cannot publish article without title/content.', 1;

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
      AND [Status] <> N'Published';

    IF @@ROWCOUNT = 0
        THROW 54283, 'Article publish failed. Record not found, already published, or version mismatch.', 1;

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
    @ExpectedVersion     INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

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
        THROW 54284, 'Article unpublish failed. Record not found, not published, or version mismatch.', 1;

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
    @ExpectedVersion     INT
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
      AND [Status] <> N'Archived';

    IF @@ROWCOUNT = 0
        THROW 54285, 'Article archive failed. Record not found, already archived, or version mismatch.', 1;

    SELECT TOP (1) *
    FROM [content].[Article]
    WHERE [ArticleId] = @ArticleId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_Article_Restore]
    @ArticleId           BIGINT,
    @ActorUserId         BIGINT = NULL,
    @ExpectedVersion     INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [content].[Article]
    SET
        [Status] = N'Draft',
        [ArchivedAt] = NULL,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedByUserId] = @ActorUserId,
        [Version] = [Version] + 1
    WHERE [ArticleId] = @ArticleId
      AND [IsDeleted] = 0
      AND [Version] = @ExpectedVersion
      AND [Status] = N'Archived';

    IF @@ROWCOUNT = 0
        THROW 54286, 'Article restore failed. Record not found, not archived, or version mismatch.', 1;

    SELECT TOP (1) *
    FROM [content].[Article]
    WHERE [ArticleId] = @ArticleId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_Article_Delete]
    @ArticleId           BIGINT,
    @DeletedByUserId     BIGINT = NULL,
    @ExpectedVersion     INT
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
        THROW 54287, 'Article delete failed. Record not found, already deleted, or version mismatch.', 1;

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
    @CreatedByUserId     BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [content].[Article]
        WHERE [ArticleId] = @ArticleId
          AND [IsDeleted] = 0
    )
        THROW 54290, 'Article does not exist or was deleted.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [content].[Tag]
        WHERE [TagId] = @TagId
          AND [IsDeleted] = 0
          AND [IsActive] = 1
    )
        THROW 54291, 'Tag does not exist, is deleted, or inactive.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [content].[ArticleTag]
        WHERE [ArticleId] = @ArticleId
          AND [TagId] = @TagId
    )
        THROW 54292, 'ArticleTag already exists.', 1;

    INSERT INTO [content].[ArticleTag]
    (
        [ArticleId],
        [TagId],
        [CreatedByUserId]
    )
    VALUES
    (
        @ArticleId,
        @TagId,
        @CreatedByUserId
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

    DELETE FROM [content].[ArticleTag]
    WHERE [ArticleId] = @ArticleId
      AND [TagId] = @TagId;

    SELECT CAST(@@ROWCOUNT AS INT) AS [RowsAffected];
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [content].[Content_ArticleTag_DeleteAllByArticleId]
    @ArticleId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

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
        [at].[CreatedAt],
        [at].[CreatedByUserId],
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
    @RevisionNumber          INT,
    @TitleSnapshot           NVARCHAR(300),
    @SummarySnapshot         NVARCHAR(2000) = NULL,
    @ContentSnapshot         NVARCHAR(MAX),
    @CategoryIdSnapshot      BIGINT = NULL,
    @StatusSnapshot          NVARCHAR(30),
    @CoverMediaIdSnapshot    BIGINT = NULL,
    @ChangedByUserId         BIGINT = NULL,
    @ChangeType              NVARCHAR(30),
    @ChangeSummary           NVARCHAR(1000) = NULL
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

    IF @RevisionNumber IS NULL OR @RevisionNumber <= 0
        THROW 54301, 'RevisionNumber must be > 0.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@TitleSnapshot, N'')))) = 0
        THROW 54302, 'TitleSnapshot is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@ContentSnapshot, N'')))) = 0
        THROW 54303, 'ContentSnapshot is required.', 1;

    IF @StatusSnapshot NOT IN (N'Draft', N'Published', N'Archived')
        THROW 54304, 'StatusSnapshot is invalid.', 1;

    IF @ChangeType NOT IN (N'Create', N'Update', N'Publish', N'Unpublish', N'Archive', N'Restore', N'Delete')
        THROW 54305, 'ChangeType is invalid.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [content].[ArticleRevision]
        WHERE [ArticleId] = @ArticleId
          AND [RevisionNumber] = @RevisionNumber
    )
        THROW 54306, 'RevisionNumber already exists for this article.', 1;

    INSERT INTO [content].[ArticleRevision]
    (
        [ArticleId],
        [RevisionNumber],
        [TitleSnapshot],
        [SummarySnapshot],
        [ContentSnapshot],
        [CategoryIdSnapshot],
        [StatusSnapshot],
        [CoverMediaIdSnapshot],
        [ChangedByUserId],
        [ChangeType],
        [ChangeSummary]
    )
    VALUES
    (
        @ArticleId,
        @RevisionNumber,
        @TitleSnapshot,
        @SummarySnapshot,
        @ContentSnapshot,
        @CategoryIdSnapshot,
        @StatusSnapshot,
        @CoverMediaIdSnapshot,
        @ChangedByUserId,
        @ChangeType,
        @ChangeSummary
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
    ORDER BY [RevisionNumber] DESC, [RevisionId] DESC;
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
    @ActionType          NVARCHAR(30),
    @FromStatus          NVARCHAR(30) = NULL,
    @ToStatus            NVARCHAR(30) = NULL,
    @Reason              NVARCHAR(1000) = NULL,
    @ActorUserId         BIGINT = NULL
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

    IF @ActionType NOT IN (N'Create', N'Update', N'Publish', N'Unpublish', N'Archive', N'Restore', N'Delete')
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
        [ActionType],
        [FromStatus],
        [ToStatus],
        [Reason],
        [ActorUserId]
    )
    VALUES
    (
        @ArticleId,
        @ActionType,
        @FromStatus,
        @ToStatus,
        @Reason,
        @ActorUserId
    );

    SELECT TOP (1) *
    FROM [content].[ArticleLifecycleEvent]
    WHERE [ArticleLifecycleEventId] = SCOPE_IDENTITY();
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
    ORDER BY [OccurredAt] DESC, [ArticleLifecycleEventId] DESC;
END
GO