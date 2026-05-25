/*
  File: db/10_modules/reading/001_tables.sql
  Module: Reading
  Purpose:
  - Create Reading derived projection tables for CommercialNews V1.
  - Reading owns public serving projections, not source truth.
  - Core derived read model:
      * ArticleReadModel
      * ArticleReadModelTag
      * ArticleReadModelMedia
      * ArticleMediaProjectionState
      * ArticleSeoRouteProjection
      * ArticleSeoMetadataProjection
      * AuthorProfileProjection
  - Support:
      * public article list/detail/search/related reads
      * slug-based public serving
      * source-derived visibility
      * optional projected tags/media
      * counters as derived values
      * idempotent async projection apply via SourceVersion + LastEventMessageId
      * rebuild/reconciliation posture

  Notes:
  - Content remains source of truth for article lifecycle/editorial data.
  - SEO remains source of truth for slug/canonical metadata.
  - Media remains source of truth for media assets.
  - Identity remains source of truth for public author profile data.
  - Interaction remains source of truth for counters/engagement.
  - Reading projection tables intentionally avoid cross-module FKs.
  - Index-heavy tuning belongs in 010_indexes.sql.
  - Stored procedures belong in 020_procs.sql.
  - Idempotent: safe to re-run.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 56001, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

USE [CommercialNews];
GO

IF SCHEMA_ID(N'reading') IS NULL
BEGIN
    THROW 56002, 'Schema [reading] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

/* =========================================================
   1) [reading].[ArticleReadModel]
   ========================================================= */
IF OBJECT_ID(N'[reading].[ArticleReadModel]', N'U') IS NULL
BEGIN
    CREATE TABLE [reading].[ArticleReadModel]
    (
        [ArticleId]                 BIGINT               NOT NULL,
        [ArticlePublicId]           CHAR(26)             NOT NULL, -- ULID from Content

        [Slug]                      NVARCHAR(200)        NULL,

        [Title]                     NVARCHAR(300)        NOT NULL,
        [Summary]                   NVARCHAR(1000)       NOT NULL,
        [Body]                      NVARCHAR(MAX)        NOT NULL,

        [CategoryId]                BIGINT               NULL,
        [CategoryName]              NVARCHAR(200)        NULL,

        [AuthorUserId]              BIGINT               NULL,
        [AuthorDisplayName]         NVARCHAR(200)        NULL,

        [CoverMediaId]              BIGINT               NULL,
        [CoverMediaUrl]             NVARCHAR(1000)       NULL,
        [CoverAlt]                  NVARCHAR(300)        NULL,

        [CanonicalUrl]              NVARCHAR(500)        NULL,
        [MetaTitle]                 NVARCHAR(300)        NULL,
        [MetaDescription]           NVARCHAR(500)        NULL,
        [OgTitle]                   NVARCHAR(300)        NULL,
        [OgDescription]             NVARCHAR(500)        NULL,
        [OgImageUrl]                NVARCHAR(800)        NULL,
        [TwitterTitle]              NVARCHAR(300)        NULL,
        [TwitterDescription]        NVARCHAR(500)        NULL,
        [TwitterImageUrl]           NVARCHAR(800)        NULL,
        [Robots]                    NVARCHAR(100)        NULL,
        [SeoIsManualOverride]       BIT                  NOT NULL
            CONSTRAINT [DF_ArticleReadModel_SeoIsManualOverride] DEFAULT (0),
        [SeoRouteIsActive]          BIT                  NOT NULL
            CONSTRAINT [DF_ArticleReadModel_SeoRouteIsActive] DEFAULT (0),
        [SeoIsIndexable]            BIT                  NOT NULL
            CONSTRAINT [DF_ArticleReadModel_SeoIsIndexable] DEFAULT (0),

        [Status]                    NVARCHAR(30)         NOT NULL,
        [IsPublic]                  BIT                  NOT NULL
            CONSTRAINT [DF_ArticleReadModel_IsPublic] DEFAULT (0),

        [PublishedAtUtc]            DATETIME2(3)         NULL,
        [UpdatedAtUtc]              DATETIME2(3)         NOT NULL,

        [SearchText]                NVARCHAR(MAX)        NULL,

        [ViewCount]                 BIGINT               NOT NULL
            CONSTRAINT [DF_ArticleReadModel_ViewCount] DEFAULT (0),
        [LikeCount]                 BIGINT               NOT NULL
            CONSTRAINT [DF_ArticleReadModel_LikeCount] DEFAULT (0),
        [CommentCount]              BIGINT               NOT NULL
            CONSTRAINT [DF_ArticleReadModel_CommentCount] DEFAULT (0),
        [PopularityScore]           FLOAT                NULL,

        [SourceVersion]             BIGINT               NOT NULL
            CONSTRAINT [DF_ArticleReadModel_SourceVersion] DEFAULT (0),

        [LastEventMessageId]        CHAR(26)             NULL,
        [LastSourceOccurredAtUtc]   DATETIME2(3)         NULL,
        [LastSyncedAtUtc]           DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_ArticleReadModel_LastSyncedAtUtc] DEFAULT (SYSUTCDATETIME()),

        [CreatedAtUtc]              DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_ArticleReadModel_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [PK_ArticleReadModel]
            PRIMARY KEY CLUSTERED ([ArticleId] ASC),

        CONSTRAINT [UQ_ArticleReadModel_ArticlePublicId]
            UNIQUE ([ArticlePublicId]),

        CONSTRAINT [CK_ArticleReadModel_ArticlePublicId_Length]
            CHECK (LEN([ArticlePublicId]) = 26),

        CONSTRAINT [CK_ArticleReadModel_LastEventMessageId_Length]
            CHECK ([LastEventMessageId] IS NULL OR LEN([LastEventMessageId]) = 26),

        CONSTRAINT [CK_ArticleReadModel_Title_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([Title]))) > 0),

        CONSTRAINT [CK_ArticleReadModel_Summary_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([Summary]))) > 0),

        CONSTRAINT [CK_ArticleReadModel_Body_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([Body]))) > 0),

        CONSTRAINT [CK_ArticleReadModel_Status]
            CHECK ([Status] IN (N'Draft', N'Published', N'Archived')),

        CONSTRAINT [CK_ArticleReadModel_PublicRequiresPublished]
            CHECK (
                [IsPublic] = 0
                OR ([Status] = N'Published' AND [PublishedAtUtc] IS NOT NULL)
            ),

        CONSTRAINT [CK_ArticleReadModel_LastSyncedAtUtc]
            CHECK ([LastSyncedAtUtc] >= [CreatedAtUtc]),

        CONSTRAINT [CK_ArticleReadModel_SourceVersion_NonNegative]
            CHECK ([SourceVersion] >= 0),

        CONSTRAINT [CK_ArticleReadModel_Counters_NonNegative]
            CHECK (
                [ViewCount] >= 0
                AND [LikeCount] >= 0
                AND [CommentCount] >= 0
            ),

        CONSTRAINT [CK_ArticleReadModel_SeoIndexableRequiresActive]
            CHECK (
                [SeoIsIndexable] = 0
                OR [SeoRouteIsActive] = 1
            )
    );

    PRINT N'Created table: [reading].[ArticleReadModel]';
END
ELSE
BEGIN
    PRINT N'Table exists: [reading].[ArticleReadModel]';
END
GO

/*
  Source timestamps are owned by Content and may legitimately be earlier than
  the Reading projection row creation time. Older databases may still have
  these projection/source timestamp comparison constraints from an earlier
  draft, so drop them idempotently.
*/
IF OBJECT_ID(N'[reading].[CK_ArticleReadModel_UpdatedAtUtc]', N'C') IS NOT NULL
BEGIN
    ALTER TABLE [reading].[ArticleReadModel]
        DROP CONSTRAINT [CK_ArticleReadModel_UpdatedAtUtc];

    PRINT N'Dropped obsolete constraint: [CK_ArticleReadModel_UpdatedAtUtc]';
END
GO

IF OBJECT_ID(N'[reading].[CK_ArticleReadModel_PublishedAtUtc]', N'C') IS NOT NULL
BEGIN
    ALTER TABLE [reading].[ArticleReadModel]
        DROP CONSTRAINT [CK_ArticleReadModel_PublishedAtUtc];

    PRINT N'Dropped obsolete constraint: [CK_ArticleReadModel_PublishedAtUtc]';
END
GO

/* =========================================================
   ArticleReadModel - SEO enrichment columns for existing DB
   ========================================================= */
IF COL_LENGTH(N'reading.ArticleReadModel', N'OgTitle') IS NULL
BEGIN
    ALTER TABLE [reading].[ArticleReadModel]
        ADD [OgTitle] NVARCHAR(300) NULL;

    PRINT N'Added column: [reading].[ArticleReadModel].[OgTitle]';
END
GO

IF COL_LENGTH(N'reading.ArticleReadModel', N'OgDescription') IS NULL
BEGIN
    ALTER TABLE [reading].[ArticleReadModel]
        ADD [OgDescription] NVARCHAR(500) NULL;

    PRINT N'Added column: [reading].[ArticleReadModel].[OgDescription]';
END
GO

IF COL_LENGTH(N'reading.ArticleReadModel', N'OgImageUrl') IS NULL
BEGIN
    ALTER TABLE [reading].[ArticleReadModel]
        ADD [OgImageUrl] NVARCHAR(800) NULL;

    PRINT N'Added column: [reading].[ArticleReadModel].[OgImageUrl]';
END
GO

IF COL_LENGTH(N'reading.ArticleReadModel', N'TwitterTitle') IS NULL
BEGIN
    ALTER TABLE [reading].[ArticleReadModel]
        ADD [TwitterTitle] NVARCHAR(300) NULL;

    PRINT N'Added column: [reading].[ArticleReadModel].[TwitterTitle]';
END
GO

IF COL_LENGTH(N'reading.ArticleReadModel', N'TwitterDescription') IS NULL
BEGIN
    ALTER TABLE [reading].[ArticleReadModel]
        ADD [TwitterDescription] NVARCHAR(500) NULL;

    PRINT N'Added column: [reading].[ArticleReadModel].[TwitterDescription]';
END
GO

IF COL_LENGTH(N'reading.ArticleReadModel', N'TwitterImageUrl') IS NULL
BEGIN
    ALTER TABLE [reading].[ArticleReadModel]
        ADD [TwitterImageUrl] NVARCHAR(800) NULL;

    PRINT N'Added column: [reading].[ArticleReadModel].[TwitterImageUrl]';
END
GO

IF COL_LENGTH(N'reading.ArticleReadModel', N'Robots') IS NULL
BEGIN
    ALTER TABLE [reading].[ArticleReadModel]
        ADD [Robots] NVARCHAR(100) NULL;

    PRINT N'Added column: [reading].[ArticleReadModel].[Robots]';
END
GO

IF COL_LENGTH(N'reading.ArticleReadModel', N'SeoIsManualOverride') IS NULL
BEGIN
    ALTER TABLE [reading].[ArticleReadModel]
        ADD [SeoIsManualOverride] BIT NOT NULL
            CONSTRAINT [DF_ArticleReadModel_SeoIsManualOverride] DEFAULT (0);

    PRINT N'Added column: [reading].[ArticleReadModel].[SeoIsManualOverride]';
END
GO

IF COL_LENGTH(N'reading.ArticleReadModel', N'SeoRouteIsActive') IS NULL
BEGIN
    ALTER TABLE [reading].[ArticleReadModel]
        ADD [SeoRouteIsActive] BIT NOT NULL
            CONSTRAINT [DF_ArticleReadModel_SeoRouteIsActive] DEFAULT (0);

    PRINT N'Added column: [reading].[ArticleReadModel].[SeoRouteIsActive]';
END
GO

IF COL_LENGTH(N'reading.ArticleReadModel', N'SeoIsIndexable') IS NULL
BEGIN
    ALTER TABLE [reading].[ArticleReadModel]
        ADD [SeoIsIndexable] BIT NOT NULL
            CONSTRAINT [DF_ArticleReadModel_SeoIsIndexable] DEFAULT (0);

    PRINT N'Added column: [reading].[ArticleReadModel].[SeoIsIndexable]';
END
GO

IF OBJECT_ID(N'[reading].[CK_ArticleReadModel_SeoIndexableRequiresActive]', N'C') IS NULL
BEGIN
    ALTER TABLE [reading].[ArticleReadModel]
        ADD CONSTRAINT [CK_ArticleReadModel_SeoIndexableRequiresActive]
            CHECK (
                [SeoIsIndexable] = 0
                OR [SeoRouteIsActive] = 1
            );

    PRINT N'Created constraint: [CK_ArticleReadModel_SeoIndexableRequiresActive]';
END
GO

/* =========================================================
   2) [reading].[ArticleReadModelTag]
   ========================================================= */
IF OBJECT_ID(N'[reading].[ArticleReadModelTag]', N'U') IS NULL
BEGIN
    CREATE TABLE [reading].[ArticleReadModelTag]
    (
        [ArticleId]                 BIGINT               NOT NULL,
        [TagId]                     BIGINT               NOT NULL,
        [TagPublicId]               CHAR(26)             NULL,

        [Name]                      NVARCHAR(150)        NOT NULL,
        [Slug]                      NVARCHAR(200)        NULL,

        [SourceVersion]             BIGINT               NOT NULL
            CONSTRAINT [DF_ArticleReadModelTag_SourceVersion] DEFAULT (0),

        [LastSyncedAtUtc]           DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_ArticleReadModelTag_LastSyncedAtUtc] DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [PK_ArticleReadModelTag]
            PRIMARY KEY CLUSTERED ([ArticleId] ASC, [TagId] ASC),

        CONSTRAINT [FK_ArticleReadModelTag_ArticleReadModel]
            FOREIGN KEY ([ArticleId])
            REFERENCES [reading].[ArticleReadModel]([ArticleId])
            ON DELETE CASCADE,

        CONSTRAINT [CK_ArticleReadModelTag_TagPublicId_Length]
            CHECK ([TagPublicId] IS NULL OR LEN([TagPublicId]) = 26),

        CONSTRAINT [CK_ArticleReadModelTag_Name_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),

        CONSTRAINT [CK_ArticleReadModelTag_SourceVersion_NonNegative]
            CHECK ([SourceVersion] >= 0)
    );

    PRINT N'Created table: [reading].[ArticleReadModelTag]';
END
ELSE
BEGIN
    PRINT N'Table exists: [reading].[ArticleReadModelTag]';
END
GO

/* =========================================================
   3) [reading].[ArticleReadModelMedia]
   ========================================================= */
IF OBJECT_ID(N'[reading].[ArticleReadModelMedia]', N'U') IS NULL
BEGIN
    CREATE TABLE [reading].[ArticleReadModelMedia]
    (
        [ArticleId]                 BIGINT               NOT NULL,
        [MediaId]                   BIGINT               NOT NULL,
        [MediaPublicId]             CHAR(26)             NOT NULL,

        [Url]                       NVARCHAR(1000)       NOT NULL,
        [Alt]                       NVARCHAR(300)        NULL,
        [Caption]                   NVARCHAR(300)        NULL,
        [MediaType]                 NVARCHAR(50)         NOT NULL,

        [SortOrder]                 INT                  NOT NULL
            CONSTRAINT [DF_ArticleReadModelMedia_SortOrder] DEFAULT (0),

        [IsPrimary]                 BIT                  NOT NULL
            CONSTRAINT [DF_ArticleReadModelMedia_IsPrimary] DEFAULT (0),

        /*
          For ArticleReadModelMedia, SourceVersion means:
          Media.ArticleMediaSet.AttachmentSetVersion.
          It must not be compared with Content article version
          or MediaAsset version.
        */
        [SourceVersion]             BIGINT               NOT NULL
            CONSTRAINT [DF_ArticleReadModelMedia_SourceVersion] DEFAULT (0),

        [LastSyncedAtUtc]           DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_ArticleReadModelMedia_LastSyncedAtUtc] DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [PK_ArticleReadModelMedia]
            PRIMARY KEY CLUSTERED ([ArticleId] ASC, [MediaId] ASC),

        CONSTRAINT [CK_ArticleReadModelMedia_ArticleId_Positive]
            CHECK ([ArticleId] > 0),

        CONSTRAINT [CK_ArticleReadModelMedia_MediaId_Positive]
            CHECK ([MediaId] > 0),

        CONSTRAINT [CK_ArticleReadModelMedia_MediaPublicId_Length]
            CHECK (LEN([MediaPublicId]) = 26),

        CONSTRAINT [CK_ArticleReadModelMedia_Url_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([Url]))) > 0),

        CONSTRAINT [CK_ArticleReadModelMedia_MediaType_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([MediaType]))) > 0),

        CONSTRAINT [CK_ArticleReadModelMedia_SortOrder_NonNegative]
            CHECK ([SortOrder] >= 0),

        CONSTRAINT [CK_ArticleReadModelMedia_SourceVersion_NonNegative]
            CHECK ([SourceVersion] >= 0)
    );

    PRINT N'Created table: [reading].[ArticleReadModelMedia]';
END
ELSE
BEGIN
    PRINT N'Table exists: [reading].[ArticleReadModelMedia]';
END
GO

/*
  Media attachment projection can arrive before Content article projection.
  Therefore ArticleReadModelMedia intentionally does not require a parent
  ArticleReadModel row.
*/
IF OBJECT_ID(N'[reading].[FK_ArticleReadModelMedia_ArticleReadModel]', N'F') IS NOT NULL
BEGIN
    ALTER TABLE [reading].[ArticleReadModelMedia]
        DROP CONSTRAINT [FK_ArticleReadModelMedia_ArticleReadModel];

    PRINT N'Dropped FK to allow media projection before article publication: [FK_ArticleReadModelMedia_ArticleReadModel]';
END
GO

/* =========================================================
   4) [reading].[ArticleMediaProjectionState]
   ========================================================= */
IF OBJECT_ID(N'[reading].[ArticleMediaProjectionState]', N'U') IS NULL
BEGIN
    CREATE TABLE [reading].[ArticleMediaProjectionState]
    (
        [ArticleId]                 BIGINT               NOT NULL,

        [SourceVersion]             BIGINT               NOT NULL
            CONSTRAINT [DF_ArticleMediaProjectionState_SourceVersion] DEFAULT (0),

        [LastEventMessageId]        CHAR(26)             NULL,
        [LastSourceOccurredAtUtc]   DATETIME2(3)         NULL,

        [LastSyncedAtUtc]           DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_ArticleMediaProjectionState_LastSyncedAtUtc] DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [PK_ArticleMediaProjectionState]
            PRIMARY KEY CLUSTERED ([ArticleId] ASC),

        CONSTRAINT [CK_ArticleMediaProjectionState_ArticleId_Positive]
            CHECK ([ArticleId] > 0),

        CONSTRAINT [CK_ArticleMediaProjectionState_SourceVersion_NonNegative]
            CHECK ([SourceVersion] >= 0),

        CONSTRAINT [CK_ArticleMediaProjectionState_LastEventMessageId_Length]
            CHECK ([LastEventMessageId] IS NULL OR LEN([LastEventMessageId]) = 26)
    );

    PRINT N'Created table: [reading].[ArticleMediaProjectionState]';
END
ELSE
BEGIN
    PRINT N'Table exists: [reading].[ArticleMediaProjectionState]';
END
GO

/* =========================================================
   5) [reading].[ArticleSeoRouteProjection]
   ========================================================= */
IF OBJECT_ID(N'[reading].[ArticleSeoRouteProjection]', N'U') IS NULL
BEGIN
    CREATE TABLE [reading].[ArticleSeoRouteProjection]
    (
        [Scope]                     VARCHAR(30)          NOT NULL,
        [ResourceType]              VARCHAR(50)          NOT NULL,
        [ResourcePublicId]          CHAR(26)             NOT NULL,

        [Slug]                      NVARCHAR(200)        NOT NULL,
        [CanonicalUrl]              NVARCHAR(500)        NULL,

        [IsActive]                  BIT                  NOT NULL
            CONSTRAINT [DF_ArticleSeoRouteProjection_IsActive] DEFAULT (0),

        [IsIndexable]               BIT                  NOT NULL
            CONSTRAINT [DF_ArticleSeoRouteProjection_IsIndexable] DEFAULT (0),

        /*
          SourceVersion means Seo.SlugRegistry.Version.
          It must not be compared with Content article version,
          Media attachment-set version, or SeoMetadata.Version.
        */
        [SourceVersion]             BIGINT               NOT NULL
            CONSTRAINT [DF_ArticleSeoRouteProjection_SourceVersion] DEFAULT (0),

        [LastEventMessageId]        CHAR(26)             NULL,
        [LastSourceOccurredAtUtc]   DATETIME2(3)         NULL,

        [LastSyncedAtUtc]           DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_ArticleSeoRouteProjection_LastSyncedAtUtc]
            DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [PK_ArticleSeoRouteProjection]
            PRIMARY KEY CLUSTERED
            (
                [Scope] ASC,
                [ResourceType] ASC,
                [ResourcePublicId] ASC
            ),

        CONSTRAINT [CK_ArticleSeoRouteProjection_Scope_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([Scope]))) > 0),

        CONSTRAINT [CK_ArticleSeoRouteProjection_ResourceType_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([ResourceType]))) > 0),

        CONSTRAINT [CK_ArticleSeoRouteProjection_ResourcePublicId_Length]
            CHECK (LEN([ResourcePublicId]) = 26),

        CONSTRAINT [CK_ArticleSeoRouteProjection_Slug_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([Slug]))) > 0),

        CONSTRAINT [CK_ArticleSeoRouteProjection_SourceVersion_NonNegative]
            CHECK ([SourceVersion] >= 0),

        CONSTRAINT [CK_ArticleSeoRouteProjection_LastEventMessageId_Length]
            CHECK (
                [LastEventMessageId] IS NULL
                OR LEN([LastEventMessageId]) = 26
            ),

        CONSTRAINT [CK_ArticleSeoRouteProjection_IndexableRequiresActive]
            CHECK (
                [IsIndexable] = 0
                OR [IsActive] = 1
            )
    );

    PRINT N'Created table: [reading].[ArticleSeoRouteProjection]';
END
ELSE
BEGIN
    PRINT N'Table exists: [reading].[ArticleSeoRouteProjection]';
END
GO

/* =========================================================
   6) [reading].[ArticleSeoMetadataProjection]
   ========================================================= */
IF OBJECT_ID(N'[reading].[ArticleSeoMetadataProjection]', N'U') IS NULL
BEGIN
    CREATE TABLE [reading].[ArticleSeoMetadataProjection]
    (
        [Scope]                     VARCHAR(30)          NOT NULL,
        [ResourceType]              VARCHAR(50)          NOT NULL,
        [ResourcePublicId]          CHAR(26)             NOT NULL,

        [MetaTitle]                 NVARCHAR(300)        NULL,
        [MetaDescription]           NVARCHAR(500)        NULL,

        [OgTitle]                   NVARCHAR(300)        NULL,
        [OgDescription]             NVARCHAR(500)        NULL,
        [OgImageUrl]                NVARCHAR(800)        NULL,

        [TwitterTitle]              NVARCHAR(300)        NULL,
        [TwitterDescription]        NVARCHAR(500)        NULL,
        [TwitterImageUrl]           NVARCHAR(800)        NULL,

        [Robots]                    NVARCHAR(100)        NULL,

        [IsManualOverride]          BIT                  NOT NULL
            CONSTRAINT [DF_ArticleSeoMetadataProjection_IsManualOverride]
            DEFAULT (0),

        /*
          SourceVersion means Seo.SeoMetadata.Version.
          It must not be compared with Content article version,
          Media attachment-set version, or SlugRegistry.Version.
        */
        [SourceVersion]             BIGINT               NOT NULL
            CONSTRAINT [DF_ArticleSeoMetadataProjection_SourceVersion] DEFAULT (0),

        [LastEventMessageId]        CHAR(26)             NULL,
        [LastSourceOccurredAtUtc]   DATETIME2(3)         NULL,

        [LastSyncedAtUtc]           DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_ArticleSeoMetadataProjection_LastSyncedAtUtc]
            DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [PK_ArticleSeoMetadataProjection]
            PRIMARY KEY CLUSTERED
            (
                [Scope] ASC,
                [ResourceType] ASC,
                [ResourcePublicId] ASC
            ),

        CONSTRAINT [CK_ArticleSeoMetadataProjection_Scope_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([Scope]))) > 0),

        CONSTRAINT [CK_ArticleSeoMetadataProjection_ResourceType_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([ResourceType]))) > 0),

        CONSTRAINT [CK_ArticleSeoMetadataProjection_ResourcePublicId_Length]
            CHECK (LEN([ResourcePublicId]) = 26),

        CONSTRAINT [CK_ArticleSeoMetadataProjection_SourceVersion_NonNegative]
            CHECK ([SourceVersion] >= 0),

        CONSTRAINT [CK_ArticleSeoMetadataProjection_LastEventMessageId_Length]
            CHECK (
                [LastEventMessageId] IS NULL
                OR LEN([LastEventMessageId]) = 26
            )
    );

    PRINT N'Created table: [reading].[ArticleSeoMetadataProjection]';
END
ELSE
BEGIN
    PRINT N'Table exists: [reading].[ArticleSeoMetadataProjection]';
END
GO

/* =========================================================
   7) [reading].[AuthorProfileProjection]
   ========================================================= */
IF OBJECT_ID(N'[reading].[AuthorProfileProjection]', N'U') IS NULL
BEGIN
    CREATE TABLE [reading].[AuthorProfileProjection]
    (
        [AuthorUserId]              BIGINT               NOT NULL,
        [AuthorUserPublicId]        CHAR(26)             NOT NULL,

        [AuthorDisplayName]         NVARCHAR(200)        NULL,
        [AuthorAvatarUrl]           NVARCHAR(800)        NULL,

        /*
          SourceVersion means Identity.UserAccount.Version.
          It must not be compared with Content article version,
          Media attachment-set version, or SEO projection versions.
        */
        [SourceVersion]             BIGINT               NOT NULL,

        [LastEventMessageId]        CHAR(26)             NOT NULL,
        [LastSourceOccurredAtUtc]   DATETIME2(3)         NOT NULL,
        [LastSyncedAtUtc]           DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_AuthorProfileProjection_LastSyncedAtUtc]
            DEFAULT (SYSUTCDATETIME()),

        [CreatedAtUtc]              DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_AuthorProfileProjection_CreatedAtUtc]
            DEFAULT (SYSUTCDATETIME()),

        [UpdatedAtUtc]              DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_AuthorProfileProjection_UpdatedAtUtc]
            DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [PK_AuthorProfileProjection]
            PRIMARY KEY CLUSTERED ([AuthorUserId] ASC),

        CONSTRAINT [UQ_AuthorProfileProjection_AuthorUserPublicId]
            UNIQUE ([AuthorUserPublicId]),

        CONSTRAINT [CK_AuthorProfileProjection_AuthorUserId_Positive]
            CHECK ([AuthorUserId] > 0),

        CONSTRAINT [CK_AuthorProfileProjection_AuthorUserPublicId_Length]
            CHECK (LEN([AuthorUserPublicId]) = 26),

        CONSTRAINT [CK_AuthorProfileProjection_SourceVersion_Positive]
            CHECK ([SourceVersion] > 0),

        CONSTRAINT [CK_AuthorProfileProjection_LastEventMessageId_Length]
            CHECK (LEN([LastEventMessageId]) = 26)
    );

    PRINT N'Created table: [reading].[AuthorProfileProjection]';
END
ELSE
BEGIN
    PRINT N'Table exists: [reading].[AuthorProfileProjection]';
END
GO

/*
  Projection timestamps can be assigned from a captured @Now value before
  CreatedAtUtc is evaluated by its DEFAULT constraint during INSERT.
  Therefore these timestamp comparison constraints are not safe.
*/
IF OBJECT_ID(N'[reading].[CK_AuthorProfileProjection_LastSyncedAtUtc]', N'C') IS NOT NULL
BEGIN
    ALTER TABLE [reading].[AuthorProfileProjection]
        DROP CONSTRAINT [CK_AuthorProfileProjection_LastSyncedAtUtc];

    PRINT N'Dropped obsolete constraint: [CK_AuthorProfileProjection_LastSyncedAtUtc]';
END
GO

IF OBJECT_ID(N'[reading].[CK_AuthorProfileProjection_UpdatedAtUtc]', N'C') IS NOT NULL
BEGIN
    ALTER TABLE [reading].[AuthorProfileProjection]
        DROP CONSTRAINT [CK_AuthorProfileProjection_UpdatedAtUtc];

    PRINT N'Dropped obsolete constraint: [CK_AuthorProfileProjection_UpdatedAtUtc]';
END
GO

/* =========================================================
   AuthorProfileProjection - existing DB compatibility
   ========================================================= */
IF COL_LENGTH(N'reading.AuthorProfileProjection', N'LastEventMessageId') IS NULL
   AND COL_LENGTH(N'reading.AuthorProfileProjection', N'LastAppliedMessageId') IS NOT NULL
BEGIN
    IF OBJECT_ID(N'[reading].[CK_AuthorProfileProjection_LastAppliedMessageId_Length]', N'C') IS NOT NULL
    BEGIN
        ALTER TABLE [reading].[AuthorProfileProjection]
            DROP CONSTRAINT [CK_AuthorProfileProjection_LastAppliedMessageId_Length];
    END

    EXEC sp_rename
        N'reading.AuthorProfileProjection.LastAppliedMessageId',
        N'LastEventMessageId',
        N'COLUMN';

    PRINT N'Renamed column: [reading].[AuthorProfileProjection].[LastAppliedMessageId] -> [LastEventMessageId]';
END
GO

IF COL_LENGTH(N'reading.AuthorProfileProjection', N'LastEventMessageId') IS NOT NULL
   AND OBJECT_ID(N'[reading].[CK_AuthorProfileProjection_LastEventMessageId_Length]', N'C') IS NULL
BEGIN
    IF OBJECT_ID(N'[reading].[CK_AuthorProfileProjection_LastAppliedMessageId_Length]', N'C') IS NOT NULL
    BEGIN
        ALTER TABLE [reading].[AuthorProfileProjection]
            DROP CONSTRAINT [CK_AuthorProfileProjection_LastAppliedMessageId_Length];
    END

    ALTER TABLE [reading].[AuthorProfileProjection]
        ADD CONSTRAINT [CK_AuthorProfileProjection_LastEventMessageId_Length]
            CHECK (LEN([LastEventMessageId]) = 26);

    PRINT N'Created constraint: [CK_AuthorProfileProjection_LastEventMessageId_Length]';
END
GO

IF EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[reading].[AuthorProfileProjection]')
      AND name = N'SourceVersion'
      AND system_type_id = TYPE_ID(N'int')
)
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_AuthorProfileProjection_LastSyncedAtUtc'
          AND object_id = OBJECT_ID(N'[reading].[AuthorProfileProjection]')
    )
    BEGIN
        DROP INDEX [IX_AuthorProfileProjection_LastSyncedAtUtc]
            ON [reading].[AuthorProfileProjection];

        PRINT N'Dropped index before SourceVersion type migration: [IX_AuthorProfileProjection_LastSyncedAtUtc]';
    END

    IF OBJECT_ID(N'[reading].[CK_AuthorProfileProjection_SourceVersion_Positive]', N'C') IS NOT NULL
    BEGIN
        ALTER TABLE [reading].[AuthorProfileProjection]
            DROP CONSTRAINT [CK_AuthorProfileProjection_SourceVersion_Positive];

        PRINT N'Dropped constraint before SourceVersion type migration: [CK_AuthorProfileProjection_SourceVersion_Positive]';
    END

    ALTER TABLE [reading].[AuthorProfileProjection]
        ALTER COLUMN [SourceVersion] BIGINT NOT NULL;

    PRINT N'Altered column: [reading].[AuthorProfileProjection].[SourceVersion] BIGINT';

    ALTER TABLE [reading].[AuthorProfileProjection]
        ADD CONSTRAINT [CK_AuthorProfileProjection_SourceVersion_Positive]
            CHECK ([SourceVersion] > 0);

    PRINT N'Recreated constraint: [CK_AuthorProfileProjection_SourceVersion_Positive]';
END
GO
