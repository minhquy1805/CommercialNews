/*
  File: db/10_modules/seo/010_indexes.sql
  Module: SEO
  Purpose:
  - Create supporting indexes for SEO truth tables in CommercialNews V1.
  - Optimize:
      * public slug resolve hot path
      * admin SEO get/upsert by resource public identity
      * metadata lookup by resource
      * active route maintenance / deactivation / switch
      * conflict-safe slug ownership
      * async Content-event apply checks
      * admin investigation / recent updates
  - Idempotent: safe to re-run.

  Notes:
  - Truth remains in [seo] tables.
  - Redis/cache/search/projections are outside this file.
  - Visibility is still enforced by Content truth after SEO route resolution.
  - SEO V1 uses ResourceType + ResourcePublicId, not internal Content ArticleId.
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

/*
  Admin/resource SEO metadata lookup.
  UQ_SeoMetadata_Scope_Resource already exists from 001_tables.sql:
  ([Scope], [ResourceType], [ResourcePublicId])

  This covering index is useful for read-heavy admin/API responses.
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SeoMetadata_Scope_ResourcePublicId_Covering'
      AND object_id = OBJECT_ID(N'[seo].[SeoMetadata]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SeoMetadata_Scope_ResourcePublicId_Covering]
    ON [seo].[SeoMetadata]
    (
        [Scope] ASC,
        [ResourceType] ASC,
        [ResourcePublicId] ASC
    )
    INCLUDE
    (
        [SeoId],
        [Slug],
        [CanonicalUrl],
        [MetaTitle],
        [MetaDescription],
        [OgTitle],
        [OgDescription],
        [OgImageUrl],
        [TwitterTitle],
        [TwitterDescription],
        [TwitterImageUrl],
        [Robots],
        [IsManualOverride],
        [SourceAggregateVersion],
        [LastAppliedMessageId],
        [LastSyncedAtUtc],
        [Version],
        [UpdatedAtUtc],
        [UpdatedByUserId]
    );

    PRINT N'Created index: [IX_SeoMetadata_Scope_ResourcePublicId_Covering]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_SeoMetadata_Scope_ResourcePublicId_Covering]';
END
GO

/* Admin metadata investigation / recent updates */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SeoMetadata_UpdatedAtUtc_SeoId'
      AND object_id = OBJECT_ID(N'[seo].[SeoMetadata]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SeoMetadata_UpdatedAtUtc_SeoId]
    ON [seo].[SeoMetadata]
    (
        [UpdatedAtUtc] DESC,
        [SeoId] DESC
    )
    INCLUDE
    (
        [Scope],
        [ResourceType],
        [ResourcePublicId],
        [Slug],
        [Version],
        [IsManualOverride],
        [UpdatedByUserId],
        [CanonicalUrl],
        [MetaTitle],
        [MetaDescription]
    );

    PRINT N'Created index: [IX_SeoMetadata_UpdatedAtUtc_SeoId]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_SeoMetadata_UpdatedAtUtc_SeoId]';
END
GO

/* Actor-based SEO admin investigation */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SeoMetadata_UpdatedByUserId_UpdatedAtUtc'
      AND object_id = OBJECT_ID(N'[seo].[SeoMetadata]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SeoMetadata_UpdatedByUserId_UpdatedAtUtc]
    ON [seo].[SeoMetadata]
    (
        [UpdatedByUserId] ASC,
        [UpdatedAtUtc] DESC,
        [SeoId] DESC
    )
    INCLUDE
    (
        [Scope],
        [ResourceType],
        [ResourcePublicId],
        [Slug],
        [Version],
        [IsManualOverride],
        [CanonicalUrl],
        [MetaTitle],
        [MetaDescription]
    );

    PRINT N'Created index: [IX_SeoMetadata_UpdatedByUserId_UpdatedAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_SeoMetadata_UpdatedByUserId_UpdatedAtUtc]';
END
GO

/* Manual override investigation / Content auto-sync skip analysis */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SeoMetadata_IsManualOverride_UpdatedAtUtc'
      AND object_id = OBJECT_ID(N'[seo].[SeoMetadata]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SeoMetadata_IsManualOverride_UpdatedAtUtc]
    ON [seo].[SeoMetadata]
    (
        [IsManualOverride] ASC,
        [UpdatedAtUtc] DESC,
        [SeoId] DESC
    )
    INCLUDE
    (
        [Scope],
        [ResourceType],
        [ResourcePublicId],
        [Slug],
        [Version],
        [SourceAggregateVersion],
        [LastSyncedAtUtc],
        [MetaTitle],
        [MetaDescription]
    );

    PRINT N'Created index: [IX_SeoMetadata_IsManualOverride_UpdatedAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_SeoMetadata_IsManualOverride_UpdatedAtUtc]';
END
GO

/* Async apply / sync lag investigation */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SeoMetadata_SourceAggregateVersion_LastSyncedAtUtc'
      AND object_id = OBJECT_ID(N'[seo].[SeoMetadata]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SeoMetadata_SourceAggregateVersion_LastSyncedAtUtc]
    ON [seo].[SeoMetadata]
    (
        [SourceAggregateVersion] DESC,
        [LastSyncedAtUtc] DESC,
        [SeoId] DESC
    )
    INCLUDE
    (
        [Scope],
        [ResourceType],
        [ResourcePublicId],
        [LastAppliedMessageId],
        [Version],
        [IsManualOverride]
    );

    PRINT N'Created index: [IX_SeoMetadata_SourceAggregateVersion_LastSyncedAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_SeoMetadata_SourceAggregateVersion_LastSyncedAtUtc]';
END
GO

/* =========================================================
   2) [seo].[SlugRegistry]
   ========================================================= */

/*
  V1 policy: active slugs must be unique by Scope + Slug.
  This is the critical conflict-safety index for public route ownership.
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_SlugRegistry_Scope_Slug_Active'
      AND object_id = OBJECT_ID(N'[seo].[SlugRegistry]')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UX_SlugRegistry_Scope_Slug_Active]
    ON [seo].[SlugRegistry]
    (
        [Scope] ASC,
        [Slug] ASC
    )
    WHERE [IsActive] = 1;

    PRINT N'Created index: [UX_SlugRegistry_Scope_Slug_Active]';
END
ELSE
BEGIN
    PRINT N'Index exists: [UX_SlugRegistry_Scope_Slug_Active]';
END
GO

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
        [ResourceType],
        [ResourcePublicId],
        [CanonicalUrl],
        [IsIndexable],
        [IsActive],
        [SourceAggregateVersion],
        [LastAppliedMessageId],
        [LastSyncedAtUtc],
        [Version],
        [UpdatedAtUtc]
    )
    WHERE [IsActive] = 1;

    PRINT N'Created index: [IX_SlugRegistry_ResolveActive_Scope_Slug]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_SlugRegistry_ResolveActive_Scope_Slug]';
END
GO

/*
  Admin/resource SEO route lookup and active route maintenance.
  UQ_SlugRegistry_Scope_Resource already exists from 001_tables.sql:
  ([Scope], [ResourceType], [ResourcePublicId])
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SlugRegistry_Scope_ResourcePublicId_Covering'
      AND object_id = OBJECT_ID(N'[seo].[SlugRegistry]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SlugRegistry_Scope_ResourcePublicId_Covering]
    ON [seo].[SlugRegistry]
    (
        [Scope] ASC,
        [ResourceType] ASC,
        [ResourcePublicId] ASC
    )
    INCLUDE
    (
        [SlugId],
        [Slug],
        [CanonicalUrl],
        [IsIndexable],
        [IsActive],
        [SourceAggregateVersion],
        [LastAppliedMessageId],
        [LastSyncedAtUtc],
        [Version],
        [CreatedAtUtc],
        [CreatedByUserId],
        [UpdatedAtUtc],
        [UpdatedByUserId]
    );

    PRINT N'Created index: [IX_SlugRegistry_Scope_ResourcePublicId_Covering]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_SlugRegistry_Scope_ResourcePublicId_Covering]';
END
GO

/* Active route listing / routing-state admin investigation */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SlugRegistry_IsActive_IsIndexable_UpdatedAtUtc'
      AND object_id = OBJECT_ID(N'[seo].[SlugRegistry]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SlugRegistry_IsActive_IsIndexable_UpdatedAtUtc]
    ON [seo].[SlugRegistry]
    (
        [IsActive] ASC,
        [IsIndexable] ASC,
        [UpdatedAtUtc] DESC,
        [SlugId] DESC
    )
    INCLUDE
    (
        [Scope],
        [Slug],
        [ResourceType],
        [ResourcePublicId],
        [CanonicalUrl],
        [SourceAggregateVersion],
        [LastSyncedAtUtc],
        [Version]
    );

    PRINT N'Created index: [IX_SlugRegistry_IsActive_IsIndexable_UpdatedAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_SlugRegistry_IsActive_IsIndexable_UpdatedAtUtc]';
END
GO

/* Actor-based route change investigation */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SlugRegistry_UpdatedByUserId_UpdatedAtUtc'
      AND object_id = OBJECT_ID(N'[seo].[SlugRegistry]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SlugRegistry_UpdatedByUserId_UpdatedAtUtc]
    ON [seo].[SlugRegistry]
    (
        [UpdatedByUserId] ASC,
        [UpdatedAtUtc] DESC,
        [SlugId] DESC
    )
    INCLUDE
    (
        [Scope],
        [Slug],
        [ResourceType],
        [ResourcePublicId],
        [IsActive],
        [IsIndexable],
        [Version],
        [SourceAggregateVersion],
        [LastSyncedAtUtc]
    );

    PRINT N'Created index: [IX_SlugRegistry_UpdatedByUserId_UpdatedAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_SlugRegistry_UpdatedByUserId_UpdatedAtUtc]';
END
GO

/* Created-by investigation / audit-ish maintenance */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SlugRegistry_CreatedByUserId_CreatedAtUtc'
      AND object_id = OBJECT_ID(N'[seo].[SlugRegistry]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SlugRegistry_CreatedByUserId_CreatedAtUtc]
    ON [seo].[SlugRegistry]
    (
        [CreatedByUserId] ASC,
        [CreatedAtUtc] DESC,
        [SlugId] DESC
    )
    INCLUDE
    (
        [Scope],
        [Slug],
        [ResourceType],
        [ResourcePublicId],
        [IsActive],
        [IsIndexable],
        [Version]
    );

    PRINT N'Created index: [IX_SlugRegistry_CreatedByUserId_CreatedAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_SlugRegistry_CreatedByUserId_CreatedAtUtc]';
END
GO

/* Async apply / sync lag investigation */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SlugRegistry_SourceAggregateVersion_LastSyncedAtUtc'
      AND object_id = OBJECT_ID(N'[seo].[SlugRegistry]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SlugRegistry_SourceAggregateVersion_LastSyncedAtUtc]
    ON [seo].[SlugRegistry]
    (
        [SourceAggregateVersion] DESC,
        [LastSyncedAtUtc] DESC,
        [SlugId] DESC
    )
    INCLUDE
    (
        [Scope],
        [Slug],
        [ResourceType],
        [ResourcePublicId],
        [IsActive],
        [IsIndexable],
        [LastAppliedMessageId],
        [Version]
    );

    PRINT N'Created index: [IX_SlugRegistry_SourceAggregateVersion_LastSyncedAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_SlugRegistry_SourceAggregateVersion_LastSyncedAtUtc]';
END
GO