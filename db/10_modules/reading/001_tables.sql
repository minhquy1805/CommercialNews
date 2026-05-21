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

        [Slug]                      NVARCHAR(300)        NULL,

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

        [CanonicalUrl]              NVARCHAR(1000)       NULL,
        [MetaTitle]                 NVARCHAR(300)        NULL,
        [MetaDescription]           NVARCHAR(500)        NULL,

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

        [Url]                       NVARCHAR(1000)       NOT NULL,
        [Alt]                       NVARCHAR(300)        NULL,
        [MediaType]                 NVARCHAR(50)         NOT NULL,

        [SortOrder]                 INT                  NOT NULL
            CONSTRAINT [DF_ArticleReadModelMedia_SortOrder] DEFAULT (0),

        [IsPrimary]                 BIT                  NOT NULL
            CONSTRAINT [DF_ArticleReadModelMedia_IsPrimary] DEFAULT (0),

        [SourceVersion]             BIGINT               NOT NULL
            CONSTRAINT [DF_ArticleReadModelMedia_SourceVersion] DEFAULT (0),

        [LastSyncedAtUtc]           DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_ArticleReadModelMedia_LastSyncedAtUtc] DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [PK_ArticleReadModelMedia]
            PRIMARY KEY CLUSTERED ([ArticleId] ASC, [MediaId] ASC),

        CONSTRAINT [FK_ArticleReadModelMedia_ArticleReadModel]
            FOREIGN KEY ([ArticleId])
            REFERENCES [reading].[ArticleReadModel]([ArticleId])
            ON DELETE CASCADE,

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
