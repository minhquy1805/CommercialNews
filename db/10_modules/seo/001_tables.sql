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
      * SEO metadata truth
      * deterministic slug conflict handling
      * active/inactive route policy
      * optimistic concurrency via Version
      * Content-event apply markers without a dedicated consumed-message table
      * future outbox / async invalidation / rebuild readiness
  - Idempotent: safe to re-run.

  Notes:
  - SEO owns routing truth and SEO metadata truth.
  - SEO does NOT own publication visibility truth.
  - Reading/Public Query must still validate Content truth before serving.
  - Redis/cache/search/projections are NOT modeled here in 001_tables.sql.
  - Index-heavy tuning belongs in 010_indexes.sql.
  - Stored procedures belong in 020_procs.sql.
  - SEO V1 does not create a dedicated SeoConsumedMessage table.
    Truth-affecting consumer idempotency is handled through:
      * SourceAggregateVersion
      * LastAppliedMessageId
      * LastSyncedAtUtc
      * unique constraints / indexes
      * idempotent upsert procedures
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

IF SCHEMA_ID(N'identity') IS NULL
BEGIN
    THROW 57003, 'Schema [identity] does not exist. Run identity/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[identity].[UserAccount]', N'U') IS NULL
BEGIN
    THROW 57004, 'Table [identity].[UserAccount] does not exist. Run identity/001_tables.sql first.', 1;
END
GO

/* =========================================================
   1) [seo].[SeoMetadata]
   ========================================================= */
IF OBJECT_ID(N'[seo].[SeoMetadata]', N'U') IS NULL
BEGIN
    CREATE TABLE [seo].[SeoMetadata]
    (
        [SeoId]                  BIGINT IDENTITY(1,1) NOT NULL,

        [Scope]                  VARCHAR(30)          NOT NULL
            CONSTRAINT [DF_SeoMetadata_Scope] DEFAULT ('public'),

        [ResourceType]           VARCHAR(50)          NOT NULL,
        [ResourcePublicId]       CHAR(26)             NOT NULL,

        [Slug]                   NVARCHAR(200)        NULL,
        [CanonicalUrl]           NVARCHAR(500)        NULL,

        [MetaTitle]              NVARCHAR(300)        NULL,
        [MetaDescription]        NVARCHAR(500)        NULL,

        [OgTitle]                NVARCHAR(300)        NULL,
        [OgDescription]          NVARCHAR(500)        NULL,
        [OgImageUrl]             NVARCHAR(800)        NULL,

        [TwitterTitle]           NVARCHAR(300)        NULL,
        [TwitterDescription]     NVARCHAR(500)        NULL,
        [TwitterImageUrl]        NVARCHAR(800)        NULL,

        [Robots]                 NVARCHAR(100)        NULL,

        [IsManualOverride]       BIT                  NOT NULL
            CONSTRAINT [DF_SeoMetadata_IsManualOverride] DEFAULT (0),

        /*
          Content-event apply markers.
          Used for idempotent, version-aware SEO sync without a dedicated consumed-message table.
        */
        [SourceAggregateVersion] BIGINT               NULL,
        [LastAppliedMessageId]   CHAR(26)             NULL,
        [LastSyncedAtUtc]        DATETIME2(3)         NULL,

        /*
          SEO-owned optimistic concurrency version.
          This is not necessarily the same as SourceAggregateVersion.
        */
        [Version]                INT                  NOT NULL
            CONSTRAINT [DF_SeoMetadata_Version] DEFAULT (1),

        [CreatedAtUtc]           DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_SeoMetadata_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),

        [UpdatedAtUtc]           DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_SeoMetadata_UpdatedAtUtc] DEFAULT (SYSUTCDATETIME()),

        [UpdatedByUserId]        BIGINT               NULL,

        CONSTRAINT [PK_SeoMetadata]
            PRIMARY KEY CLUSTERED ([SeoId] ASC),

        CONSTRAINT [UQ_SeoMetadata_Scope_Resource]
            UNIQUE ([Scope], [ResourceType], [ResourcePublicId]),

        CONSTRAINT [FK_SeoMetadata_UpdatedByUser]
            FOREIGN KEY ([UpdatedByUserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [CK_SeoMetadata_Scope]
            CHECK ([Scope] IN ('public')),

        CONSTRAINT [CK_SeoMetadata_ResourceType]
            CHECK ([ResourceType] IN ('Article')),

        CONSTRAINT [CK_SeoMetadata_ResourcePublicId_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([ResourcePublicId]))) > 0),

        CONSTRAINT [CK_SeoMetadata_Slug_NotBlank]
            CHECK ([Slug] IS NULL OR LEN(LTRIM(RTRIM([Slug]))) > 0),

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

        CONSTRAINT [CK_SeoMetadata_Robots_NotBlank]
            CHECK ([Robots] IS NULL OR LEN(LTRIM(RTRIM([Robots]))) > 0),

        CONSTRAINT [CK_SeoMetadata_SourceAggregateVersion_Positive]
            CHECK ([SourceAggregateVersion] IS NULL OR [SourceAggregateVersion] > 0),

        CONSTRAINT [CK_SeoMetadata_LastAppliedMessageId_NotBlank]
            CHECK ([LastAppliedMessageId] IS NULL OR LEN(LTRIM(RTRIM([LastAppliedMessageId]))) > 0),

        CONSTRAINT [CK_SeoMetadata_Version_Positive]
            CHECK ([Version] > 0),

        CONSTRAINT [CK_SeoMetadata_UpdatedAtUtc]
            CHECK ([UpdatedAtUtc] >= [CreatedAtUtc])
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
        [SlugId]                 BIGINT IDENTITY(1,1) NOT NULL,

        [Scope]                  VARCHAR(30)          NOT NULL
            CONSTRAINT [DF_SlugRegistry_Scope] DEFAULT ('public'),

        [Slug]                   NVARCHAR(200)        NOT NULL,

        [ResourceType]           VARCHAR(50)          NOT NULL,
        [ResourcePublicId]       CHAR(26)             NOT NULL,

        [CanonicalUrl]           NVARCHAR(500)        NULL,

        [IsIndexable]            BIT                  NOT NULL
            CONSTRAINT [DF_SlugRegistry_IsIndexable] DEFAULT (0),

        [IsActive]               BIT                  NOT NULL
            CONSTRAINT [DF_SlugRegistry_IsActive] DEFAULT (1),

        /*
          Content-event apply markers.
          Used for idempotent, version-aware SEO sync without a dedicated consumed-message table.
        */
        [SourceAggregateVersion] BIGINT               NULL,
        [LastAppliedMessageId]   CHAR(26)             NULL,
        [LastSyncedAtUtc]        DATETIME2(3)         NULL,

        /*
          SEO-owned optimistic concurrency version.
          This is not necessarily the same as SourceAggregateVersion.
        */
        [Version]                INT                  NOT NULL
            CONSTRAINT [DF_SlugRegistry_Version] DEFAULT (1),

        [CreatedAtUtc]           DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_SlugRegistry_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),

        [CreatedByUserId]        BIGINT               NULL,

        [UpdatedAtUtc]           DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_SlugRegistry_UpdatedAtUtc] DEFAULT (SYSUTCDATETIME()),

        [UpdatedByUserId]        BIGINT               NULL,

        CONSTRAINT [PK_SlugRegistry]
            PRIMARY KEY CLUSTERED ([SlugId] ASC),

        /*
          One current route per resource in a scope.
          Slug uniqueness itself should be enforced in 010_indexes.sql,
          preferably with a filtered unique index on active routes.
        */
        CONSTRAINT [UQ_SlugRegistry_Scope_Resource]
            UNIQUE ([Scope], [ResourceType], [ResourcePublicId]),

        CONSTRAINT [FK_SlugRegistry_CreatedByUser]
            FOREIGN KEY ([CreatedByUserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [FK_SlugRegistry_UpdatedByUser]
            FOREIGN KEY ([UpdatedByUserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [CK_SlugRegistry_Scope]
            CHECK ([Scope] IN ('public')),

        CONSTRAINT [CK_SlugRegistry_ResourceType]
            CHECK ([ResourceType] IN ('Article')),

        CONSTRAINT [CK_SlugRegistry_ResourcePublicId_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([ResourcePublicId]))) > 0),

        CONSTRAINT [CK_SlugRegistry_Slug_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([Slug]))) > 0),

        CONSTRAINT [CK_SlugRegistry_CanonicalUrl_NotBlank]
            CHECK ([CanonicalUrl] IS NULL OR LEN(LTRIM(RTRIM([CanonicalUrl]))) > 0),

        CONSTRAINT [CK_SlugRegistry_SourceAggregateVersion_Positive]
            CHECK ([SourceAggregateVersion] IS NULL OR [SourceAggregateVersion] > 0),

        CONSTRAINT [CK_SlugRegistry_LastAppliedMessageId_NotBlank]
            CHECK ([LastAppliedMessageId] IS NULL OR LEN(LTRIM(RTRIM([LastAppliedMessageId]))) > 0),

        CONSTRAINT [CK_SlugRegistry_Version_Positive]
            CHECK ([Version] > 0),

        CONSTRAINT [CK_SlugRegistry_UpdatedAtUtc]
            CHECK ([UpdatedAtUtc] >= [CreatedAtUtc])
    );

    PRINT N'Created table: [seo].[SlugRegistry]';
END
ELSE
BEGIN
    PRINT N'Table exists: [seo].[SlugRegistry]';
END
GO