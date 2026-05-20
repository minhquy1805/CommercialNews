/*
  File: db/10_modules/reading/010_indexes.sql
  Module: Reading
  Purpose:
  - Create supporting indexes for Reading derived projection tables in CommercialNews V1.
  - Optimize:
      * public article listing
      * public detail lookup by public id
      * public detail lookup by slug
      * category/tag filtering
      * popularity sorting
      * related article lookup
      * projection freshness diagnostics
      * async projection apply diagnostics
  - Idempotent: safe to re-run.

  Notes:
  - Reading projection tables are derived state.
  - Source truth remains in Content / SEO / Media / Interaction.
  - Table creation belongs in 001_tables.sql.
  - Stored procedures belong in 020_procs.sql.
  - Keep indexes focused on known V1 access patterns; avoid speculative over-indexing.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 56101, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

USE [CommercialNews];
GO

IF SCHEMA_ID(N'reading') IS NULL
BEGIN
    THROW 56102, 'Schema [reading] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

IF OBJECT_ID(N'[reading].[ArticleReadModel]', N'U') IS NULL
BEGIN
    THROW 56103, 'Table [reading].[ArticleReadModel] does not exist. Run reading/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[reading].[ArticleReadModelTag]', N'U') IS NULL
BEGIN
    THROW 56104, 'Table [reading].[ArticleReadModelTag] does not exist. Run reading/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[reading].[ArticleReadModelMedia]', N'U') IS NULL
BEGIN
    THROW 56105, 'Table [reading].[ArticleReadModelMedia] does not exist. Run reading/001_tables.sql first.', 1;
END
GO

/* =========================================================
   1) [reading].[ArticleReadModel]
   ========================================================= */

/* Public detail lookup by slug */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleReadModel_Public_Slug'
      AND object_id = OBJECT_ID(N'[reading].[ArticleReadModel]')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_ArticleReadModel_Public_Slug]
    ON [reading].[ArticleReadModel]
    (
        [Slug] ASC
    )
    INCLUDE
    (
        [ArticleId],
        [ArticlePublicId],
        [Title],
        [Summary],
        [PublishedAtUtc],
        [SourceVersion],
        [LastSyncedAtUtc]
    )
    WHERE [Slug] IS NOT NULL
      AND [IsPublic] = 1;

    PRINT N'Created index: [IX_ArticleReadModel_Public_Slug]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleReadModel_Public_Slug]';
END
GO

/* Public article listing: newest first */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleReadModel_Public_PublishedAt'
      AND object_id = OBJECT_ID(N'[reading].[ArticleReadModel]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleReadModel_Public_PublishedAt]
    ON [reading].[ArticleReadModel]
    (
        [PublishedAtUtc] DESC,
        [ArticleId] DESC
    )
    INCLUDE
    (
        [ArticlePublicId],
        [Slug],
        [Title],
        [Summary],
        [CategoryId],
        [CategoryName],
        [AuthorUserId],
        [AuthorDisplayName],
        [CoverMediaId],
        [CoverMediaUrl],
        [CoverAlt],
        [ViewCount],
        [LikeCount],
        [CommentCount],
        [PopularityScore]
    )
    WHERE [IsPublic] = 1;

    PRINT N'Created index: [IX_ArticleReadModel_Public_PublishedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleReadModel_Public_PublishedAt]';
END
GO

/* Public category feed */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleReadModel_Public_Category_PublishedAt'
      AND object_id = OBJECT_ID(N'[reading].[ArticleReadModel]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleReadModel_Public_Category_PublishedAt]
    ON [reading].[ArticleReadModel]
    (
        [CategoryId] ASC,
        [PublishedAtUtc] DESC,
        [ArticleId] DESC
    )
    INCLUDE
    (
        [ArticlePublicId],
        [Slug],
        [Title],
        [Summary],
        [CategoryName],
        [AuthorUserId],
        [AuthorDisplayName],
        [CoverMediaUrl],
        [CoverAlt],
        [ViewCount],
        [LikeCount],
        [CommentCount],
        [PopularityScore]
    )
    WHERE [IsPublic] = 1;

    PRINT N'Created index: [IX_ArticleReadModel_Public_Category_PublishedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleReadModel_Public_Category_PublishedAt]';
END
GO

/* Public popularity sort */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleReadModel_Public_Popularity'
      AND object_id = OBJECT_ID(N'[reading].[ArticleReadModel]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleReadModel_Public_Popularity]
    ON [reading].[ArticleReadModel]
    (
        [PopularityScore] DESC,
        [PublishedAtUtc] DESC,
        [ArticleId] DESC
    )
    INCLUDE
    (
        [ArticlePublicId],
        [Slug],
        [Title],
        [Summary],
        [CategoryId],
        [CategoryName],
        [CoverMediaUrl],
        [CoverAlt],
        [ViewCount],
        [LikeCount],
        [CommentCount]
    )
    WHERE [IsPublic] = 1
      AND [PopularityScore] IS NOT NULL;

    PRINT N'Created index: [IX_ArticleReadModel_Public_Popularity]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleReadModel_Public_Popularity]';
END
GO

/* Projection apply / freshness diagnostics */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleReadModel_SourceVersion'
      AND object_id = OBJECT_ID(N'[reading].[ArticleReadModel]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleReadModel_SourceVersion]
    ON [reading].[ArticleReadModel]
    (
        [ArticleId] ASC,
        [SourceVersion] ASC
    )
    INCLUDE
    (
        [ArticlePublicId],
        [Status],
        [IsPublic],
        [LastEventMessageId],
        [LastSourceOccurredAtUtc],
        [LastSyncedAtUtc]
    );

    PRINT N'Created index: [IX_ArticleReadModel_SourceVersion]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleReadModel_SourceVersion]';
END
GO

/* Projection freshness scan / reconciliation */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleReadModel_LastSyncedAtUtc'
      AND object_id = OBJECT_ID(N'[reading].[ArticleReadModel]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleReadModel_LastSyncedAtUtc]
    ON [reading].[ArticleReadModel]
    (
        [LastSyncedAtUtc] ASC,
        [ArticleId] ASC
    )
    INCLUDE
    (
        [ArticlePublicId],
        [SourceVersion],
        [Status],
        [IsPublic]
    );

    PRINT N'Created index: [IX_ArticleReadModel_LastSyncedAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleReadModel_LastSyncedAtUtc]';
END
GO

/* Public search fallback support */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleReadModel_Public_SearchFields'
      AND object_id = OBJECT_ID(N'[reading].[ArticleReadModel]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleReadModel_Public_SearchFields]
    ON [reading].[ArticleReadModel]
    (
        [PublishedAtUtc] DESC,
        [ArticleId] DESC
    )
    INCLUDE
    (
        [ArticlePublicId],
        [Slug],
        [Title],
        [Summary],
        [CategoryId],
        [CategoryName]
    )
    WHERE [IsPublic] = 1;

    PRINT N'Created index: [IX_ArticleReadModel_Public_SearchFields]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleReadModel_Public_SearchFields]';
END
GO

/* =========================================================
   2) [reading].[ArticleReadModelTag]
   ========================================================= */

/* Reverse traversal: public articles by projected tag */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleReadModelTag_TagId_ArticleId'
      AND object_id = OBJECT_ID(N'[reading].[ArticleReadModelTag]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleReadModelTag_TagId_ArticleId]
    ON [reading].[ArticleReadModelTag]
    (
        [TagId] ASC,
        [ArticleId] ASC
    )
    INCLUDE
    (
        [TagPublicId],
        [Name],
        [Slug],
        [SourceVersion],
        [LastSyncedAtUtc]
    );

    PRINT N'Created index: [IX_ArticleReadModelTag_TagId_ArticleId]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleReadModelTag_TagId_ArticleId]';
END
GO

/* Tag slug lookup / diagnostics */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleReadModelTag_Slug'
      AND object_id = OBJECT_ID(N'[reading].[ArticleReadModelTag]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleReadModelTag_Slug]
    ON [reading].[ArticleReadModelTag]
    (
        [Slug] ASC,
        [ArticleId] ASC
    )
    INCLUDE
    (
        [TagId],
        [TagPublicId],
        [Name]
    )
    WHERE [Slug] IS NOT NULL;

    PRINT N'Created index: [IX_ArticleReadModelTag_Slug]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleReadModelTag_Slug]';
END
GO

/* =========================================================
   3) [reading].[ArticleReadModelMedia]
   ========================================================= */

/* Detail media gallery / primary-first ordering */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleReadModelMedia_Article_Primary_Order'
      AND object_id = OBJECT_ID(N'[reading].[ArticleReadModelMedia]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleReadModelMedia_Article_Primary_Order]
    ON [reading].[ArticleReadModelMedia]
    (
        [ArticleId] ASC,
        [IsPrimary] DESC,
        [SortOrder] ASC,
        [MediaId] ASC
    )
    INCLUDE
    (
        [Url],
        [Alt],
        [MediaType],
        [SourceVersion],
        [LastSyncedAtUtc]
    );

    PRINT N'Created index: [IX_ArticleReadModelMedia_Article_Primary_Order]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleReadModelMedia_Article_Primary_Order]';
END
GO

/* Media projection diagnostics */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleReadModelMedia_MediaId'
      AND object_id = OBJECT_ID(N'[reading].[ArticleReadModelMedia]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleReadModelMedia_MediaId]
    ON [reading].[ArticleReadModelMedia]
    (
        [MediaId] ASC,
        [ArticleId] ASC
    )
    INCLUDE
    (
        [IsPrimary],
        [SortOrder],
        [MediaType],
        [LastSyncedAtUtc]
    );

    PRINT N'Created index: [IX_ArticleReadModelMedia_MediaId]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleReadModelMedia_MediaId]';
END
GO