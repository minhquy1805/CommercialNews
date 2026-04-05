/*
  File: db/10_modules/seo/010_indexes.sql
  Module: SEO
  Purpose:
  - Create supporting indexes for SEO truth tables in CommercialNews V1.
  - Optimize:
      * public slug resolve hot path
      * admin SEO get/upsert by article
      * metadata lookup by article
      * active route maintenance / deactivation / switch
      * conflict-safe slug ownership
  - Idempotent: safe to re-run.

  Notes:
  - Truth remains in [seo] tables.
  - Redis/cache/search/projections are outside this file.
  - Keep indexes focused on known V1 access patterns; avoid speculative over-indexing.
  - Visibility is still enforced by Content truth after SEO route resolution.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 57101, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

USE [CommercialNews];
GO

IF SCHEMA_ID(N'seo') IS NULL
BEGIN
    THROW 57102, 'Schema [seo] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

IF OBJECT_ID(N'[seo].[SeoMetadata]', N'U') IS NULL
BEGIN
    THROW 57103, 'Table [seo].[SeoMetadata] does not exist. Run seo/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[seo].[SlugRegistry]', N'U') IS NULL
BEGIN
    THROW 57104, 'Table [seo].[SlugRegistry] does not exist. Run seo/001_tables.sql first.', 1;
END
GO

/* =========================================================
   1) [seo].[SeoMetadata]
   ========================================================= */

/* Admin/article SEO lookup by article */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SeoMetadata_ArticleId'
      AND object_id = OBJECT_ID(N'[seo].[SeoMetadata]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SeoMetadata_ArticleId]
    ON [seo].[SeoMetadata]
    (
        [ArticleId] ASC
    )
    INCLUDE
    (
        [SeoId],
        [CanonicalUrl],
        [MetaTitle],
        [MetaDescription],
        [OgTitle],
        [OgDescription],
        [OgImageUrl],
        [TwitterTitle],
        [TwitterDescription],
        [TwitterImageUrl],
        [Version],
        [UpdatedAt],
        [UpdatedByUserId]
    );

    PRINT N'Created index: [IX_SeoMetadata_ArticleId]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_SeoMetadata_ArticleId]';
END
GO

/* Admin metadata investigation / recent updates */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SeoMetadata_UpdatedAt_SeoId'
      AND object_id = OBJECT_ID(N'[seo].[SeoMetadata]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SeoMetadata_UpdatedAt_SeoId]
    ON [seo].[SeoMetadata]
    (
        [UpdatedAt] DESC,
        [SeoId] DESC
    )
    INCLUDE
    (
        [ArticleId],
        [Version],
        [UpdatedByUserId],
        [CanonicalUrl],
        [MetaTitle],
        [MetaDescription]
    );

    PRINT N'Created index: [IX_SeoMetadata_UpdatedAt_SeoId]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_SeoMetadata_UpdatedAt_SeoId]';
END
GO

/* Actor-based SEO admin investigation */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SeoMetadata_UpdatedByUserId_UpdatedAt'
      AND object_id = OBJECT_ID(N'[seo].[SeoMetadata]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SeoMetadata_UpdatedByUserId_UpdatedAt]
    ON [seo].[SeoMetadata]
    (
        [UpdatedByUserId] ASC,
        [UpdatedAt] DESC,
        [SeoId] DESC
    )
    INCLUDE
    (
        [ArticleId],
        [Version],
        [CanonicalUrl],
        [MetaTitle],
        [MetaDescription]
    );

    PRINT N'Created index: [IX_SeoMetadata_UpdatedByUserId_UpdatedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_SeoMetadata_UpdatedByUserId_UpdatedAt]';
END
GO

/* =========================================================
   2) [seo].[SlugRegistry]
   ========================================================= */

/* Public hot path: resolve active public route by scope + slug */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SlugRegistry_ResolveActive_Scope_Slug'
      AND object_id = OBJECT_ID(N'[seo].[SlugRegistry]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SlugRegistry_ResolveActive_Scope_Slug]
    ON [seo].[SlugRegistry]
    (
        [Scope] ASC,
        [Slug] ASC
    )
    INCLUDE
    (
        [SlugId],
        [ArticleId],
        [CanonicalUrl],
        [IsIndexable],
        [IsActive],
        [Version],
        [UpdatedAt]
    )
    WHERE [IsActive] = 1;

    PRINT N'Created index: [IX_SlugRegistry_ResolveActive_Scope_Slug]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_SlugRegistry_ResolveActive_Scope_Slug]';
END
GO

/* Admin/article SEO route lookup and active route maintenance */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SlugRegistry_ArticleId_Scope_IsActive_UpdatedAt'
      AND object_id = OBJECT_ID(N'[seo].[SlugRegistry]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SlugRegistry_ArticleId_Scope_IsActive_UpdatedAt]
    ON [seo].[SlugRegistry]
    (
        [ArticleId] ASC,
        [Scope] ASC,
        [IsActive] ASC,
        [UpdatedAt] DESC,
        [SlugId] DESC
    )
    INCLUDE
    (
        [Slug],
        [CanonicalUrl],
        [IsIndexable],
        [Version],
        [CreatedAt],
        [CreatedByUserId],
        [UpdatedByUserId]
    );

    PRINT N'Created index: [IX_SlugRegistry_ArticleId_Scope_IsActive_UpdatedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_SlugRegistry_ArticleId_Scope_IsActive_UpdatedAt]';
END
GO

/* V1 policy: one active slug per article per scope */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_SlugRegistry_ArticleId_Scope_Active'
      AND object_id = OBJECT_ID(N'[seo].[SlugRegistry]')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UX_SlugRegistry_ArticleId_Scope_Active]
    ON [seo].[SlugRegistry]
    (
        [ArticleId] ASC,
        [Scope] ASC
    )
    WHERE [IsActive] = 1;

    PRINT N'Created index: [UX_SlugRegistry_ArticleId_Scope_Active]';
END
ELSE
BEGIN
    PRINT N'Index exists: [UX_SlugRegistry_ArticleId_Scope_Active]';
END
GO

/* Active route listing / routing-state admin investigation */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SlugRegistry_IsActive_IsIndexable_UpdatedAt'
      AND object_id = OBJECT_ID(N'[seo].[SlugRegistry]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SlugRegistry_IsActive_IsIndexable_UpdatedAt]
    ON [seo].[SlugRegistry]
    (
        [IsActive] ASC,
        [IsIndexable] ASC,
        [UpdatedAt] DESC,
        [SlugId] DESC
    )
    INCLUDE
    (
        [ArticleId],
        [Scope],
        [Slug],
        [CanonicalUrl],
        [Version]
    );

    PRINT N'Created index: [IX_SlugRegistry_IsActive_IsIndexable_UpdatedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_SlugRegistry_IsActive_IsIndexable_UpdatedAt]';
END
GO

/* Actor-based route change investigation */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SlugRegistry_UpdatedByUserId_UpdatedAt'
      AND object_id = OBJECT_ID(N'[seo].[SlugRegistry]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SlugRegistry_UpdatedByUserId_UpdatedAt]
    ON [seo].[SlugRegistry]
    (
        [UpdatedByUserId] ASC,
        [UpdatedAt] DESC,
        [SlugId] DESC
    )
    INCLUDE
    (
        [ArticleId],
        [Scope],
        [Slug],
        [IsActive],
        [IsIndexable],
        [Version]
    );

    PRINT N'Created index: [IX_SlugRegistry_UpdatedByUserId_UpdatedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_SlugRegistry_UpdatedByUserId_UpdatedAt]';
END
GO

/* Created-by investigation / audit-ish maintenance */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SlugRegistry_CreatedByUserId_CreatedAt'
      AND object_id = OBJECT_ID(N'[seo].[SlugRegistry]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SlugRegistry_CreatedByUserId_CreatedAt]
    ON [seo].[SlugRegistry]
    (
        [CreatedByUserId] ASC,
        [CreatedAt] DESC,
        [SlugId] DESC
    )
    INCLUDE
    (
        [ArticleId],
        [Scope],
        [Slug],
        [IsActive],
        [Version]
    );

    PRINT N'Created index: [IX_SlugRegistry_CreatedByUserId_CreatedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_SlugRegistry_CreatedByUserId_CreatedAt]';
END
GO