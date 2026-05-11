/*
  File: db/10_modules/content/010_indexes.sql
  Module: Content
  Purpose:
  - Create supporting indexes for Content truth tables in CommercialNews V1.
  - Optimize:
      * public feed / public detail lookup
      * admin article listing / filtering
      * taxonomy lookup
      * article-tag traversal
      * revision timeline
      * lifecycle timeline
  - Idempotent: safe to re-run.

  Notes:
  - Truth remains in [content] tables.
  - Public read models / search / cache are outside this file.
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
    THROW 54101, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

USE [CommercialNews];
GO

IF SCHEMA_ID(N'content') IS NULL
BEGIN
    THROW 54102, 'Schema [content] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

IF OBJECT_ID(N'[content].[Category]', N'U') IS NULL
BEGIN
    THROW 54103, 'Table [content].[Category] does not exist. Run content/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[content].[Tag]', N'U') IS NULL
BEGIN
    THROW 54104, 'Table [content].[Tag] does not exist. Run content/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[content].[Article]', N'U') IS NULL
BEGIN
    THROW 54105, 'Table [content].[Article] does not exist. Run content/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[content].[ArticleTag]', N'U') IS NULL
BEGIN
    THROW 54106, 'Table [content].[ArticleTag] does not exist. Run content/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[content].[ArticleRevision]', N'U') IS NULL
BEGIN
    THROW 54107, 'Table [content].[ArticleRevision] does not exist. Run content/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[content].[ArticleLifecycleEvent]', N'U') IS NULL
BEGIN
    THROW 54108, 'Table [content].[ArticleLifecycleEvent] does not exist. Run content/001_tables.sql first.', 1;
END
GO

/* =========================================================
   1) [content].[Category]
   ========================================================= */

/* Active category listing / admin taxonomy ordering */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Category_IsDeleted_IsActive_DisplayOrder_Name'
      AND object_id = OBJECT_ID(N'[content].[Category]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Category_IsDeleted_IsActive_DisplayOrder_Name]
    ON [content].[Category]
    (
        [IsDeleted] ASC,
        [IsActive] ASC,
        [DisplayOrder] ASC,
        [Name] ASC
    )
    INCLUDE
    (
        [CategoryId],
        [PublicId],
        [ParentCategoryId],
        [UpdatedAt]
    );

    PRINT N'Created index: [IX_Category_IsDeleted_IsActive_DisplayOrder_Name]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_Category_IsDeleted_IsActive_DisplayOrder_Name]';
END
GO

/* Children lookup under a parent category */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Category_ParentCategoryId_IsDeleted_DisplayOrder_Name'
      AND object_id = OBJECT_ID(N'[content].[Category]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Category_ParentCategoryId_IsDeleted_DisplayOrder_Name]
    ON [content].[Category]
    (
        [ParentCategoryId] ASC,
        [IsDeleted] ASC,
        [DisplayOrder] ASC,
        [Name] ASC
    )
    INCLUDE
    (
        [CategoryId],
        [PublicId],
        [IsActive]
    );

    PRINT N'Created index: [IX_Category_ParentCategoryId_IsDeleted_DisplayOrder_Name]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_Category_ParentCategoryId_IsDeleted_DisplayOrder_Name]';
END
GO

/* =========================================================
   2) [content].[Tag]
   ========================================================= */

/* Active tag listing / admin tag management */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Tag_IsDeleted_IsActive_Name'
      AND object_id = OBJECT_ID(N'[content].[Tag]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Tag_IsDeleted_IsActive_Name]
    ON [content].[Tag]
    (
        [IsDeleted] ASC,
        [IsActive] ASC,
        [Name] ASC
    )
    INCLUDE
    (
        [TagId],
        [PublicId],
        [UpdatedAt]
    );

    PRINT N'Created index: [IX_Tag_IsDeleted_IsActive_Name]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_Tag_IsDeleted_IsActive_Name]';
END
GO

/* =========================================================
   3) [content].[Article]
   ========================================================= */

/* Public feed: latest published non-deleted articles */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Article_PublicFeed_PublishedAt'
      AND object_id = OBJECT_ID(N'[content].[Article]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Article_PublicFeed_PublishedAt]
    ON [content].[Article]
    (
        [PublishedAt] DESC,
        [ArticleId] DESC
    )
    INCLUDE
    (
        [ArticlePublicId],
        [CategoryId],
        [AuthorUserId],
        [Title],
        [Summary],
        [CoverMediaId]
    )
    WHERE [IsDeleted] = 0
      AND [Status] = N'Published';

    PRINT N'Created index: [IX_Article_PublicFeed_PublishedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_Article_PublicFeed_PublishedAt]';
END
GO

/* Category-specific public feed */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Article_PublicFeed_Category_PublishedAt'
      AND object_id = OBJECT_ID(N'[content].[Article]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Article_PublicFeed_Category_PublishedAt]
    ON [content].[Article]
    (
        [CategoryId] ASC,
        [PublishedAt] DESC,
        [ArticleId] DESC
    )
    INCLUDE
    (
        [ArticlePublicId],
        [AuthorUserId],
        [Title],
        [Summary],
        [CoverMediaId]
    )
    WHERE [IsDeleted] = 0
      AND [Status] = N'Published';

    PRINT N'Created index: [IX_Article_PublicFeed_Category_PublishedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_Article_PublicFeed_Category_PublishedAt]';
END
GO

/* Admin list: status-first filtering */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Article_Admin_Status_IsDeleted_UpdatedAt'
      AND object_id = OBJECT_ID(N'[content].[Article]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Article_Admin_Status_IsDeleted_UpdatedAt]
    ON [content].[Article]
    (
        [Status] ASC,
        [IsDeleted] ASC,
        [UpdatedAt] DESC,
        [ArticleId] DESC
    )
    INCLUDE
    (
        [ArticlePublicId],
        [CategoryId],
        [AuthorUserId],
        [Title],
        [Summary],
        [PublishedAt],
        [ArchivedAt],
        [Version]
    );

    PRINT N'Created index: [IX_Article_Admin_Status_IsDeleted_UpdatedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_Article_Admin_Status_IsDeleted_UpdatedAt]';
END
GO

/* Admin list: author dashboard / authored articles */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Article_AuthorUserId_IsDeleted_CreatedAt'
      AND object_id = OBJECT_ID(N'[content].[Article]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Article_AuthorUserId_IsDeleted_CreatedAt]
    ON [content].[Article]
    (
        [AuthorUserId] ASC,
        [IsDeleted] ASC,
        [CreatedAt] DESC,
        [ArticleId] DESC
    )
    INCLUDE
    (
        [ArticlePublicId],
        [CategoryId],
        [Status],
        [Title],
        [Summary],
        [PublishedAt],
        [UpdatedAt],
        [Version]
    );

    PRINT N'Created index: [IX_Article_AuthorUserId_IsDeleted_CreatedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_Article_AuthorUserId_IsDeleted_CreatedAt]';
END
GO

/* Admin/category filter */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Article_CategoryId_IsDeleted_UpdatedAt'
      AND object_id = OBJECT_ID(N'[content].[Article]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Article_CategoryId_IsDeleted_UpdatedAt]
    ON [content].[Article]
    (
        [CategoryId] ASC,
        [IsDeleted] ASC,
        [UpdatedAt] DESC,
        [ArticleId] DESC
    )
    INCLUDE
    (
        [ArticlePublicId],
        [AuthorUserId],
        [Status],
        [Title],
        [Summary],
        [PublishedAt],
        [ArchivedAt],
        [Version]
    );

    PRINT N'Created index: [IX_Article_CategoryId_IsDeleted_UpdatedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_Article_CategoryId_IsDeleted_UpdatedAt]';
END
GO

/* ArticlePublicId lookup */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Article_ArticlePublicId'
      AND object_id = OBJECT_ID(N'[content].[Article]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Article_ArticlePublicId]
    ON [content].[Article]
    (
        [ArticlePublicId] ASC
    )
    INCLUDE
    (
        [ArticleId],
        [Status],
        [IsDeleted],
        [Version]
    );

    PRINT N'Created index: [IX_Article_ArticlePublicId]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_Article_ArticlePublicId]';
END
GO

/* Optional keyword-ish admin fallback by title prefix / ordering */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Article_IsDeleted_Title'
      AND object_id = OBJECT_ID(N'[content].[Article]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Article_IsDeleted_Title]
    ON [content].[Article]
    (
        [IsDeleted] ASC,
        [Title] ASC
    )
    INCLUDE
    (
        [ArticleId],
        [ArticlePublicId],
        [Status],
        [UpdatedAt],
        [CategoryId],
        [AuthorUserId]
    );

    PRINT N'Created index: [IX_Article_IsDeleted_Title]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_Article_IsDeleted_Title]';
END
GO

/* =========================================================
   4) [content].[ArticleTag]
   ========================================================= */

/* Reverse traversal: find articles by tag efficiently */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleTag_TagId_ArticleId'
      AND object_id = OBJECT_ID(N'[content].[ArticleTag]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleTag_TagId_ArticleId]
    ON [content].[ArticleTag]
    (
        [TagId] ASC,
        [ArticleId] ASC
    )
    INCLUDE
    (
        [CreatedAt],
        [CreatedByUserId]
    );

    PRINT N'Created index: [IX_ArticleTag_TagId_ArticleId]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleTag_TagId_ArticleId]';
END
GO

/* Timeline / diagnostics for article-tag attach order */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleTag_ArticleId_CreatedAt'
      AND object_id = OBJECT_ID(N'[content].[ArticleTag]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleTag_ArticleId_CreatedAt]
    ON [content].[ArticleTag]
    (
        [ArticleId] ASC,
        [CreatedAt] DESC,
        [TagId] ASC
    );

    PRINT N'Created index: [IX_ArticleTag_ArticleId_CreatedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleTag_ArticleId_CreatedAt]';
END
GO

/* =========================================================
   5) [content].[ArticleRevision]
   ========================================================= */

/* Revision timeline per article */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleRevision_ArticleId_EditedAt'
      AND object_id = OBJECT_ID(N'[content].[ArticleRevision]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleRevision_ArticleId_EditedAt]
    ON [content].[ArticleRevision]
    (
        [ArticleId] ASC,
        [EditedAt] DESC,
        [RevisionId] DESC
    )
    INCLUDE
    (
        [EditedByUserId],
        [CorrelationId],
        [ChangeSummary]
    );

    PRINT N'Created index: [IX_ArticleRevision_ArticleId_EditedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleRevision_ArticleId_EditedAt]';
END
GO

/* Actor-based audit investigation */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleRevision_EditedByUserId_EditedAt'
      AND object_id = OBJECT_ID(N'[content].[ArticleRevision]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleRevision_EditedByUserId_EditedAt]
    ON [content].[ArticleRevision]
    (
        [EditedByUserId] ASC,
        [EditedAt] DESC,
        [RevisionId] DESC
    )
    INCLUDE
    (
        [ArticleId],
        [CorrelationId],
        [ChangeSummary]
    );

    PRINT N'Created index: [IX_ArticleRevision_EditedByUserId_EditedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleRevision_EditedByUserId_EditedAt]';
END
GO

/* =========================================================
   6) [content].[ArticleLifecycleEvent]
   ========================================================= */

/* Lifecycle timeline per article */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleLifecycleEvent_ArticleId_OccurredAt'
      AND object_id = OBJECT_ID(N'[content].[ArticleLifecycleEvent]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleLifecycleEvent_ArticleId_OccurredAt]
    ON [content].[ArticleLifecycleEvent]
    (
        [ArticleId] ASC,
        [OccurredAt] DESC,
        [EventId] DESC
    )
    INCLUDE
    (
        [ArticleVersion],
        [ActionType],
        [FromStatus],
        [ToStatus],
        [ActorUserId],
        [Reason]
    );

    PRINT N'Created index: [IX_ArticleLifecycleEvent_ArticleId_OccurredAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleLifecycleEvent_ArticleId_OccurredAt]';
END
GO

/* Lifecycle event lookup by aggregate version */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleLifecycleEvent_ArticleId_Version'
      AND object_id = OBJECT_ID(N'[content].[ArticleLifecycleEvent]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleLifecycleEvent_ArticleId_Version]
    ON [content].[ArticleLifecycleEvent]
    (
        [ArticleId] ASC,
        [ArticleVersion] ASC
    )
    INCLUDE
    (
        [EventId],
        [ActionType],
        [OccurredAt],
        [ActorUserId],
        [CorrelationId]
    );

    PRINT N'Created index: [IX_ArticleLifecycleEvent_ArticleId_Version]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleLifecycleEvent_ArticleId_Version]';
END
GO

/* Actor investigation / moderation audit */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleLifecycleEvent_ActorUserId_OccurredAt'
      AND object_id = OBJECT_ID(N'[content].[ArticleLifecycleEvent]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleLifecycleEvent_ActorUserId_OccurredAt]
    ON [content].[ArticleLifecycleEvent]
    (
        [ActorUserId] ASC,
        [OccurredAt] DESC,
        [EventId] DESC
    )
    INCLUDE
    (
        [ArticleId],
        [ArticleVersion],
        [ActionType],
        [FromStatus],
        [ToStatus]
    );

    PRINT N'Created index: [IX_ArticleLifecycleEvent_ActorUserId_OccurredAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleLifecycleEvent_ActorUserId_OccurredAt]';
END
GO

/* Action-type investigation */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ArticleLifecycleEvent_ActionType_OccurredAt'
      AND object_id = OBJECT_ID(N'[content].[ArticleLifecycleEvent]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleLifecycleEvent_ActionType_OccurredAt]
    ON [content].[ArticleLifecycleEvent]
    (
        [ActionType] ASC,
        [OccurredAt] DESC,
        [EventId] DESC
    )
    INCLUDE
    (
        [ArticleId],
        [ArticleVersion],
        [ActorUserId],
        [FromStatus],
        [ToStatus]
    );

    PRINT N'Created index: [IX_ArticleLifecycleEvent_ActionType_OccurredAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleLifecycleEvent_ActionType_OccurredAt]';
END
GO
