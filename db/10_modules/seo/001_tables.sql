/*
  File: db/10_modules/seo/001_tables.sql
  Module: SEO
  Purpose:
  - Create SEO truth tables for CommercialNews V1.
  - Core OLTP truth model:
      * SlugRegistry
      * SeoMetadata
  - Support:
      * fast slug routing truth
      * optional SEO metadata truth
      * deterministic slug conflict handling
      * active/inactive route policy
      * optimistic concurrency via Version
      * future outbox / async invalidation / rebuild readiness
  - Idempotent: safe to re-run.

  Notes:
  - SEO owns routing truth and SEO metadata truth.
  - SEO does NOT own publication visibility truth.
  - Reading/Public Query must still validate Content truth before serving.
  - Redis/cache/search/projections are NOT modeled here in 001_tables.sql.
  - Index-heavy tuning belongs in 010_indexes.sql.
  - Stored procedures belong in 020_procs.sql.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 57001, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

USE [CommercialNews];
GO

IF SCHEMA_ID(N'seo') IS NULL
BEGIN
    THROW 57002, 'Schema [seo] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

IF SCHEMA_ID(N'content') IS NULL
BEGIN
    THROW 57003, 'Schema [content] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

IF SCHEMA_ID(N'identity') IS NULL
BEGIN
    THROW 57004, 'Schema [identity] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

IF OBJECT_ID(N'[content].[Article]', N'U') IS NULL
BEGIN
    THROW 57005, 'Table [content].[Article] does not exist. Run content/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[identity].[UserAccount]', N'U') IS NULL
BEGIN
    THROW 57006, 'Table [identity].[UserAccount] does not exist. Run identity/001_tables.sql first.', 1;
END
GO

/* =========================================================
   1) [seo].[SeoMetadata]
   ========================================================= */
IF OBJECT_ID(N'[seo].[SeoMetadata]', N'U') IS NULL
BEGIN
    CREATE TABLE [seo].[SeoMetadata]
    (
        [SeoId]                 BIGINT IDENTITY(1,1) NOT NULL,

        [ArticleId]             BIGINT               NOT NULL,

        [CanonicalUrl]          NVARCHAR(500)        NULL,
        [MetaTitle]             NVARCHAR(300)        NULL,
        [MetaDescription]       NVARCHAR(500)        NULL,

        [OgTitle]               NVARCHAR(300)        NULL,
        [OgDescription]         NVARCHAR(500)        NULL,
        [OgImageUrl]            NVARCHAR(800)        NULL,

        [TwitterTitle]          NVARCHAR(300)        NULL,
        [TwitterDescription]    NVARCHAR(500)        NULL,
        [TwitterImageUrl]       NVARCHAR(800)        NULL,

        [Version]               INT                  NOT NULL
            CONSTRAINT [DF_SeoMetadata_Version] DEFAULT (1),

        [UpdatedAt]             DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_SeoMetadata_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedByUserId]       BIGINT               NULL,

        CONSTRAINT [PK_SeoMetadata]
            PRIMARY KEY CLUSTERED ([SeoId] ASC),

        CONSTRAINT [UQ_SeoMetadata_ArticleId]
            UNIQUE ([ArticleId]),

        CONSTRAINT [FK_SeoMetadata_Article]
            FOREIGN KEY ([ArticleId])
            REFERENCES [content].[Article]([ArticleId]),

        CONSTRAINT [FK_SeoMetadata_UpdatedByUser]
            FOREIGN KEY ([UpdatedByUserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [CK_SeoMetadata_CanonicalUrl_NotBlank]
            CHECK ([CanonicalUrl] IS NULL OR LEN(LTRIM(RTRIM([CanonicalUrl]))) > 0),

        CONSTRAINT [CK_SeoMetadata_MetaTitle_NotBlank]
            CHECK ([MetaTitle] IS NULL OR LEN(LTRIM(RTRIM([MetaTitle]))) > 0),

        CONSTRAINT [CK_SeoMetadata_MetaDescription_NotBlank]
            CHECK ([MetaDescription] IS NULL OR LEN(LTRIM(RTRIM([MetaDescription]))) > 0),

        CONSTRAINT [CK_SeoMetadata_OgTitle_NotBlank]
            CHECK ([OgTitle] IS NULL OR LEN(LTRIM(RTRIM([OgTitle]))) > 0),

        CONSTRAINT [CK_SeoMetadata_OgDescription_NotBlank]
            CHECK ([OgDescription] IS NULL OR LEN(LTRIM(RTRIM([OgDescription]))) > 0),

        CONSTRAINT [CK_SeoMetadata_OgImageUrl_NotBlank]
            CHECK ([OgImageUrl] IS NULL OR LEN(LTRIM(RTRIM([OgImageUrl]))) > 0),

        CONSTRAINT [CK_SeoMetadata_TwitterTitle_NotBlank]
            CHECK ([TwitterTitle] IS NULL OR LEN(LTRIM(RTRIM([TwitterTitle]))) > 0),

        CONSTRAINT [CK_SeoMetadata_TwitterDescription_NotBlank]
            CHECK ([TwitterDescription] IS NULL OR LEN(LTRIM(RTRIM([TwitterDescription]))) > 0),

        CONSTRAINT [CK_SeoMetadata_TwitterImageUrl_NotBlank]
            CHECK ([TwitterImageUrl] IS NULL OR LEN(LTRIM(RTRIM([TwitterImageUrl]))) > 0),

        CONSTRAINT [CK_SeoMetadata_Version_Positive]
            CHECK ([Version] > 0)
    );

    PRINT N'Created table: [seo].[SeoMetadata]';
END
ELSE
BEGIN
    PRINT N'Table exists: [seo].[SeoMetadata]';
END
GO

/* =========================================================
   2) [seo].[SlugRegistry]
   ========================================================= */
IF OBJECT_ID(N'[seo].[SlugRegistry]', N'U') IS NULL
BEGIN
    CREATE TABLE [seo].[SlugRegistry]
    (
        [SlugId]                BIGINT IDENTITY(1,1) NOT NULL,

        [ArticleId]             BIGINT               NOT NULL,

        [Slug]                  NVARCHAR(200)        NOT NULL,
        [Scope]                 VARCHAR(30)          NOT NULL
            CONSTRAINT [DF_SlugRegistry_Scope] DEFAULT ('public'),

        [CanonicalUrl]          NVARCHAR(500)        NULL,

        [IsIndexable]           BIT                  NOT NULL
            CONSTRAINT [DF_SlugRegistry_IsIndexable] DEFAULT (0),

        [IsActive]              BIT                  NOT NULL
            CONSTRAINT [DF_SlugRegistry_IsActive] DEFAULT (1),

        [Version]               INT                  NOT NULL
            CONSTRAINT [DF_SlugRegistry_Version] DEFAULT (1),

        [CreatedAt]             DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_SlugRegistry_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [CreatedByUserId]       BIGINT               NULL,

        [UpdatedAt]             DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_SlugRegistry_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedByUserId]       BIGINT               NULL,

        CONSTRAINT [PK_SlugRegistry]
            PRIMARY KEY CLUSTERED ([SlugId] ASC),

        CONSTRAINT [UQ_SlugRegistry_Scope_Slug]
            UNIQUE ([Scope], [Slug]),

        CONSTRAINT [FK_SlugRegistry_Article]
            FOREIGN KEY ([ArticleId])
            REFERENCES [content].[Article]([ArticleId]),

        CONSTRAINT [FK_SlugRegistry_CreatedByUser]
            FOREIGN KEY ([CreatedByUserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [FK_SlugRegistry_UpdatedByUser]
            FOREIGN KEY ([UpdatedByUserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [CK_SlugRegistry_Slug_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([Slug]))) > 0),

        CONSTRAINT [CK_SlugRegistry_Scope]
            CHECK ([Scope] IN ('public')),

        CONSTRAINT [CK_SlugRegistry_CanonicalUrl_NotBlank]
            CHECK ([CanonicalUrl] IS NULL OR LEN(LTRIM(RTRIM([CanonicalUrl]))) > 0),

        CONSTRAINT [CK_SlugRegistry_UpdatedAt]
            CHECK ([UpdatedAt] >= [CreatedAt]),

        CONSTRAINT [CK_SlugRegistry_Version_Positive]
            CHECK ([Version] > 0)
    );

    PRINT N'Created table: [seo].[SlugRegistry]';
END
ELSE
BEGIN
    PRINT N'Table exists: [seo].[SlugRegistry]';
END
GO