/*
  File: db/10_modules/media/020_procs.sql
  Module: Media
  Purpose:
  - Create stored procedures for Media V1.
  - Focus on practical OLTP operations for:
      * media asset registration and lookup
      * media asset soft delete / restore
      * article-media attachment lifecycle
      * primary-media selection
      * deterministic attachment reorder
      * article-scoped media queries
  - Idempotent: uses CREATE OR ALTER PROCEDURE

  Notes:
  - Business orchestration still primarily belongs in application/service layer.
  - Media truth owns:
      * media metadata
      * attachment membership
      * primary selection
      * ordering
      * soft delete / restore state
  - Outbox / audit emission is intentionally handled outside this file.
  - Reorder is treated as a final-state set operation.
  - Re-attaching a soft-deleted relation should restore the existing row
    rather than create a duplicate relationship row.
*/

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

USE [CommercialNews];
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 52201, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

IF SCHEMA_ID(N'media') IS NULL
BEGIN
    THROW 52202, 'Schema [media] does not exist. Run bootstrap scripts first.', 1;
END
GO

/* =========================================================
   MediaAsset
   ========================================================= */

CREATE OR ALTER PROCEDURE [media].[Media_MediaAsset_Insert]
    @PublicId         VARCHAR(26),
    @StorageProvider  VARCHAR(30),
    @Url              NVARCHAR(800),
    @StoragePath      NVARCHAR(800) = NULL,
    @FileName         NVARCHAR(255) = NULL,
    @MediaType        VARCHAR(20),
    @MimeType         NVARCHAR(100) = NULL,
    @FileSizeBytes    BIGINT = NULL,
    @Width            INT = NULL,
    @Height           INT = NULL,
    @DurationSeconds  INT = NULL,
    @AltText          NVARCHAR(300) = NULL,
    @MetadataJson     NVARCHAR(MAX) = NULL,
    @ContentHash      VARBINARY(32) = NULL,
    @CreatedBy        BIGINT = NULL,
    @MediaId          BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    INSERT INTO [media].[MediaAsset]
    (
        [PublicId],
        [StorageProvider],
        [Url],
        [StoragePath],
        [FileName],
        [MediaType],
        [MimeType],
        [FileSizeBytes],
        [Width],
        [Height],
        [DurationSeconds],
        [AltText],
        [MetadataJson],
        [ContentHash],
        [CreatedAt],
        [CreatedBy],
        [UpdatedAt],
        [UpdatedBy],
        [Version],
        [IsDeleted],
        [DeletedAt],
        [DeletedBy],
        [RestoreUntil]
    )
    VALUES
    (
        @PublicId,
        @StorageProvider,
        @Url,
        @StoragePath,
        @FileName,
        @MediaType,
        @MimeType,
        @FileSizeBytes,
        @Width,
        @Height,
        @DurationSeconds,
        @AltText,
        @MetadataJson,
        @ContentHash,
        SYSUTCDATETIME(),
        @CreatedBy,
        SYSUTCDATETIME(),
        @CreatedBy,
        1,
        0,
        NULL,
        NULL,
        NULL
    );

    SET @MediaId = SCOPE_IDENTITY();
END;
GO

CREATE OR ALTER PROCEDURE [media].[Media_MediaAsset_SelectById]
    @MediaId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [MediaId],
        [PublicId],
        [StorageProvider],
        [Url],
        [StoragePath],
        [FileName],
        [MediaType],
        [MimeType],
        [FileSizeBytes],
        [Width],
        [Height],
        [DurationSeconds],
        [AltText],
        [MetadataJson],
        [ContentHash],
        [CreatedAt],
        [CreatedBy],
        [UpdatedAt],
        [UpdatedBy],
        [Version],
        [IsDeleted],
        [DeletedAt],
        [DeletedBy],
        [RestoreUntil]
    FROM [media].[MediaAsset]
    WHERE [MediaId] = @MediaId;
END;
GO

CREATE OR ALTER PROCEDURE [media].[Media_MediaAsset_SelectByPublicId]
    @PublicId VARCHAR(26)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [MediaId],
        [PublicId],
        [StorageProvider],
        [Url],
        [StoragePath],
        [FileName],
        [MediaType],
        [MimeType],
        [FileSizeBytes],
        [Width],
        [Height],
        [DurationSeconds],
        [AltText],
        [MetadataJson],
        [ContentHash],
        [CreatedAt],
        [CreatedBy],
        [UpdatedAt],
        [UpdatedBy],
        [Version],
        [IsDeleted],
        [DeletedAt],
        [DeletedBy],
        [RestoreUntil]
    FROM [media].[MediaAsset]
    WHERE [PublicId] = @PublicId;
END;
GO

CREATE OR ALTER PROCEDURE [media].[Media_MediaAsset_SoftDelete]
    @MediaId        BIGINT,
    @DeletedBy      BIGINT = NULL,
    @RestoreUntil   DATETIME2(3) = NULL,
    @AffectedRows   INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    /* Option A: if this asset is current primary anywhere, unset it in the same truth transaction */
    UPDATE [media].[ArticleMedia]
    SET
        [IsPrimary] = 0,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedBy] = @DeletedBy,
        [Version] = [Version] + 1
    WHERE [MediaId] = @MediaId
      AND [IsDeleted] = 0
      AND [IsPrimary] = 1;

    UPDATE [media].[MediaAsset]
    SET
        [IsDeleted] = 1,
        [DeletedAt] = SYSUTCDATETIME(),
        [DeletedBy] = @DeletedBy,
        [RestoreUntil] = @RestoreUntil,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedBy] = @DeletedBy,
        [Version] = [Version] + 1
    WHERE [MediaId] = @MediaId
      AND [IsDeleted] = 0;

    SET @AffectedRows = @@ROWCOUNT;

    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE [media].[Media_MediaAsset_Restore]
    @MediaId        BIGINT,
    @RestoredBy     BIGINT = NULL,
    @AffectedRows   INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [media].[MediaAsset]
    SET
        [IsDeleted] = 0,
        [DeletedAt] = NULL,
        [DeletedBy] = NULL,
        [RestoreUntil] = NULL,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedBy] = @RestoredBy,
        [Version] = [Version] + 1
    WHERE [MediaId] = @MediaId
      AND [IsDeleted] = 1
      AND ([RestoreUntil] IS NULL OR [RestoreUntil] >= SYSUTCDATETIME());

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [media].[Media_MediaAsset_GetRecordCount]
    @IsDeleted BIT = NULL,
    @MediaType VARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(1) AS [TotalRecords]
    FROM [media].[MediaAsset]
    WHERE (@IsDeleted IS NULL OR [IsDeleted] = @IsDeleted)
      AND (@MediaType IS NULL OR [MediaType] = @MediaType);
END;
GO

CREATE OR ALTER PROCEDURE [media].[Media_MediaAsset_SelectSkipAndTake]
    @Skip            INT,
    @Take            INT,
    @IsDeleted       BIT = NULL,
    @MediaType       VARCHAR(20) = NULL,
    @SortBy          NVARCHAR(50) = N'CreatedAt',
    @SortDirection   NVARCHAR(4) = N'DESC'
AS
BEGIN
    SET NOCOUNT ON;

    IF @Skip < 0
        SET @Skip = 0;

    IF @Take IS NULL OR @Take <= 0
        SET @Take = 20;

    IF @Take > 200
        SET @Take = 200;

    IF @SortBy NOT IN (N'MediaId', N'CreatedAt', N'UpdatedAt', N'FileName', N'MediaType')
        SET @SortBy = N'CreatedAt';

    IF UPPER(@SortDirection) NOT IN (N'ASC', N'DESC')
        SET @SortDirection = N'DESC';

    DECLARE @Sql NVARCHAR(MAX) =
    N'
    SELECT
        [MediaId],
        [PublicId],
        [StorageProvider],
        [Url],
        [StoragePath],
        [FileName],
        [MediaType],
        [MimeType],
        [FileSizeBytes],
        [Width],
        [Height],
        [DurationSeconds],
        [AltText],
        [MetadataJson],
        [ContentHash],
        [CreatedAt],
        [CreatedBy],
        [UpdatedAt],
        [UpdatedBy],
        [Version],
        [IsDeleted],
        [DeletedAt],
        [DeletedBy],
        [RestoreUntil]
    FROM [media].[MediaAsset]
    WHERE (@IsDeleted IS NULL OR [IsDeleted] = @IsDeleted)
      AND (@MediaType IS NULL OR [MediaType] = @MediaType)
    ORDER BY ' + QUOTENAME(@SortBy) + N' ' + @SortDirection + N'
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;';

    EXEC sp_executesql
        @Sql,
        N'@IsDeleted BIT, @MediaType VARCHAR(20), @Skip INT, @Take INT',
        @IsDeleted = @IsDeleted,
        @MediaType = @MediaType,
        @Skip = @Skip,
        @Take = @Take;
END;
GO

/* =========================================================
   ArticleMedia
   ========================================================= */

CREATE OR ALTER PROCEDURE [media].[Media_ArticleMedia_Attach]
    @ArticleId      BIGINT,
    @MediaId        BIGINT,
    @IsPrimary      BIT = 0,
    @CreatedBy      BIGINT = NULL,
    @ArticleMediaId BIGINT OUTPUT,
    @AffectedRows   INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ExistingArticleMediaId BIGINT;

    BEGIN TRANSACTION;

    /* If relation exists but is soft-deleted, restore it */
    SELECT TOP (1)
        @ExistingArticleMediaId = [ArticleMediaId]
    FROM [media].[ArticleMedia]
    WHERE [ArticleId] = @ArticleId
      AND [MediaId] = @MediaId;

    IF @ExistingArticleMediaId IS NOT NULL
    BEGIN
        UPDATE [media].[ArticleMedia]
        SET
            [IsDeleted] = 0,
            [DeletedAt] = NULL,
            [DeletedBy] = NULL,
            [UpdatedAt] = SYSUTCDATETIME(),
            [UpdatedBy] = @CreatedBy,
            [Version] = [Version] + 1
        WHERE [ArticleMediaId] = @ExistingArticleMediaId
          AND [IsDeleted] = 1;

        SET @AffectedRows = CASE WHEN @@ROWCOUNT > 0 THEN 1 ELSE 0 END;
        SET @ArticleMediaId = @ExistingArticleMediaId;
    END
    ELSE
    BEGIN
        DECLARE @NextSortOrder INT;

        SELECT @NextSortOrder = ISNULL(MAX([SortOrder]), -1) + 1
        FROM [media].[ArticleMedia]
        WHERE [ArticleId] = @ArticleId
          AND [IsDeleted] = 0;

        INSERT INTO [media].[ArticleMedia]
        (
            [ArticleId],
            [MediaId],
            [SortOrder],
            [IsPrimary],
            [AltTextOverride],
            [Caption],
            [Version],
            [CreatedAt],
            [CreatedBy],
            [UpdatedAt],
            [UpdatedBy],
            [IsDeleted],
            [DeletedAt],
            [DeletedBy]
        )
        VALUES
        (
            @ArticleId,
            @MediaId,
            @NextSortOrder,
            0,
            NULL,
            NULL,
            1,
            SYSUTCDATETIME(),
            @CreatedBy,
            SYSUTCDATETIME(),
            @CreatedBy,
            0,
            NULL,
            NULL
        );

        SET @ArticleMediaId = SCOPE_IDENTITY();
        SET @AffectedRows = 1;
    END

    IF @IsPrimary = 1
    BEGIN
        UPDATE [media].[ArticleMedia]
        SET
            [IsPrimary] = 0,
            [UpdatedAt] = SYSUTCDATETIME(),
            [UpdatedBy] = @CreatedBy,
            [Version] = [Version] + 1
        WHERE [ArticleId] = @ArticleId
          AND [IsDeleted] = 0
          AND [IsPrimary] = 1
          AND [MediaId] <> @MediaId;

        UPDATE [media].[ArticleMedia]
        SET
            [IsPrimary] = 1,
            [UpdatedAt] = SYSUTCDATETIME(),
            [UpdatedBy] = @CreatedBy,
            [Version] = [Version] + 1
        WHERE [ArticleId] = @ArticleId
          AND [MediaId] = @MediaId
          AND [IsDeleted] = 0;
    END

    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE [media].[Media_ArticleMedia_Detach]
    @ArticleId      BIGINT,
    @MediaId        BIGINT,
    @DeletedBy      BIGINT = NULL,
    @AffectedRows   INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [media].[ArticleMedia]
    SET
        [IsPrimary] = 0,
        [IsDeleted] = 1,
        [DeletedAt] = SYSUTCDATETIME(),
        [DeletedBy] = @DeletedBy,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedBy] = @DeletedBy,
        [Version] = [Version] + 1
    WHERE [ArticleId] = @ArticleId
      AND [MediaId] = @MediaId
      AND [IsDeleted] = 0;

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [media].[Media_ArticleMedia_Restore]
    @ArticleId      BIGINT,
    @MediaId        BIGINT,
    @RestoredBy     BIGINT = NULL,
    @AffectedRows   INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [media].[ArticleMedia]
    SET
        [IsDeleted] = 0,
        [DeletedAt] = NULL,
        [DeletedBy] = NULL,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedBy] = @RestoredBy,
        [Version] = [Version] + 1
    WHERE [ArticleId] = @ArticleId
      AND [MediaId] = @MediaId
      AND [IsDeleted] = 1;

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [media].[Media_ArticleMedia_SetPrimary]
    @ArticleId      BIGINT,
    @MediaId        BIGINT,
    @UpdatedBy      BIGINT = NULL,
    @AffectedRows   INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    UPDATE [media].[ArticleMedia]
    SET
        [IsPrimary] = 0,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedBy] = @UpdatedBy,
        [Version] = [Version] + 1
    WHERE [ArticleId] = @ArticleId
      AND [IsDeleted] = 0
      AND [IsPrimary] = 1
      AND [MediaId] <> @MediaId;

    UPDATE [media].[ArticleMedia]
    SET
        [IsPrimary] = 1,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedBy] = @UpdatedBy,
        [Version] = [Version] + 1
    WHERE [ArticleId] = @ArticleId
      AND [MediaId] = @MediaId
      AND [IsDeleted] = 0;

    SET @AffectedRows = @@ROWCOUNT;

    COMMIT TRANSACTION;
END;
GO

/* =========================================================
   ArticleMedia TVP for reorder
   ========================================================= */

IF TYPE_ID(N'[media].[MediaOrderListType]') IS NOT NULL
BEGIN
    DROP TYPE [media].[MediaOrderListType];
END
GO

CREATE TYPE [media].[MediaOrderListType] AS TABLE
(
    [MediaId] BIGINT NOT NULL,
    [SortOrder] INT NOT NULL
);
GO

/* =========================================================
   ArticleMedia reorder
   ========================================================= */

CREATE OR ALTER PROCEDURE [media].[Media_ArticleMedia_ReorderByIds]
    @ArticleId      BIGINT,
    @UpdatedBy      BIGINT = NULL,
    @Orders         [media].[MediaOrderListType] READONLY,
    @AffectedRows   INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    UPDATE AM
    SET
        [SortOrder] = O.[SortOrder],
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedBy] = @UpdatedBy,
        [Version] = [Version] + 1
    FROM [media].[ArticleMedia] AM
    INNER JOIN @Orders O
        ON O.[MediaId] = AM.[MediaId]
    WHERE AM.[ArticleId] = @ArticleId
      AND AM.[IsDeleted] = 0;

    SET @AffectedRows = @@ROWCOUNT;

    COMMIT TRANSACTION;
END;
GO

/* =========================================================
   ArticleMedia lookup
   ========================================================= */

CREATE OR ALTER PROCEDURE [media].[Media_ArticleMedia_SelectById]
    @ArticleMediaId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        AM.[ArticleMediaId],
        AM.[ArticleId],
        AM.[MediaId],
        MA.[PublicId],
        MA.[StorageProvider],
        MA.[Url],
        MA.[StoragePath],
        MA.[FileName],
        MA.[MediaType],
        MA.[MimeType],
        MA.[FileSizeBytes],
        MA.[Width],
        MA.[Height],
        MA.[DurationSeconds],
        MA.[AltText] AS [DefaultAltText],
        AM.[AltTextOverride],
        AM.[Caption],
        AM.[SortOrder],
        AM.[IsPrimary],
        AM.[CreatedAt],
        AM.[CreatedBy],
        AM.[UpdatedAt],
        AM.[UpdatedBy],
        AM.[Version],
        AM.[IsDeleted],
        AM.[DeletedAt],
        AM.[DeletedBy]
    FROM [media].[ArticleMedia] AM
    INNER JOIN [media].[MediaAsset] MA
        ON MA.[MediaId] = AM.[MediaId]
    WHERE AM.[ArticleMediaId] = @ArticleMediaId;
END;
GO

CREATE OR ALTER PROCEDURE [media].[Media_ArticleMedia_SelectAllByArticleId]
    @ArticleId BIGINT,
    @IncludeDeleted BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        AM.[ArticleMediaId],
        AM.[ArticleId],
        AM.[MediaId],
        MA.[PublicId],
        MA.[StorageProvider],
        MA.[Url],
        MA.[StoragePath],
        MA.[FileName],
        MA.[MediaType],
        MA.[MimeType],
        MA.[FileSizeBytes],
        MA.[Width],
        MA.[Height],
        MA.[DurationSeconds],
        MA.[AltText] AS [DefaultAltText],
        AM.[AltTextOverride],
        AM.[Caption],
        AM.[SortOrder],
        AM.[IsPrimary],
        AM.[CreatedAt],
        AM.[CreatedBy],
        AM.[UpdatedAt],
        AM.[UpdatedBy],
        AM.[Version],
        AM.[IsDeleted],
        AM.[DeletedAt],
        AM.[DeletedBy]
    FROM [media].[ArticleMedia] AM
    INNER JOIN [media].[MediaAsset] MA
        ON MA.[MediaId] = AM.[MediaId]
    WHERE AM.[ArticleId] = @ArticleId
      AND (@IncludeDeleted = 1 OR AM.[IsDeleted] = 0)
    ORDER BY AM.[SortOrder] ASC, AM.[ArticleMediaId] ASC;
END;
GO

CREATE OR ALTER PROCEDURE [media].[Media_ArticleMedia_SelectPrimaryByArticleId]
    @ArticleId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        AM.[ArticleMediaId],
        AM.[ArticleId],
        AM.[MediaId],
        MA.[PublicId],
        MA.[StorageProvider],
        MA.[Url],
        MA.[StoragePath],
        MA.[FileName],
        MA.[MediaType],
        MA.[MimeType],
        MA.[FileSizeBytes],
        MA.[Width],
        MA.[Height],
        MA.[DurationSeconds],
        MA.[AltText] AS [DefaultAltText],
        AM.[AltTextOverride],
        AM.[Caption],
        AM.[SortOrder],
        AM.[IsPrimary],
        AM.[CreatedAt],
        AM.[CreatedBy],
        AM.[UpdatedAt],
        AM.[UpdatedBy],
        AM.[Version]
    FROM [media].[ArticleMedia] AM
    INNER JOIN [media].[MediaAsset] MA
        ON MA.[MediaId] = AM.[MediaId]
    WHERE AM.[ArticleId] = @ArticleId
      AND AM.[IsDeleted] = 0
      AND AM.[IsPrimary] = 1
    ORDER BY AM.[ArticleMediaId] ASC;
END;
GO

CREATE OR ALTER PROCEDURE [media].[Media_ArticleMedia_SelectAllByMediaId]
    @MediaId BIGINT,
    @IncludeDeleted BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        AM.[ArticleMediaId],
        AM.[ArticleId],
        AM.[MediaId],
        AM.[SortOrder],
        AM.[IsPrimary],
        AM.[AltTextOverride],
        AM.[Caption],
        AM.[CreatedAt],
        AM.[CreatedBy],
        AM.[UpdatedAt],
        AM.[UpdatedBy],
        AM.[Version],
        AM.[IsDeleted],
        AM.[DeletedAt],
        AM.[DeletedBy]
    FROM [media].[ArticleMedia] AM
    WHERE AM.[MediaId] = @MediaId
      AND (@IncludeDeleted = 1 OR AM.[IsDeleted] = 0)
    ORDER BY AM.[ArticleId] ASC, AM.[SortOrder] ASC, AM.[ArticleMediaId] ASC;
END;
GO

CREATE OR ALTER PROCEDURE [media].[Media_ArticleMedia_GetRecordCountByArticleId]
    @ArticleId BIGINT,
    @IncludeDeleted BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SELECT COUNT(1) AS [TotalRecords]
    FROM [media].[ArticleMedia] AM
    WHERE AM.[ArticleId] = @ArticleId
      AND (@IncludeDeleted = 1 OR AM.[IsDeleted] = 0);
END;
GO

CREATE OR ALTER PROCEDURE [media].[Media_ArticleMedia_SelectSkipAndTakeByArticleId]
    @ArticleId BIGINT,
    @Skip INT,
    @Take INT,
    @IncludeDeleted BIT = 0,
    @SortBy NVARCHAR(50) = N'SortOrder',
    @SortDirection NVARCHAR(4) = N'ASC'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Skip < 0
        SET @Skip = 0;

    IF @Take IS NULL OR @Take <= 0
        SET @Take = 20;

    IF @Take > 200
        SET @Take = 200;

    IF @SortBy NOT IN (N'SortOrder', N'CreatedAt', N'UpdatedAt', N'MediaId')
        SET @SortBy = N'SortOrder';

    IF UPPER(@SortDirection) NOT IN (N'ASC', N'DESC')
        SET @SortDirection = N'ASC';

    DECLARE @Sql NVARCHAR(MAX) =
    N'
    SELECT
        AM.[ArticleMediaId],
        AM.[ArticleId],
        AM.[MediaId],
        MA.[PublicId],
        MA.[StorageProvider],
        MA.[Url],
        MA.[StoragePath],
        MA.[FileName],
        MA.[MediaType],
        MA.[MimeType],
        MA.[FileSizeBytes],
        MA.[Width],
        MA.[Height],
        MA.[DurationSeconds],
        MA.[AltText] AS [DefaultAltText],
        AM.[AltTextOverride],
        AM.[Caption],
        AM.[SortOrder],
        AM.[IsPrimary],
        AM.[CreatedAt],
        AM.[CreatedBy],
        AM.[UpdatedAt],
        AM.[UpdatedBy],
        AM.[Version],
        AM.[IsDeleted],
        AM.[DeletedAt],
        AM.[DeletedBy]
    FROM [media].[ArticleMedia] AM
    INNER JOIN [media].[MediaAsset] MA
        ON MA.[MediaId] = AM.[MediaId]
    WHERE AM.[ArticleId] = @ArticleId
      AND (@IncludeDeleted = 1 OR AM.[IsDeleted] = 0)
    ORDER BY ' + QUOTENAME(@SortBy) + N' ' + @SortDirection + N', AM.[ArticleMediaId] ASC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;';

    EXEC sp_executesql
        @Sql,
        N'@ArticleId BIGINT, @IncludeDeleted BIT, @Skip INT, @Take INT',
        @ArticleId = @ArticleId,
        @IncludeDeleted = @IncludeDeleted,
        @Skip = @Skip,
        @Take = @Take;
END;
GO