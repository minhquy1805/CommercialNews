/*
  File: db/10_modules/media/020_procs.sql
  Module: Media
  Purpose:
  - Create stored procedures for Media V1.
  - Focus on practical OLTP operations for:
      * media asset registration and lookup
      * safe media metadata update
      * media asset soft delete / restore
      * article-media attachment-set versioning
      * article-media attachment lifecycle
      * primary-media selection
      * deterministic attachment reorder
      * article-scoped media queries
  - Idempotent: uses CREATE OR ALTER PROCEDURE where possible.

  Notes:
  - Business orchestration still primarily belongs in application/service layer.
  - These procedures mutate Media truth only.
  - Application/use-case layer must execute truth mutation and shared Outbox insert
    inside the same local transaction for commands that emit Media events.
  - Media truth owns:
      * media metadata
      * attachment membership
      * attachment-set version
      * primary selection
      * ordering
      * soft delete / restore state
  - Reorder is treated as a final-state set operation.
  - Re-attaching a soft-deleted relation should restore the existing row
    rather than create a duplicate relationship row.

  ResultCode convention for write procs that expose it:
  - 0 = Success
  - 1 = NotFound
  - 2 = InvalidState / Deleted / RestoreNotAllowed
  - 3 = VersionConflict
  - 4 = InvalidReorderList
  - 5 = Duplicate / ConstraintConflict
  - 6 = ExpectedVersionRequired
  - 7 = PrimaryMediaMustBeImage
*/

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 52201, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

USE [CommercialNews];
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
    @MediaId          BIGINT OUTPUT,
    @NewVersion       INT OUTPUT
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
        [RestoreUntil],
        [RestoredAt],
        [RestoredBy]
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
        NULL,
        NULL,
        NULL
    );

    SET @MediaId = SCOPE_IDENTITY();
    SET @NewVersion = 1;
END;
GO

CREATE OR ALTER PROCEDURE [media].[Media_MediaAsset_UpdateMetadata]
    @MediaId        BIGINT,
    @AltText        NVARCHAR(300) = NULL,
    @MetadataJson   NVARCHAR(MAX) = NULL,
    @UpdatedBy      BIGINT = NULL,
    @AffectedRows   INT OUTPUT,
    @NewVersion     INT OUTPUT,
    @ResultCode     INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @AffectedRows = 0;
    SET @NewVersion = NULL;
    SET @ResultCode = 0;

    IF NOT EXISTS (SELECT 1 FROM [media].[MediaAsset] WHERE [MediaId] = @MediaId)
    BEGIN
        SET @ResultCode = 1;
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM [media].[MediaAsset] WHERE [MediaId] = @MediaId AND [IsDeleted] = 1)
    BEGIN
        SET @ResultCode = 2;
        SELECT @NewVersion = [Version]
        FROM [media].[MediaAsset]
        WHERE [MediaId] = @MediaId;
        RETURN;
    END

    UPDATE [media].[MediaAsset]
    SET
        [AltText] = @AltText,
        [MetadataJson] = @MetadataJson,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedBy] = @UpdatedBy,
        [Version] = [Version] + 1
    WHERE [MediaId] = @MediaId
      AND [IsDeleted] = 0
      AND
      (
          ([AltText] <> @AltText OR ([AltText] IS NULL AND @AltText IS NOT NULL) OR ([AltText] IS NOT NULL AND @AltText IS NULL))
          OR
          ([MetadataJson] <> @MetadataJson OR ([MetadataJson] IS NULL AND @MetadataJson IS NOT NULL) OR ([MetadataJson] IS NOT NULL AND @MetadataJson IS NULL))
      );

    SET @AffectedRows = @@ROWCOUNT;

    SELECT @NewVersion = [Version]
    FROM [media].[MediaAsset]
    WHERE [MediaId] = @MediaId;
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
        [RestoreUntil],
        [RestoredAt],
        [RestoredBy]
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
        [RestoreUntil],
        [RestoredAt],
        [RestoredBy]
    FROM [media].[MediaAsset]
    WHERE [PublicId] = @PublicId;
END;
GO

CREATE OR ALTER PROCEDURE [media].[Media_MediaAsset_SoftDelete]
    @MediaId              BIGINT,
    @DeletedBy            BIGINT = NULL,
    @RestoreUntil         DATETIME2(3) = NULL,
    @AffectedRows         INT OUTPUT,
    @PrimaryClearedCount  INT OUTPUT,
    @NewVersion           INT OUTPUT,
    @ResultCode           INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @AffectedRows = 0;
    SET @PrimaryClearedCount = 0;
    SET @NewVersion = NULL;
    SET @ResultCode = 0;

    DECLARE @AffectedArticles TABLE
    (
        [ArticleId] BIGINT NOT NULL PRIMARY KEY
    );

    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [media].[MediaAsset] WITH (UPDLOCK, HOLDLOCK) WHERE [MediaId] = @MediaId)
    BEGIN
        SET @ResultCode = 1;
        COMMIT TRANSACTION;
        RETURN;
    END

    INSERT INTO @AffectedArticles ([ArticleId])
    SELECT DISTINCT [ArticleId]
    FROM [media].[ArticleMedia] WITH (UPDLOCK, HOLDLOCK)
    WHERE [MediaId] = @MediaId
      AND [IsDeleted] = 0
      AND [IsPrimary] = 1;

    UPDATE [media].[ArticleMedia]
    SET
        [IsPrimary] = 0,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedBy] = @DeletedBy,
        [Version] = [Version] + 1
    WHERE [MediaId] = @MediaId
      AND [IsDeleted] = 0
      AND [IsPrimary] = 1;

    SET @PrimaryClearedCount = @@ROWCOUNT;

    IF @PrimaryClearedCount > 0
    BEGIN
        UPDATE AMS
        SET
            [Version] = [Version] + 1,
            [UpdatedAt] = SYSUTCDATETIME(),
            [UpdatedBy] = @DeletedBy
        FROM [media].[ArticleMediaSet] AMS
        INNER JOIN @AffectedArticles A
            ON A.[ArticleId] = AMS.[ArticleId];
    END

    UPDATE [media].[MediaAsset]
    SET
        [IsDeleted] = 1,
        [DeletedAt] = SYSUTCDATETIME(),
        [DeletedBy] = @DeletedBy,
        [RestoreUntil] = @RestoreUntil,
        [RestoredAt] = NULL,
        [RestoredBy] = NULL,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedBy] = @DeletedBy,
        [Version] = [Version] + 1
    WHERE [MediaId] = @MediaId
      AND [IsDeleted] = 0;

    SET @AffectedRows = @@ROWCOUNT;

    SELECT @NewVersion = [Version]
    FROM [media].[MediaAsset]
    WHERE [MediaId] = @MediaId;

    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE [media].[Media_MediaAsset_Restore]
    @MediaId        BIGINT,
    @RestoredBy     BIGINT = NULL,
    @AffectedRows   INT OUTPUT,
    @NewVersion     INT OUTPUT,
    @ResultCode     INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @AffectedRows = 0;
    SET @NewVersion = NULL;
    SET @ResultCode = 0;

    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [media].[MediaAsset] WITH (UPDLOCK, HOLDLOCK) WHERE [MediaId] = @MediaId)
    BEGIN
        SET @ResultCode = 1;
        COMMIT TRANSACTION;
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM [media].[MediaAsset] WHERE [MediaId] = @MediaId AND [IsDeleted] = 0)
    BEGIN
        SELECT @NewVersion = [Version]
        FROM [media].[MediaAsset]
        WHERE [MediaId] = @MediaId;

        COMMIT TRANSACTION;
        RETURN;
    END

    IF EXISTS
    (
        SELECT 1
        FROM [media].[MediaAsset]
        WHERE [MediaId] = @MediaId
          AND [IsDeleted] = 1
          AND [RestoreUntil] IS NOT NULL
          AND [RestoreUntil] < SYSUTCDATETIME()
    )
    BEGIN
        SET @ResultCode = 2;

        SELECT @NewVersion = [Version]
        FROM [media].[MediaAsset]
        WHERE [MediaId] = @MediaId;

        COMMIT TRANSACTION;
        RETURN;
    END

    UPDATE [media].[MediaAsset]
    SET
        [IsDeleted] = 0,
        [DeletedAt] = NULL,
        [DeletedBy] = NULL,
        [RestoreUntil] = NULL,
        [RestoredAt] = SYSUTCDATETIME(),
        [RestoredBy] = @RestoredBy,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedBy] = @RestoredBy,
        [Version] = [Version] + 1
    WHERE [MediaId] = @MediaId
      AND [IsDeleted] = 1;

    SET @AffectedRows = @@ROWCOUNT;

    SELECT @NewVersion = [Version]
    FROM [media].[MediaAsset]
    WHERE [MediaId] = @MediaId;

    COMMIT TRANSACTION;
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
        [RestoreUntil],
        [RestoredAt],
        [RestoredBy]
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
   ArticleMediaSet
   ========================================================= */

CREATE OR ALTER PROCEDURE [media].[Media_ArticleMediaSet_SelectByArticleId]
    @ArticleId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [ArticleId],
        [Version],
        [CreatedAt],
        [CreatedBy],
        [UpdatedAt],
        [UpdatedBy]
    FROM [media].[ArticleMediaSet]
    WHERE [ArticleId] = @ArticleId;
END;
GO

CREATE OR ALTER PROCEDURE [media].[Media_ArticleMediaSet_Ensure]
    @ArticleId BIGINT,
    @ActorUserId BIGINT = NULL,
    @Version INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [media].[ArticleMediaSet] WITH (UPDLOCK, HOLDLOCK)
        WHERE [ArticleId] = @ArticleId
    )
    BEGIN
        INSERT INTO [media].[ArticleMediaSet]
        (
            [ArticleId],
            [Version],
            [CreatedAt],
            [CreatedBy],
            [UpdatedAt],
            [UpdatedBy]
        )
        VALUES
        (
            @ArticleId,
            0,
            SYSUTCDATETIME(),
            @ActorUserId,
            SYSUTCDATETIME(),
            @ActorUserId
        );
    END

    SELECT @Version = [Version]
    FROM [media].[ArticleMediaSet]
    WHERE [ArticleId] = @ArticleId;

    COMMIT TRANSACTION;
END;
GO

/* =========================================================
   ArticleMedia
   ========================================================= */

CREATE OR ALTER PROCEDURE [media].[Media_ArticleMedia_Attach]
    @ArticleId       BIGINT,
    @MediaId         BIGINT,
    @IsPrimary       BIT = 0,
    @CreatedBy       BIGINT = NULL,
    @ArticleMediaId  BIGINT OUTPUT,
    @AffectedRows    INT OUTPUT,
    @PrimaryChanged  BIT OUTPUT,
    @NewVersion      INT OUTPUT,
    @ResultCode      INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @ArticleMediaId = NULL;
    SET @AffectedRows = 0;
    SET @PrimaryChanged = 0;
    SET @NewVersion = NULL;
    SET @ResultCode = 0;

    DECLARE @ExistingIsDeleted BIT = NULL;
    DECLARE @ExistingIsPrimary BIT = NULL;
    DECLARE @RelationChanged BIT = 0;
    DECLARE @UnsetRows INT = 0;
    DECLARE @SetRows INT = 0;
    DECLARE @NextSortOrder INT;

    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [media].[MediaAsset] WITH (UPDLOCK, HOLDLOCK) WHERE [MediaId] = @MediaId)
    BEGIN
        SET @ResultCode = 1;
        COMMIT TRANSACTION;
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM [media].[MediaAsset] WHERE [MediaId] = @MediaId AND [IsDeleted] = 1)
    BEGIN
        SET @ResultCode = 2;
        COMMIT TRANSACTION;
        RETURN;
    END

    IF @IsPrimary = 1
       AND NOT EXISTS
       (
           SELECT 1
           FROM [media].[MediaAsset]
           WHERE [MediaId] = @MediaId
             AND [MediaType] = 'Image'
       )
    BEGIN
        SET @ResultCode = 7;
        COMMIT TRANSACTION;
        RETURN;
    END

    IF NOT EXISTS
    (
        SELECT 1
        FROM [media].[ArticleMediaSet] WITH (UPDLOCK, HOLDLOCK)
        WHERE [ArticleId] = @ArticleId
    )
    BEGIN
        INSERT INTO [media].[ArticleMediaSet]
        (
            [ArticleId],
            [Version],
            [CreatedAt],
            [CreatedBy],
            [UpdatedAt],
            [UpdatedBy]
        )
        VALUES
        (
            @ArticleId,
            0,
            SYSUTCDATETIME(),
            @CreatedBy,
            SYSUTCDATETIME(),
            @CreatedBy
        );
    END

    SELECT TOP (1)
        @ArticleMediaId = [ArticleMediaId],
        @ExistingIsDeleted = [IsDeleted],
        @ExistingIsPrimary = [IsPrimary]
    FROM [media].[ArticleMedia] WITH (UPDLOCK, HOLDLOCK)
    WHERE [ArticleId] = @ArticleId
      AND [MediaId] = @MediaId;

    IF @ArticleMediaId IS NOT NULL
    BEGIN
        IF @ExistingIsDeleted = 1
        BEGIN
            SELECT @NextSortOrder = ISNULL(MAX([SortOrder]), -1) + 1
            FROM [media].[ArticleMedia]
            WHERE [ArticleId] = @ArticleId
              AND [IsDeleted] = 0;

            UPDATE [media].[ArticleMedia]
            SET
                [SortOrder] = @NextSortOrder,
                [IsDeleted] = 0,
                [DeletedAt] = NULL,
                [DeletedBy] = NULL,
                [UpdatedAt] = SYSUTCDATETIME(),
                [UpdatedBy] = @CreatedBy,
                [Version] = [Version] + 1
            WHERE [ArticleMediaId] = @ArticleMediaId
              AND [IsDeleted] = 1;

            SET @RelationChanged = CASE WHEN @@ROWCOUNT > 0 THEN 1 ELSE 0 END;
            SET @ExistingIsPrimary = 0;
        END
    END
    ELSE
    BEGIN
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
        SET @RelationChanged = 1;
        SET @ExistingIsPrimary = 0;
    END

    IF @IsPrimary = 1 AND ISNULL(@ExistingIsPrimary, 0) = 0
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

        SET @UnsetRows = @@ROWCOUNT;

        UPDATE [media].[ArticleMedia]
        SET
            [IsPrimary] = 1,
            [UpdatedAt] = SYSUTCDATETIME(),
            [UpdatedBy] = @CreatedBy,
            [Version] = [Version] + 1
        WHERE [ArticleId] = @ArticleId
          AND [MediaId] = @MediaId
          AND [IsDeleted] = 0
          AND [IsPrimary] = 0;

        SET @SetRows = @@ROWCOUNT;
        SET @PrimaryChanged = CASE WHEN (@UnsetRows + @SetRows) > 0 THEN 1 ELSE 0 END;
    END

    SET @AffectedRows = CONVERT(INT, @RelationChanged) + @UnsetRows + @SetRows;

    IF @AffectedRows > 0
    BEGIN
        UPDATE [media].[ArticleMediaSet]
        SET
            [Version] = [Version] + 1,
            [UpdatedAt] = SYSUTCDATETIME(),
            [UpdatedBy] = @CreatedBy
        WHERE [ArticleId] = @ArticleId;
    END

    SELECT @NewVersion = [Version]
    FROM [media].[ArticleMediaSet]
    WHERE [ArticleId] = @ArticleId;

    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE [media].[Media_ArticleMedia_Detach]
    @ArticleId       BIGINT,
    @MediaId         BIGINT,
    @DeletedBy       BIGINT = NULL,
    @AffectedRows    INT OUTPUT,
    @PrimaryCleared  BIT OUTPUT,
    @NewVersion      INT OUTPUT,
    @ResultCode      INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @AffectedRows = 0;
    SET @PrimaryCleared = 0;
    SET @NewVersion = NULL;
    SET @ResultCode = 0;

    DECLARE @CurrentIsPrimary BIT = 0;

    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [media].[ArticleMediaSet] WITH (UPDLOCK, HOLDLOCK) WHERE [ArticleId] = @ArticleId)
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END

    SELECT TOP (1)
        @CurrentIsPrimary = [IsPrimary]
    FROM [media].[ArticleMedia] WITH (UPDLOCK, HOLDLOCK)
    WHERE [ArticleId] = @ArticleId
      AND [MediaId] = @MediaId
      AND [IsDeleted] = 0;

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
    SET @PrimaryCleared = CASE WHEN @AffectedRows > 0 AND @CurrentIsPrimary = 1 THEN 1 ELSE 0 END;

    IF @AffectedRows > 0
    BEGIN
        UPDATE [media].[ArticleMediaSet]
        SET
            [Version] = [Version] + 1,
            [UpdatedAt] = SYSUTCDATETIME(),
            [UpdatedBy] = @DeletedBy
        WHERE [ArticleId] = @ArticleId;
    END

    SELECT @NewVersion = [Version]
    FROM [media].[ArticleMediaSet]
    WHERE [ArticleId] = @ArticleId;

    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE [media].[Media_ArticleMedia_Restore]
    @ArticleId      BIGINT,
    @MediaId        BIGINT,
    @RestoredBy     BIGINT = NULL,
    @AffectedRows   INT OUTPUT,
    @NewVersion     INT OUTPUT,
    @ResultCode     INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @AffectedRows = 0;
    SET @NewVersion = NULL;
    SET @ResultCode = 0;

    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [media].[ArticleMediaSet] WITH (UPDLOCK, HOLDLOCK) WHERE [ArticleId] = @ArticleId)
    BEGIN
        SET @ResultCode = 1;
        COMMIT TRANSACTION;
        RETURN;
    END

    IF EXISTS
    (
        SELECT 1
        FROM [media].[MediaAsset]
        WHERE [MediaId] = @MediaId
          AND [IsDeleted] = 1
    )
    BEGIN
        SET @ResultCode = 2;

        SELECT @NewVersion = [Version]
        FROM [media].[ArticleMediaSet]
        WHERE [ArticleId] = @ArticleId;

        COMMIT TRANSACTION;
        RETURN;
    END

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

    IF @AffectedRows > 0
    BEGIN
        UPDATE [media].[ArticleMediaSet]
        SET
            [Version] = [Version] + 1,
            [UpdatedAt] = SYSUTCDATETIME(),
            [UpdatedBy] = @RestoredBy
        WHERE [ArticleId] = @ArticleId;
    END

    SELECT @NewVersion = [Version]
    FROM [media].[ArticleMediaSet]
    WHERE [ArticleId] = @ArticleId;

    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE [media].[Media_ArticleMedia_SetPrimary]
    @ArticleId        BIGINT,
    @MediaId          BIGINT,
    @ExpectedVersion  INT = NULL,
    @UpdatedBy        BIGINT = NULL,
    @AffectedRows     INT OUTPUT,
    @NewVersion       INT OUTPUT,
    @ResultCode       INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @AffectedRows = 0;
    SET @NewVersion = NULL;
    SET @ResultCode = 0;

    DECLARE @CurrentVersion INT;
    DECLARE @CurrentIsPrimary BIT;
    DECLARE @MediaIsDeleted BIT;
    DECLARE @MediaType VARCHAR(20);
    DECLARE @UnsetRows INT = 0;
    DECLARE @SetRows INT = 0;

    BEGIN TRANSACTION;

    SELECT @CurrentVersion = [Version]
    FROM [media].[ArticleMediaSet] WITH (UPDLOCK, HOLDLOCK)
    WHERE [ArticleId] = @ArticleId;

    IF @CurrentVersion IS NULL
    BEGIN
        SET @ResultCode = 1;
        COMMIT TRANSACTION;
        RETURN;
    END

    IF @ExpectedVersion IS NULL
    BEGIN
        SET @ResultCode = 6;
        SET @NewVersion = @CurrentVersion;
        COMMIT TRANSACTION;
        RETURN;
    END

    IF @CurrentVersion <> @ExpectedVersion
    BEGIN
        SET @ResultCode = 3;
        SET @NewVersion = @CurrentVersion;
        COMMIT TRANSACTION;
        RETURN;
    END

    SELECT TOP (1)
        @CurrentIsPrimary = AM.[IsPrimary],
        @MediaIsDeleted = MA.[IsDeleted],
        @MediaType = MA.[MediaType]
    FROM [media].[ArticleMedia] AM WITH (UPDLOCK, HOLDLOCK)
    INNER JOIN [media].[MediaAsset] MA WITH (UPDLOCK, HOLDLOCK)
        ON MA.[MediaId] = AM.[MediaId]
    WHERE AM.[ArticleId] = @ArticleId
      AND AM.[MediaId] = @MediaId
      AND AM.[IsDeleted] = 0;

    IF @CurrentIsPrimary IS NULL
    BEGIN
        SET @ResultCode = 1;
        SET @NewVersion = @CurrentVersion;
        COMMIT TRANSACTION;
        RETURN;
    END

    IF @MediaIsDeleted = 1
    BEGIN
        SET @ResultCode = 2;
        SET @NewVersion = @CurrentVersion;
        COMMIT TRANSACTION;
        RETURN;
    END

    IF @MediaType <> 'Image'
    BEGIN
        SET @ResultCode = 7;
        SET @NewVersion = @CurrentVersion;
        COMMIT TRANSACTION;
        RETURN;
    END

    IF @CurrentIsPrimary = 1
    BEGIN
        SET @NewVersion = @CurrentVersion;
        COMMIT TRANSACTION;
        RETURN;
    END

    UPDATE [media].[ArticleMedia]
    SET
        [IsPrimary] = 0,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedBy] = @UpdatedBy,
        [Version] = [Version] + 1
    WHERE [ArticleId] = @ArticleId
      AND [IsDeleted] = 0
      AND [IsPrimary] = 1;

    SET @UnsetRows = @@ROWCOUNT;

    UPDATE [media].[ArticleMedia]
    SET
        [IsPrimary] = 1,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedBy] = @UpdatedBy,
        [Version] = [Version] + 1
    WHERE [ArticleId] = @ArticleId
      AND [MediaId] = @MediaId
      AND [IsDeleted] = 0
      AND [IsPrimary] = 0;

    SET @SetRows = @@ROWCOUNT;
    SET @AffectedRows = @UnsetRows + @SetRows;

    IF @SetRows = 0
    BEGIN
        SET @ResultCode = 2;
        SET @NewVersion = @CurrentVersion;
        ROLLBACK TRANSACTION;
        RETURN;
    END

    UPDATE [media].[ArticleMediaSet]
    SET
        [Version] = [Version] + 1,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedBy] = @UpdatedBy
    WHERE [ArticleId] = @ArticleId;

    SELECT @NewVersion = [Version]
    FROM [media].[ArticleMediaSet]
    WHERE [ArticleId] = @ArticleId;

    COMMIT TRANSACTION;
END;
GO

/* =========================================================
   ArticleMedia TVP for reorder
   ========================================================= */

IF OBJECT_ID(N'[media].[Media_ArticleMedia_ReorderByIds]', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE [media].[Media_ArticleMedia_ReorderByIds];
END
GO

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
    @ArticleId        BIGINT,
    @ExpectedVersion  INT = NULL,
    @UpdatedBy        BIGINT = NULL,
    @Orders           [media].[MediaOrderListType] READONLY,
    @AffectedRows     INT OUTPUT,
    @NewVersion       INT OUTPUT,
    @ResultCode       INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @AffectedRows = 0;
    SET @NewVersion = NULL;
    SET @ResultCode = 0;

    DECLARE @CurrentVersion INT;
    DECLARE @OrderCount INT;
    DECLARE @DistinctMediaCount INT;
    DECLARE @DistinctSortOrderCount INT;
    DECLARE @ActiveCount INT;
    DECLARE @InvalidItemCount INT;

    BEGIN TRANSACTION;

    SELECT @CurrentVersion = [Version]
    FROM [media].[ArticleMediaSet] WITH (UPDLOCK, HOLDLOCK)
    WHERE [ArticleId] = @ArticleId;

    IF @CurrentVersion IS NULL
    BEGIN
        SET @ResultCode = 1;
        COMMIT TRANSACTION;
        RETURN;
    END

    IF @ExpectedVersion IS NULL
    BEGIN
        SET @ResultCode = 6;
        SET @NewVersion = @CurrentVersion;
        COMMIT TRANSACTION;
        RETURN;
    END

    IF @CurrentVersion <> @ExpectedVersion
    BEGIN
        SET @ResultCode = 3;
        SET @NewVersion = @CurrentVersion;
        COMMIT TRANSACTION;
        RETURN;
    END

    SELECT
        @OrderCount = COUNT(1),
        @DistinctMediaCount = COUNT(DISTINCT [MediaId]),
        @DistinctSortOrderCount = COUNT(DISTINCT [SortOrder])
    FROM @Orders;

    IF @OrderCount = 0
    BEGIN
        SET @ResultCode = 4;
        SET @NewVersion = @CurrentVersion;
        COMMIT TRANSACTION;
        RETURN;
    END

    SELECT @ActiveCount = COUNT(1)
    FROM [media].[ArticleMedia]
    WHERE [ArticleId] = @ArticleId
      AND [IsDeleted] = 0;

    SELECT @InvalidItemCount = COUNT(1)
    FROM @Orders O
    WHERE O.[SortOrder] < 0
       OR NOT EXISTS
       (
           SELECT 1
           FROM [media].[ArticleMedia] AM
           WHERE AM.[ArticleId] = @ArticleId
             AND AM.[MediaId] = O.[MediaId]
             AND AM.[IsDeleted] = 0
       );

    IF @OrderCount <> @ActiveCount
       OR @DistinctMediaCount <> @OrderCount
       OR @DistinctSortOrderCount <> @OrderCount
       OR @InvalidItemCount > 0
    BEGIN
        SET @ResultCode = 4;
        SET @NewVersion = @CurrentVersion;
        COMMIT TRANSACTION;
        RETURN;
    END

    UPDATE AM
    SET
        [SortOrder] = O.[SortOrder],
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedBy] = @UpdatedBy,
        [Version] = AM.[Version] + 1
    FROM [media].[ArticleMedia] AM
    INNER JOIN @Orders O
        ON O.[MediaId] = AM.[MediaId]
    WHERE AM.[ArticleId] = @ArticleId
      AND AM.[IsDeleted] = 0
      AND AM.[SortOrder] <> O.[SortOrder];

    SET @AffectedRows = @@ROWCOUNT;

    IF @AffectedRows > 0
    BEGIN
        UPDATE [media].[ArticleMediaSet]
        SET
            [Version] = [Version] + 1,
            [UpdatedAt] = SYSUTCDATETIME(),
            [UpdatedBy] = @UpdatedBy
        WHERE [ArticleId] = @ArticleId;
    END

    SELECT @NewVersion = [Version]
    FROM [media].[ArticleMediaSet]
    WHERE [ArticleId] = @ArticleId;

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
        AMS.[Version] AS [AttachmentSetVersion],
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
        MA.[IsDeleted] AS [MediaIsDeleted],
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
    INNER JOIN [media].[ArticleMediaSet] AMS
        ON AMS.[ArticleId] = AM.[ArticleId]
    INNER JOIN [media].[MediaAsset] MA
        ON MA.[MediaId] = AM.[MediaId]
    WHERE AM.[ArticleMediaId] = @ArticleMediaId;
END;
GO

CREATE OR ALTER PROCEDURE [media].[Media_ArticleMedia_SelectListItemById]
    @ArticleMediaId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        AM.[ArticleMediaId],
        AM.[ArticleId],
        AMS.[Version] AS [AttachmentSetVersion],
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
        MA.[IsDeleted] AS [MediaIsDeleted],
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
    INNER JOIN [media].[ArticleMediaSet] AMS
        ON AMS.[ArticleId] = AM.[ArticleId]
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
        AMS.[Version] AS [AttachmentSetVersion],
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
        MA.[IsDeleted] AS [MediaIsDeleted],
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
    INNER JOIN [media].[ArticleMediaSet] AMS
        ON AMS.[ArticleId] = AM.[ArticleId]
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
        AMS.[Version] AS [AttachmentSetVersion],
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
    INNER JOIN [media].[ArticleMediaSet] AMS
        ON AMS.[ArticleId] = AM.[ArticleId]
    INNER JOIN [media].[MediaAsset] MA
        ON MA.[MediaId] = AM.[MediaId]
    WHERE AM.[ArticleId] = @ArticleId
      AND AM.[IsDeleted] = 0
      AND AM.[IsPrimary] = 1
      AND MA.[IsDeleted] = 0
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
        AMS.[Version] AS [AttachmentSetVersion],
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
    INNER JOIN [media].[ArticleMediaSet] AMS
        ON AMS.[ArticleId] = AM.[ArticleId]
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

    DECLARE @OrderByExpression NVARCHAR(100);

    SET @OrderByExpression =
        CASE @SortBy
            WHEN N'CreatedAt' THEN N'AM.[CreatedAt]'
            WHEN N'UpdatedAt' THEN N'AM.[UpdatedAt]'
            WHEN N'MediaId' THEN N'AM.[MediaId]'
            ELSE N'AM.[SortOrder]'
        END;

    DECLARE @Sql NVARCHAR(MAX) =
    N'
    SELECT
        AM.[ArticleMediaId],
        AM.[ArticleId],
        AMS.[Version] AS [AttachmentSetVersion],
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
        MA.[IsDeleted] AS [MediaIsDeleted],
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
    INNER JOIN [media].[ArticleMediaSet] AMS
        ON AMS.[ArticleId] = AM.[ArticleId]
    INNER JOIN [media].[MediaAsset] MA
        ON MA.[MediaId] = AM.[MediaId]
    WHERE AM.[ArticleId] = @ArticleId
      AND (@IncludeDeleted = 1 OR AM.[IsDeleted] = 0)
    ORDER BY ' + @OrderByExpression + N' ' + @SortDirection + N', AM.[ArticleMediaId] ASC
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
