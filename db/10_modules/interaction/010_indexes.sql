/*
  File: db/10_modules/interaction/010_indexes.sql
  Module: Interaction
  Purpose:
  - Create supporting indexes for Interaction V1 Async + Admin Moderation.
  - Optimize:
      * eligibility projection resync diagnostics
      * active-like recount and like-state support
      * public visible top-level comment paging
      * admin comment moderation queues
      * comment report lookup and case investigation
      * one-open-case-per-comment enforcement
      * moderation-case admin queues and alert inspection
      * moderation action history lookup
  - Idempotent: safe to re-run.

  Notes:
  - Unique public-id and one-row-per-article constraints declared in
    001_tables.sql already create supporting unique indexes.
  - ArticleViewCount is a hot atomic-update table; no speculative secondary
    indexes are added in V1.
  - ArticleInteractionStats is accessed by ArticlePublicId through its
    unique constraint; popularity/trending indexes are outside V1 scope.
  - V1 does not store ArticleViewEvent raw history.
  - V1 does not support reply traversal queries; ParentCommentId is reserved
    only for future compatibility.
  - Shared Outbox indexes are defined by the Outbox module, not here.
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

IF OBJECT_ID(N'[interaction].[ArticleInteractionTargetProjection]', N'U') IS NULL
BEGIN
    THROW 58103, 'Table [interaction].[ArticleInteractionTargetProjection] does not exist. Run interaction/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[interaction].[ArticleViewCount]', N'U') IS NULL
BEGIN
    THROW 58104, 'Table [interaction].[ArticleViewCount] does not exist. Run interaction/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[interaction].[ArticleLike]', N'U') IS NULL
BEGIN
    THROW 58105, 'Table [interaction].[ArticleLike] does not exist. Run interaction/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[interaction].[Comment]', N'U') IS NULL
BEGIN
    THROW 58106, 'Table [interaction].[Comment] does not exist. Run interaction/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[interaction].[CommentReport]', N'U') IS NULL
BEGIN
    THROW 58107, 'Table [interaction].[CommentReport] does not exist. Run interaction/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[interaction].[CommentModerationCase]', N'U') IS NULL
BEGIN
    THROW 58108, 'Table [interaction].[CommentModerationCase] does not exist. Run interaction/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[interaction].[CommentModerationActionHistory]', N'U') IS NULL
BEGIN
    THROW 58109, 'Table [interaction].[CommentModerationActionHistory] does not exist. Run interaction/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[interaction].[ArticleInteractionStats]', N'U') IS NULL
BEGIN
    THROW 58110, 'Table [interaction].[ArticleInteractionStats] does not exist. Run interaction/001_tables.sql first.', 1;
END
GO

/* =========================================================
   1) [interaction].[ArticleInteractionTargetProjection]
   ========================================================= */

/*
   Note:
   - [UQ_ArticleInteractionTargetProjection_ArticlePublicId] already supports
     the hot eligibility lookup by ArticlePublicId.
   - This filtered index is for bounded repair/resync operations only.
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_ArticleInteractionTargetProjection_RequiresResync_UpdatedAtUtc'
      AND [object_id] = OBJECT_ID(N'[interaction].[ArticleInteractionTargetProjection]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleInteractionTargetProjection_RequiresResync_UpdatedAtUtc]
    ON [interaction].[ArticleInteractionTargetProjection]
    (
        [UpdatedAtUtc] ASC,
        [ArticleInteractionTargetProjectionId] ASC
    )
    INCLUDE
    (
        [ArticlePublicId],
        [SourceStatus],
        [IsInteractionEnabled],
        [LastSourceVersion],
        [LastSourceMessageId],
        [LastSyncedAtUtc]
    )
    WHERE [RequiresResync] = 1;

    PRINT N'Created index: [IX_ArticleInteractionTargetProjection_RequiresResync_UpdatedAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleInteractionTargetProjection_RequiresResync_UpdatedAtUtc]';
END
GO

/* =========================================================
   2) [interaction].[ArticleViewCount]
   ========================================================= */

/*
   Note:
   - [UQ_ArticleViewCount_ArticlePublicId] already supports:
       * atomic view-counter lookup/update by ArticlePublicId
       * stats materialization lookup by ArticlePublicId
       * admin diagnostics lookup by ArticlePublicId
   - No additional V1 index is intentionally added because this is a
     hot-update table and extra indexes increase write amplification.
*/
PRINT N'No additional secondary indexes required for [interaction].[ArticleViewCount] in V1.';
GO

/* =========================================================
   3) [interaction].[ArticleLike]
   ========================================================= */

/*
   Note:
   - [UQ_ArticleLike_ArticlePublicId_UserId] already supports:
       * current user like-state lookup
       * idempotent like/unlike relationship mutation
   - The index below supports LikeCount reconciliation/materialization
     using only active relationship rows.
*/

/* Active-like recount/materialization by article */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_ArticleLike_ArticlePublicId_Active'
      AND [object_id] = OBJECT_ID(N'[interaction].[ArticleLike]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleLike_ArticlePublicId_Active]
    ON [interaction].[ArticleLike]
    (
        [ArticlePublicId] ASC,
        [ArticleLikeId] ASC
    )
    INCLUDE
    (
        [PublicId],
        [UserId],
        [LikedAtUtc],
        [Version],
        [UpdatedAtUtc]
    )
    WHERE [IsActive] = 1;

    PRINT N'Created index: [IX_ArticleLike_ArticlePublicId_Active]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_ArticleLike_ArticlePublicId_Active]';
END
GO

/* =========================================================
   4) [interaction].[Comment]
   ========================================================= */

/*
   Public query and visible-comment reconciliation:
   - V1 public comments are top-level only.
   - Query predicate is expected to include:
       ArticlePublicId = @ArticlePublicId
       Status = 'Visible'
       ParentCommentId IS NULL
   - This index also supports VisibleCommentCount reconciliation.
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_Comment_ArticlePublicId_Status_ParentCommentId_CreatedAtUtc'
      AND [object_id] = OBJECT_ID(N'[interaction].[Comment]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Comment_ArticlePublicId_Status_ParentCommentId_CreatedAtUtc]
    ON [interaction].[Comment]
    (
        [ArticlePublicId] ASC,
        [Status] ASC,
        [ParentCommentId] ASC,
        [CreatedAtUtc] DESC,
        [CommentId] DESC
    )
    INCLUDE
    (
        [PublicId],
        [AuthorUserId],
        [Content],
        [Version],
        [UpdatedAtUtc]
    );

    PRINT N'Created index: [IX_Comment_ArticlePublicId_Status_ParentCommentId_CreatedAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_Comment_ArticlePublicId_Status_ParentCommentId_CreatedAtUtc]';
END
GO

/* Admin comment moderation queue by status */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_Comment_Status_CreatedAtUtc'
      AND [object_id] = OBJECT_ID(N'[interaction].[Comment]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Comment_Status_CreatedAtUtc]
    ON [interaction].[Comment]
    (
        [Status] ASC,
        [CreatedAtUtc] DESC,
        [CommentId] DESC
    )
    INCLUDE
    (
        [PublicId],
        [ArticlePublicId],
        [AuthorUserId],
        [ParentCommentId],
        [Version],
        [UpdatedAtUtc],
        [DeletedAtUtc]
    );

    PRINT N'Created index: [IX_Comment_Status_CreatedAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_Comment_Status_CreatedAtUtc]';
END
GO

/* Admin investigation/filtering by comment author */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_Comment_AuthorUserId_CreatedAtUtc'
      AND [object_id] = OBJECT_ID(N'[interaction].[Comment]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Comment_AuthorUserId_CreatedAtUtc]
    ON [interaction].[Comment]
    (
        [AuthorUserId] ASC,
        [CreatedAtUtc] DESC,
        [CommentId] DESC
    )
    INCLUDE
    (
        [PublicId],
        [ArticlePublicId],
        [Status],
        [Version],
        [UpdatedAtUtc],
        [DeletedAtUtc]
    );

    PRINT N'Created index: [IX_Comment_AuthorUserId_CreatedAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_Comment_AuthorUserId_CreatedAtUtc]';
END
GO

/* =========================================================
   5) [interaction].[CommentReport]
   ========================================================= */

/*
   Note:
   - [UQ_CommentReport_CommentId_ReporterUserId] already enforces and
     supports one report per user/comment lookup.
*/

/* Case detail and pending-report resolution */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_CommentReport_CommentModerationCaseId_Status_CreatedAtUtc'
      AND [object_id] = OBJECT_ID(N'[interaction].[CommentReport]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CommentReport_CommentModerationCaseId_Status_CreatedAtUtc]
    ON [interaction].[CommentReport]
    (
        [CommentModerationCaseId] ASC,
        [Status] ASC,
        [CreatedAtUtc] ASC,
        [CommentReportId] ASC
    )
    INCLUDE
    (
        [PublicId],
        [CommentId],
        [ReporterUserId],
        [ReasonCode],
        [ResolvedAtUtc]
    );

    PRINT N'Created index: [IX_CommentReport_CommentModerationCaseId_Status_CreatedAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_CommentReport_CommentModerationCaseId_Status_CreatedAtUtc]';
END
GO

/* Report investigation by comment and report status */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_CommentReport_CommentId_Status_CreatedAtUtc'
      AND [object_id] = OBJECT_ID(N'[interaction].[CommentReport]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CommentReport_CommentId_Status_CreatedAtUtc]
    ON [interaction].[CommentReport]
    (
        [CommentId] ASC,
        [Status] ASC,
        [CreatedAtUtc] DESC,
        [CommentReportId] DESC
    )
    INCLUDE
    (
        [PublicId],
        [CommentModerationCaseId],
        [ReporterUserId],
        [ReasonCode],
        [ResolvedAtUtc]
    );

    PRINT N'Created index: [IX_CommentReport_CommentId_Status_CreatedAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_CommentReport_CommentId_Status_CreatedAtUtc]';
END
GO

/* =========================================================
   6) [interaction].[CommentModerationCase]
   ========================================================= */

/*
   Authoritative invariant:
   - A comment may have multiple historical closed cases.
   - A comment may have at most one current Open case.
   - This requires a SQL Server filtered unique index.
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'UX_CommentModerationCase_CommentId_Open'
      AND [object_id] = OBJECT_ID(N'[interaction].[CommentModerationCase]')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UX_CommentModerationCase_CommentId_Open]
    ON [interaction].[CommentModerationCase]
    (
        [CommentId] ASC
    )
    INCLUDE
    (
        [CommentModerationCaseId],
        [PublicId],
        [Priority],
        [HighestSeverity],
        [AlertTriggeredAtUtc],
        [OpenedAtUtc],
        [Version]
    )
    WHERE [Status] = N'Open';

    PRINT N'Created index: [UX_CommentModerationCase_CommentId_Open]';
END
ELSE
BEGIN
    PRINT N'Index exists: [UX_CommentModerationCase_CommentId_Open]';
END
GO

/* Case history/investigation by comment */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_CommentModerationCase_CommentId_OpenedAtUtc'
      AND [object_id] = OBJECT_ID(N'[interaction].[CommentModerationCase]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CommentModerationCase_CommentId_OpenedAtUtc]
    ON [interaction].[CommentModerationCase]
    (
        [CommentId] ASC,
        [OpenedAtUtc] DESC,
        [CommentModerationCaseId] DESC
    )
    INCLUDE
    (
        [PublicId],
        [Status],
        [Priority],
        [HighestSeverity],
        [AlertTriggeredAtUtc],
        [AlertLevel],
        [ResolvedAtUtc],
        [ResolutionType],
        [Version]
    );

    PRINT N'Created index: [IX_CommentModerationCase_CommentId_OpenedAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_CommentModerationCase_CommentId_OpenedAtUtc]';
END
GO

/* Admin open/resolved moderation-case queue */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_CommentModerationCase_Status_Priority_OpenedAtUtc'
      AND [object_id] = OBJECT_ID(N'[interaction].[CommentModerationCase]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CommentModerationCase_Status_Priority_OpenedAtUtc]
    ON [interaction].[CommentModerationCase]
    (
        [Status] ASC,
        [Priority] ASC,
        [OpenedAtUtc] DESC,
        [CommentModerationCaseId] DESC
    )
    INCLUDE
    (
        [PublicId],
        [CommentId],
        [HighestSeverity],
        [AlertTriggeredAtUtc],
        [AlertLevel],
        [ResolvedAtUtc],
        [Version]
    );

    PRINT N'Created index: [IX_CommentModerationCase_Status_Priority_OpenedAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_CommentModerationCase_Status_Priority_OpenedAtUtc]';
END
GO

/* Alert-triggered open-case operational lookup */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_CommentModerationCase_Open_AlertTriggeredAtUtc'
      AND [object_id] = OBJECT_ID(N'[interaction].[CommentModerationCase]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CommentModerationCase_Open_AlertTriggeredAtUtc]
    ON [interaction].[CommentModerationCase]
    (
        [AlertTriggeredAtUtc] DESC,
        [OpenedAtUtc] DESC,
        [CommentModerationCaseId] DESC
    )
    INCLUDE
    (
        [PublicId],
        [CommentId],
        [Priority],
        [HighestSeverity],
        [AlertLevel],
        [Version]
    )
    WHERE [Status] = N'Open'
      AND [AlertTriggeredAtUtc] IS NOT NULL;

    PRINT N'Created index: [IX_CommentModerationCase_Open_AlertTriggeredAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_CommentModerationCase_Open_AlertTriggeredAtUtc]';
END
GO

/* =========================================================
   7) [interaction].[CommentModerationActionHistory]
   ========================================================= */

/* Admin moderation history by comment */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_CommentModerationActionHistory_CommentId_OccurredAtUtc'
      AND [object_id] = OBJECT_ID(N'[interaction].[CommentModerationActionHistory]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CommentModerationActionHistory_CommentId_OccurredAtUtc]
    ON [interaction].[CommentModerationActionHistory]
    (
        [CommentId] ASC,
        [OccurredAtUtc] DESC,
        [CommentModerationActionHistoryId] DESC
    )
    INCLUDE
    (
        [PublicId],
        [CommentModerationCaseId],
        [ActionType],
        [FromStatus],
        [ToStatus],
        [ActorUserId],
        [ActorType],
        [ReasonCode],
        [CorrelationId]
    );

    PRINT N'Created index: [IX_CommentModerationActionHistory_CommentId_OccurredAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_CommentModerationActionHistory_CommentId_OccurredAtUtc]';
END
GO

/* Admin moderation history by case */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_CommentModerationActionHistory_CaseId_OccurredAtUtc'
      AND [object_id] = OBJECT_ID(N'[interaction].[CommentModerationActionHistory]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CommentModerationActionHistory_CaseId_OccurredAtUtc]
    ON [interaction].[CommentModerationActionHistory]
    (
        [CommentModerationCaseId] ASC,
        [OccurredAtUtc] DESC,
        [CommentModerationActionHistoryId] DESC
    )
    INCLUDE
    (
        [PublicId],
        [CommentId],
        [ActionType],
        [FromStatus],
        [ToStatus],
        [ActorUserId],
        [ActorType],
        [ReasonCode],
        [CorrelationId]
    )
    WHERE [CommentModerationCaseId] IS NOT NULL;

    PRINT N'Created index: [IX_CommentModerationActionHistory_CaseId_OccurredAtUtc]';
END
ELSE
BEGIN
    PRINT N'Index exists: [IX_CommentModerationActionHistory_CaseId_OccurredAtUtc]';
END
GO

/* =========================================================
   8) [interaction].[ArticleInteractionStats]
   ========================================================= */

/*
   Note:
   - [UQ_ArticleInteractionStats_ArticlePublicId] already supports:
       * stats materialization upsert/read by ArticlePublicId
       * admin stats lookup by ArticlePublicId
       * publication payload lookup by ArticlePublicId
   - Interaction V1 does not implement popularity/trending ranking.
   - No extra secondary index is intentionally added here.
*/
PRINT N'No additional secondary indexes required for [interaction].[ArticleInteractionStats] in V1.';
GO
