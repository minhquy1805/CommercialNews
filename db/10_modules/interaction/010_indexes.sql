/*
  File: db/10_modules/interaction/010_indexes.sql
  Module: Interaction
  Purpose:
  - Create supporting indexes for Interaction truth and derived tables in CommercialNews V1.
  - Optimize:
      * non-blocking raw view event write + bounded retention cleanup
      * like/unlike truth lookup and active-like counting
      * comment paging by article and visibility
      * optional parent-thread traversal
      * derived stats lookup for later Reading enrichment and popularity sorting
  - Idempotent: safe to re-run.

  Notes:
  - Truth remains in [interaction] tables.
  - ArticleViewEvent is high-write: keep indexes intentionally minimal.
  - ArticleInteractionStats is derived state, not correctness truth.
  - Redis / outbox / broker / worker / projections are outside this file.
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
    THROW 58101, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

USE [CommercialNews];
GO

IF SCHEMA_ID(N'interaction') IS NULL
BEGIN
    THROW 58102, 'Schema [interaction] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

IF OBJECT_ID(N'[interaction].[ArticleViewEvent]', N'U') IS NULL
BEGIN
    THROW 58103, 'Table [interaction].[ArticleViewEvent] does not exist. Run interaction/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[interaction].[ArticleLike]', N'U') IS NULL
BEGIN
    THROW 58104, 'Table [interaction].[ArticleLike] does not exist. Run interaction/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[interaction].[Comment]', N'U') IS NULL
BEGIN
    THROW 58105, 'Table [interaction].[Comment] does not exist. Run interaction/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[interaction].[ArticleInteractionStats]', N'U') IS NULL
BEGIN
    THROW 58106, 'Table [interaction].[ArticleInteractionStats] does not exist. Run interaction/001_tables.sql first.', 1;
END
GO

/* =========================================================
   1) [interaction].[ArticleViewEvent]
   ========================================================= */

/* Article-bounded view lookup / counting / bounded replay input */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleViewEvent_ArticleId_ViewedAt'
      AND object_id = OBJECT_ID(N'[interaction].[ArticleViewEvent]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleViewEvent_ArticleId_ViewedAt]
    ON [interaction].[ArticleViewEvent]
    (
        [ArticleId] ASC,
        [ViewedAt] DESC
    )
    INCLUDE
    (
        [ArticleViewEventId],
        [UserId],
        [VisitorKey]
    );

    PRINT N'Created index: [IX_ArticleViewEvent_ArticleId_ViewedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleViewEvent_ArticleId_ViewedAt]';
END
GO

/* Retention / purge window by ViewedAt */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleViewEvent_ViewedAt'
      AND object_id = OBJECT_ID(N'[interaction].[ArticleViewEvent]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleViewEvent_ViewedAt]
    ON [interaction].[ArticleViewEvent]
    (
        [ViewedAt] ASC
    )
    INCLUDE
    (
        [ArticleViewEventId],
        [ArticleId]
    );

    PRINT N'Created index: [IX_ArticleViewEvent_ViewedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleViewEvent_ViewedAt]';
END
GO

/* =========================================================
   2) [interaction].[ArticleLike]
   ========================================================= */

/*
   Note:
   - [UQ_ArticleLike_ArticleId_UserId] already exists as a UNIQUE constraint
     and supports truth lookup by (ArticleId, UserId).
   - Additional indexes below focus on active-like counting / user-centric reads.
*/

/* Active-like counting by article */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleLike_ArticleId_IsActive'
      AND object_id = OBJECT_ID(N'[interaction].[ArticleLike]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleLike_ArticleId_IsActive]
    ON [interaction].[ArticleLike]
    (
        [ArticleId] ASC,
        [IsActive] ASC
    )
    INCLUDE
    (
        [ArticleLikeId],
        [UserId],
        [LikedAt],
        [UnlikedAt],
        [UpdatedAt]
    );

    PRINT N'Created index: [IX_ArticleLike_ArticleId_IsActive]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleLike_ArticleId_IsActive]';
END
GO

/* Optional user-centric activity lookup */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleLike_UserId_IsActive_LikedAt'
      AND object_id = OBJECT_ID(N'[interaction].[ArticleLike]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleLike_UserId_IsActive_LikedAt]
    ON [interaction].[ArticleLike]
    (
        [UserId] ASC,
        [IsActive] ASC,
        [LikedAt] DESC
    )
    INCLUDE
    (
        [ArticleLikeId],
        [ArticleId],
        [UnlikedAt],
        [UpdatedAt]
    );

    PRINT N'Created index: [IX_ArticleLike_UserId_IsActive_LikedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleLike_UserId_IsActive_LikedAt]';
END
GO

/* =========================================================
   3) [interaction].[Comment]
   ========================================================= */

/* Public comment paging by article + visibility */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Comment_ArticleId_Status_CreatedAt'
      AND object_id = OBJECT_ID(N'[interaction].[Comment]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Comment_ArticleId_Status_CreatedAt]
    ON [interaction].[Comment]
    (
        [ArticleId] ASC,
        [Status] ASC,
        [CreatedAt] DESC
    )
    INCLUDE
    (
        [CommentId],
        [UserId],
        [ParentCommentId],
        [UpdatedAt],
        [DeletedAt],
        [DeletedByUserId],
        [EditCount]
    );

    PRINT N'Created index: [IX_Comment_ArticleId_Status_CreatedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_Comment_ArticleId_Status_CreatedAt]';
END
GO

/* Parent-thread traversal / reply listing */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Comment_ParentCommentId_CreatedAt'
      AND object_id = OBJECT_ID(N'[interaction].[Comment]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Comment_ParentCommentId_CreatedAt]
    ON [interaction].[Comment]
    (
        [ParentCommentId] ASC,
        [CreatedAt] ASC
    )
    INCLUDE
    (
        [CommentId],
        [ArticleId],
        [UserId],
        [Status],
        [UpdatedAt],
        [DeletedAt],
        [EditCount]
    );

    PRINT N'Created index: [IX_Comment_ParentCommentId_CreatedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_Comment_ParentCommentId_CreatedAt]';
END
GO

/* Optional author-centric comment lookup / moderation investigation */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Comment_UserId_CreatedAt'
      AND object_id = OBJECT_ID(N'[interaction].[Comment]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Comment_UserId_CreatedAt]
    ON [interaction].[Comment]
    (
        [UserId] ASC,
        [CreatedAt] DESC
    )
    INCLUDE
    (
        [CommentId],
        [ArticleId],
        [Status],
        [UpdatedAt],
        [DeletedAt],
        [EditCount]
    );

    PRINT N'Created index: [IX_Comment_UserId_CreatedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_Comment_UserId_CreatedAt]';
END
GO

/* =========================================================
   4) [interaction].[ArticleInteractionStats]
   ========================================================= */

/* Article stats lookup for Reading enrichment / counters endpoint */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleInteractionStats_ArticleId'
      AND object_id = OBJECT_ID(N'[interaction].[ArticleInteractionStats]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleInteractionStats_ArticleId]
    ON [interaction].[ArticleInteractionStats]
    (
        [ArticleId] ASC
    )
    INCLUDE
    (
        [ViewsTotal],
        [LikesTotal],
        [CommentsTotal],
        [PopularityScore],
        [CreatedAt],
        [UpdatedAt],
        [LastAggregatedAt]
    );

    PRINT N'Created index: [IX_ArticleInteractionStats_ArticleId]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleInteractionStats_ArticleId]';
END
GO

/* Derived popularity sort / trending candidate reads */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleInteractionStats_PopularityScore_ArticleId'
      AND object_id = OBJECT_ID(N'[interaction].[ArticleInteractionStats]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleInteractionStats_PopularityScore_ArticleId]
    ON [interaction].[ArticleInteractionStats]
    (
        [PopularityScore] DESC,
        [ArticleId] DESC
    )
    INCLUDE
    (
        [ViewsTotal],
        [LikesTotal],
        [CommentsTotal],
        [UpdatedAt],
        [LastAggregatedAt]
    );

    PRINT N'Created index: [IX_ArticleInteractionStats_PopularityScore_ArticleId]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleInteractionStats_PopularityScore_ArticleId]';
END
GO

/* Aggregate freshness / reconciliation investigation */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleInteractionStats_UpdatedAt_ArticleId'
      AND object_id = OBJECT_ID(N'[interaction].[ArticleInteractionStats]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleInteractionStats_UpdatedAt_ArticleId]
    ON [interaction].[ArticleInteractionStats]
    (
        [UpdatedAt] DESC,
        [ArticleId] DESC
    )
    INCLUDE
    (
        [ViewsTotal],
        [LikesTotal],
        [CommentsTotal],
        [PopularityScore],
        [LastAggregatedAt]
    );

    PRINT N'Created index: [IX_ArticleInteractionStats_UpdatedAt_ArticleId]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleInteractionStats_UpdatedAt_ArticleId]';
END
GO