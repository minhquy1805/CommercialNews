/*
  File: db/10_modules/interaction/001_tables.sql
  Module: Interaction
  Purpose:
  - Create Interaction-owned truth, workflow and derived tables
    for CommercialNews V1 Async + Admin Moderation.
  - Truth / workflow state:
      * ArticleLike
      * Comment
      * CommentReport
      * CommentModerationCase
      * CommentModerationActionHistory
  - Derived / processing state:
      * ArticleInteractionTargetProjection
      * ArticleViewCount
      * ArticleInteractionStats
  - Support:
      * async article interaction eligibility from Content
      * durable materialized accepted-view counting
      * deterministic like/unlike truth
      * top-level comment moderation
      * comment report and moderation-case workflow
      * one-time admin alert intent metadata
      * versioned public counter snapshots for Reading
  - Idempotent for fresh/current schema: safe to re-run when tables already match this definition.

  Notes:
  - Interaction owns likes, comments, reports, moderation workflow and local derived state.
  - Interaction does NOT own Content publication truth, Reading serving truth,
    Notifications delivery truth or Audit evidence truth.
  - Cross-module references use logical identifiers:
      * ArticlePublicId -> Content-owned article public identity
      * UserId fields   -> Identity-owned user identity
    Physical foreign keys into Content/Identity are intentionally not created here.
  - V1 does NOT store raw ArticleViewEvent history.
  - V1 does NOT support comment editing or reply behavior.
  - Comment.ParentCommentId is nullable only for future compatibility;
    V1 command/procedure paths must create comments with ParentCommentId = NULL.
  - ArticleInteractionStats is derived state, not relationship/moderation truth.
  - Redis/cache is not durable Interaction truth and is not modeled here.
  - Shared Outbox infrastructure is not modeled as an Interaction-owned business table.
  - Filtered/secondary indexes belong in 010_indexes.sql.
  - Stored procedures belong in 020_procs.sql.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 58001, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

USE [CommercialNews];
GO

IF SCHEMA_ID(N'interaction') IS NULL
BEGIN
    THROW 58002, 'Schema [interaction] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

/* =========================================================
   1) [interaction].[ArticleInteractionTargetProjection]
   Derived eligibility projection consumed asynchronously
   from Content-owned article public/lifecycle state.
   ========================================================= */
IF OBJECT_ID(N'[interaction].[ArticleInteractionTargetProjection]', N'U') IS NULL
BEGIN
    CREATE TABLE [interaction].[ArticleInteractionTargetProjection]
    (
        [ArticleInteractionTargetProjectionId] BIGINT IDENTITY(1,1) NOT NULL,

        [ArticlePublicId]          CHAR(26)             NOT NULL,

        [SourceStatus]             NVARCHAR(30)         NOT NULL,
        [IsInteractionEnabled]     BIT                  NOT NULL
            CONSTRAINT [DF_ArticleInteractionTargetProjection_IsInteractionEnabled]
            DEFAULT (0),

        [LastSourceVersion]        BIGINT               NOT NULL
            CONSTRAINT [DF_ArticleInteractionTargetProjection_LastSourceVersion]
            DEFAULT (0),

        [LastSourceMessageId]      CHAR(26)             NULL,
        [LastSourceOccurredAtUtc]  DATETIME2(3)         NULL,

        [LastSyncedAtUtc]          DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_ArticleInteractionTargetProjection_LastSyncedAtUtc]
            DEFAULT (SYSUTCDATETIME()),

        [RequiresResync]           BIT                  NOT NULL
            CONSTRAINT [DF_ArticleInteractionTargetProjection_RequiresResync]
            DEFAULT (0),

        [CreatedAtUtc]             DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_ArticleInteractionTargetProjection_CreatedAtUtc]
            DEFAULT (SYSUTCDATETIME()),

        [UpdatedAtUtc]             DATETIME2(3)         NULL,

        CONSTRAINT [PK_ArticleInteractionTargetProjection]
            PRIMARY KEY CLUSTERED ([ArticleInteractionTargetProjectionId] ASC),

        CONSTRAINT [UQ_ArticleInteractionTargetProjection_ArticlePublicId]
            UNIQUE ([ArticlePublicId]),

        CONSTRAINT [CK_ArticleInteractionTargetProjection_ArticlePublicId_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([ArticlePublicId]))) > 0),

        CONSTRAINT [CK_ArticleInteractionTargetProjection_SourceStatus_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([SourceStatus]))) > 0),

        CONSTRAINT [CK_ArticleInteractionTargetProjection_LastSourceVersion]
            CHECK ([LastSourceVersion] >= 0),

        CONSTRAINT [CK_ArticleInteractionTargetProjection_LastSourceMessageId_NotBlank]
            CHECK (
                [LastSourceMessageId] IS NULL
                OR LEN(LTRIM(RTRIM([LastSourceMessageId]))) > 0
            ),

        CONSTRAINT [CK_ArticleInteractionTargetProjection_UpdatedAtUtc]
            CHECK (
                [UpdatedAtUtc] IS NULL
                OR [UpdatedAtUtc] >= [CreatedAtUtc]
            )
    );

    PRINT N'Created table: [interaction].[ArticleInteractionTargetProjection]';
END
ELSE
BEGIN
    PRINT N'Table exists: [interaction].[ArticleInteractionTargetProjection]';
END
GO

/* =========================================================
   2) [interaction].[ArticleViewCount]
   Durable materialized accepted-view counter.
   V1 does not retain raw per-view history.
   ========================================================= */
IF OBJECT_ID(N'[interaction].[ArticleViewCount]', N'U') IS NULL
BEGIN
    CREATE TABLE [interaction].[ArticleViewCount]
    (
        [ArticleViewCountId]        BIGINT IDENTITY(1,1) NOT NULL,

        [ArticlePublicId]           CHAR(26)             NOT NULL,

        [ViewCount]                 BIGINT               NOT NULL
            CONSTRAINT [DF_ArticleViewCount_ViewCount]
            DEFAULT (0),

        [ViewVersion]               BIGINT               NOT NULL
            CONSTRAINT [DF_ArticleViewCount_ViewVersion]
            DEFAULT (0),

        [LastAcceptedViewAtUtc]     DATETIME2(3)         NULL,

        [CreatedAtUtc]              DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_ArticleViewCount_CreatedAtUtc]
            DEFAULT (SYSUTCDATETIME()),

        [UpdatedAtUtc]              DATETIME2(3)         NULL,

        CONSTRAINT [PK_ArticleViewCount]
            PRIMARY KEY CLUSTERED ([ArticleViewCountId] ASC),

        CONSTRAINT [UQ_ArticleViewCount_ArticlePublicId]
            UNIQUE ([ArticlePublicId]),

        CONSTRAINT [CK_ArticleViewCount_ArticlePublicId_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([ArticlePublicId]))) > 0),

        CONSTRAINT [CK_ArticleViewCount_ViewCount]
            CHECK ([ViewCount] >= 0),

        CONSTRAINT [CK_ArticleViewCount_ViewVersion]
            CHECK ([ViewVersion] >= 0),

        CONSTRAINT [CK_ArticleViewCount_UpdatedAtUtc]
            CHECK (
                [UpdatedAtUtc] IS NULL
                OR [UpdatedAtUtc] >= [CreatedAtUtc]
            ),

        CONSTRAINT [CK_ArticleViewCount_LastAcceptedViewAtUtc]
            CHECK (
                [LastAcceptedViewAtUtc] IS NULL
                OR [LastAcceptedViewAtUtc] >= [CreatedAtUtc]
            )
    );

    PRINT N'Created table: [interaction].[ArticleViewCount]';
END
ELSE
BEGIN
    PRINT N'Table exists: [interaction].[ArticleViewCount]';
END
GO

/* =========================================================
   3) [interaction].[ArticleLike]
   Authoritative like/unlike relationship truth.
   V1 keeps one reusable row per (ArticlePublicId, UserId)
   and toggles IsActive.
   ========================================================= */
IF OBJECT_ID(N'[interaction].[ArticleLike]', N'U') IS NULL
BEGIN
    CREATE TABLE [interaction].[ArticleLike]
    (
        [ArticleLikeId]             BIGINT IDENTITY(1,1) NOT NULL,

        [PublicId]                  CHAR(26)             NOT NULL,
        [ArticlePublicId]           CHAR(26)             NOT NULL,
        [UserId]                    BIGINT               NOT NULL,

        [IsActive]                  BIT                  NOT NULL
            CONSTRAINT [DF_ArticleLike_IsActive]
            DEFAULT (1),

        [LikedAtUtc]                DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_ArticleLike_LikedAtUtc]
            DEFAULT (SYSUTCDATETIME()),

        [UnlikedAtUtc]              DATETIME2(3)         NULL,

        [Version]                   BIGINT               NOT NULL
            CONSTRAINT [DF_ArticleLike_Version]
            DEFAULT (1),

        [CreatedAtUtc]              DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_ArticleLike_CreatedAtUtc]
            DEFAULT (SYSUTCDATETIME()),

        [UpdatedAtUtc]              DATETIME2(3)         NULL,

        CONSTRAINT [PK_ArticleLike]
            PRIMARY KEY CLUSTERED ([ArticleLikeId] ASC),

        CONSTRAINT [UQ_ArticleLike_PublicId]
            UNIQUE ([PublicId]),

        CONSTRAINT [UQ_ArticleLike_ArticlePublicId_UserId]
            UNIQUE ([ArticlePublicId], [UserId]),

        CONSTRAINT [CK_ArticleLike_PublicId_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([PublicId]))) > 0),

        CONSTRAINT [CK_ArticleLike_ArticlePublicId_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([ArticlePublicId]))) > 0),

        CONSTRAINT [CK_ArticleLike_UserId]
            CHECK ([UserId] > 0),

        CONSTRAINT [CK_ArticleLike_Version]
            CHECK ([Version] >= 1),

        CONSTRAINT [CK_ArticleLike_UpdatedAtUtc]
            CHECK (
                [UpdatedAtUtc] IS NULL
                OR [UpdatedAtUtc] >= [CreatedAtUtc]
            ),

        CONSTRAINT [CK_ArticleLike_UnlikedAtUtc]
            CHECK (
                [UnlikedAtUtc] IS NULL
                OR [UnlikedAtUtc] >= [LikedAtUtc]
            ),

        CONSTRAINT [CK_ArticleLike_State]
            CHECK (
                ([IsActive] = 1 AND [UnlikedAtUtc] IS NULL)
                OR
                ([IsActive] = 0 AND [UnlikedAtUtc] IS NOT NULL)
            )
    );

    PRINT N'Created table: [interaction].[ArticleLike]';
END
ELSE
BEGIN
    PRINT N'Table exists: [interaction].[ArticleLike]';
END
GO

/* =========================================================
   4) [interaction].[Comment]
   Authoritative comment content and current moderation state.
   V1 supports top-level comments only.
   ========================================================= */
IF OBJECT_ID(N'[interaction].[Comment]', N'U') IS NULL
BEGIN
    CREATE TABLE [interaction].[Comment]
    (
        [CommentId]                 BIGINT IDENTITY(1,1) NOT NULL,

        [PublicId]                  CHAR(26)             NOT NULL,
        [ArticlePublicId]           CHAR(26)             NOT NULL,
        [AuthorUserId]              BIGINT               NOT NULL,

        [ParentCommentId]           BIGINT               NULL,

        [Content]                   NVARCHAR(2000)       NOT NULL,

        [Status]                    NVARCHAR(20)         NOT NULL
            CONSTRAINT [DF_Comment_Status]
            DEFAULT (N'Visible'),

        [Version]                   BIGINT               NOT NULL
            CONSTRAINT [DF_Comment_Version]
            DEFAULT (1),

        [CreatedAtUtc]              DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_Comment_CreatedAtUtc]
            DEFAULT (SYSUTCDATETIME()),

        [UpdatedAtUtc]              DATETIME2(3)         NULL,
        [DeletedAtUtc]              DATETIME2(3)         NULL,

        CONSTRAINT [PK_Comment]
            PRIMARY KEY CLUSTERED ([CommentId] ASC),

        CONSTRAINT [UQ_Comment_PublicId]
            UNIQUE ([PublicId]),

        CONSTRAINT [FK_Comment_ParentComment]
            FOREIGN KEY ([ParentCommentId])
            REFERENCES [interaction].[Comment]([CommentId]),

        CONSTRAINT [CK_Comment_ParentCommentId_V1TopLevelOnly]
            CHECK ([ParentCommentId] IS NULL),

        CONSTRAINT [CK_Comment_PublicId_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([PublicId]))) > 0),

        CONSTRAINT [CK_Comment_ArticlePublicId_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([ArticlePublicId]))) > 0),

        CONSTRAINT [CK_Comment_AuthorUserId]
            CHECK ([AuthorUserId] > 0),

        CONSTRAINT [CK_Comment_Content_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([Content]))) > 0),

        CONSTRAINT [CK_Comment_Status]
            CHECK (
                [Status] IN (
                    N'Pending',
                    N'Visible',
                    N'Rejected',
                    N'Hidden',
                    N'Deleted'
                )
            ),

        CONSTRAINT [CK_Comment_Version]
            CHECK ([Version] >= 1),

        CONSTRAINT [CK_Comment_UpdatedAtUtc]
            CHECK (
                [UpdatedAtUtc] IS NULL
                OR [UpdatedAtUtc] >= [CreatedAtUtc]
            ),

        CONSTRAINT [CK_Comment_DeletedAtUtc]
            CHECK (
                [DeletedAtUtc] IS NULL
                OR [DeletedAtUtc] >= [CreatedAtUtc]
            ),

        CONSTRAINT [CK_Comment_DeletedState]
            CHECK (
                ([Status] = N'Deleted' AND [DeletedAtUtc] IS NOT NULL)
                OR
                ([Status] <> N'Deleted' AND [DeletedAtUtc] IS NULL)
            )
    );

    PRINT N'Created table: [interaction].[Comment]';
END
ELSE
BEGIN
    PRINT N'Table exists: [interaction].[Comment]';
END
GO

/*
  V1 uses post-moderation by default. Valid newly-created comments are
  visible immediately; Pending remains reserved for future selective moderation.
*/
IF OBJECT_ID(N'[interaction].[DF_Comment_Status]', N'D') IS NOT NULL
BEGIN
    ALTER TABLE [interaction].[Comment]
        DROP CONSTRAINT [DF_Comment_Status];

    PRINT N'Dropped default constraint for rebuild: [DF_Comment_Status]';
END
GO

IF OBJECT_ID(N'[interaction].[DF_Comment_Status]', N'D') IS NULL
BEGIN
    ALTER TABLE [interaction].[Comment]
        ADD CONSTRAINT [DF_Comment_Status]
            DEFAULT (N'Visible') FOR [Status];

    PRINT N'Created default constraint: [DF_Comment_Status]';
END
GO

/* =========================================================
   5) [interaction].[CommentModerationCase]
   One report-review cycle for a comment.
   One Open case per Comment is enforced in 010_indexes.sql
   through a filtered unique index.
   ========================================================= */
IF OBJECT_ID(N'[interaction].[CommentModerationCase]', N'U') IS NULL
BEGIN
    CREATE TABLE [interaction].[CommentModerationCase]
    (
        [CommentModerationCaseId]   BIGINT IDENTITY(1,1) NOT NULL,

        [PublicId]                  CHAR(26)             NOT NULL,
        [CommentId]                 BIGINT               NOT NULL,

        [Status]                    NVARCHAR(30)         NOT NULL
            CONSTRAINT [DF_CommentModerationCase_Status]
            DEFAULT (N'Open'),

        [Priority]                  NVARCHAR(20)         NOT NULL
            CONSTRAINT [DF_CommentModerationCase_Priority]
            DEFAULT (N'Normal'),

        [HighestSeverity]           NVARCHAR(20)         NOT NULL
            CONSTRAINT [DF_CommentModerationCase_HighestSeverity]
            DEFAULT (N'Normal'),

        [AlertTriggeredAtUtc]       DATETIME2(3)         NULL,
        [AlertLevel]                NVARCHAR(20)         NULL,
        [AlertMessageId]            CHAR(26)             NULL,

        [OpenedAtUtc]               DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_CommentModerationCase_OpenedAtUtc]
            DEFAULT (SYSUTCDATETIME()),

        [ResolvedAtUtc]             DATETIME2(3)         NULL,
        [ResolvedByUserId]          BIGINT               NULL,

        [ResolutionType]            NVARCHAR(40)         NULL,
        [ResolutionReasonCode]      NVARCHAR(40)         NULL,
        [ResolutionNote]            NVARCHAR(1000)       NULL,

        [Version]                   BIGINT               NOT NULL
            CONSTRAINT [DF_CommentModerationCase_Version]
            DEFAULT (1),

        CONSTRAINT [PK_CommentModerationCase]
            PRIMARY KEY CLUSTERED ([CommentModerationCaseId] ASC),

        CONSTRAINT [UQ_CommentModerationCase_PublicId]
            UNIQUE ([PublicId]),

        CONSTRAINT [UQ_CommentModerationCase_CommentModerationCaseId_CommentId]
            UNIQUE ([CommentModerationCaseId], [CommentId]),

        CONSTRAINT [FK_CommentModerationCase_Comment]
            FOREIGN KEY ([CommentId])
            REFERENCES [interaction].[Comment]([CommentId]),

        CONSTRAINT [CK_CommentModerationCase_PublicId_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([PublicId]))) > 0),

        CONSTRAINT [CK_CommentModerationCase_Status]
            CHECK (
                [Status] IN (
                    N'Open',
                    N'Dismissed',
                    N'Actioned',
                    N'ClosedByAuthorDeletion'
                )
            ),

        CONSTRAINT [CK_CommentModerationCase_Priority]
            CHECK (
                [Priority] IN (
                    N'Normal',
                    N'High',
                    N'Critical'
                )
            ),

        CONSTRAINT [CK_CommentModerationCase_HighestSeverity]
            CHECK (
                [HighestSeverity] IN (
                    N'Normal',
                    N'High',
                    N'Critical'
                )
            ),

        CONSTRAINT [CK_CommentModerationCase_AlertLevel]
            CHECK (
                [AlertLevel] IS NULL
                OR [AlertLevel] IN (N'High', N'Critical')
            ),

        CONSTRAINT [CK_CommentModerationCase_AlertState]
            CHECK (
                (
                    [AlertTriggeredAtUtc] IS NULL
                    AND [AlertLevel] IS NULL
                    AND [AlertMessageId] IS NULL
                )
                OR
                (
                    [AlertTriggeredAtUtc] IS NOT NULL
                    AND [AlertLevel] IS NOT NULL
                    AND [AlertMessageId] IS NOT NULL
                )
            ),

        CONSTRAINT [CK_CommentModerationCase_ResolutionType]
            CHECK (
                [ResolutionType] IS NULL
                OR [ResolutionType] IN (
                    N'DismissReportedCase',
                    N'HideReportedComment',
                    N'CloseCaseByAuthorDeletion'
                )
            ),

        CONSTRAINT [CK_CommentModerationCase_ResolutionReasonCode]
            CHECK (
                [ResolutionReasonCode] IS NULL
                OR [ResolutionReasonCode] IN (
                    N'Spam',
                    N'Harassment',
                    N'HateSpeech',
                    N'Violence',
                    N'SexualContent',
                    N'PersonalInformation',
                    N'Misinformation',
                    N'OffTopic',
                    N'PolicyViolation',
                    N'Other'
                )
            ),

        CONSTRAINT [CK_CommentModerationCase_ReasonRequiredByResolutionType]
            CHECK (
                (
                    [ResolutionType] IS NULL
                    AND [ResolutionReasonCode] IS NULL
                    AND [ResolutionNote] IS NULL
                )
                OR
                (
                    [ResolutionType] IN (
                        N'DismissReportedCase',
                        N'HideReportedComment'
                    )
                    AND [ResolutionReasonCode] IS NOT NULL
                )
                OR
                (
                    [ResolutionType] = N'CloseCaseByAuthorDeletion'
                    AND [ResolutionReasonCode] IS NULL
                    AND [ResolutionNote] IS NULL
                )
            ),

        CONSTRAINT [CK_CommentModerationCase_ResolutionState]
            CHECK (
                (
                    [Status] = N'Open'
                    AND [ResolvedAtUtc] IS NULL
                    AND [ResolutionType] IS NULL
                    AND [ResolvedByUserId] IS NULL
                )
                OR
                (
                    [Status] = N'Dismissed'
                    AND [ResolvedAtUtc] IS NOT NULL
                    AND [ResolutionType] = N'DismissReportedCase'
                    AND [ResolvedByUserId] IS NOT NULL
                )
                OR
                (
                    [Status] = N'Actioned'
                    AND [ResolvedAtUtc] IS NOT NULL
                    AND [ResolutionType] = N'HideReportedComment'
                    AND [ResolvedByUserId] IS NOT NULL
                )
                OR
                (
                    [Status] = N'ClosedByAuthorDeletion'
                    AND [ResolvedAtUtc] IS NOT NULL
                    AND [ResolutionType] = N'CloseCaseByAuthorDeletion'
                    AND [ResolvedByUserId] IS NOT NULL
                )
            ),

        CONSTRAINT [CK_CommentModerationCase_ResolutionNote_NotBlank]
            CHECK (
                [ResolutionNote] IS NULL
                OR LEN(LTRIM(RTRIM([ResolutionNote]))) > 0
            ),

        CONSTRAINT [CK_CommentModerationCase_OtherResolutionNoteRequired]
            CHECK (
                [ResolutionReasonCode] <> N'Other'
                OR (
                    [ResolutionNote] IS NOT NULL
                    AND LEN(LTRIM(RTRIM([ResolutionNote]))) > 0
                )
            ),

        CONSTRAINT [CK_CommentModerationCase_ResolvedAtUtc]
            CHECK (
                [ResolvedAtUtc] IS NULL
                OR [ResolvedAtUtc] >= [OpenedAtUtc]
            ),

        CONSTRAINT [CK_CommentModerationCase_AlertTriggeredAtUtc]
            CHECK (
                [AlertTriggeredAtUtc] IS NULL
                OR [AlertTriggeredAtUtc] >= [OpenedAtUtc]
            ),

        CONSTRAINT [CK_CommentModerationCase_Version]
            CHECK ([Version] >= 1)
    );

    PRINT N'Created table: [interaction].[CommentModerationCase]';
END
ELSE
BEGIN
    PRINT N'Table exists: [interaction].[CommentModerationCase]';
END
GO

/* =========================================================
   6) [interaction].[CommentReport]
   User-submitted allegation against a visible comment.
   Does not automatically hide content.
   ========================================================= */
IF OBJECT_ID(N'[interaction].[CommentReport]', N'U') IS NULL
BEGIN
    CREATE TABLE [interaction].[CommentReport]
    (
        [CommentReportId]           BIGINT IDENTITY(1,1) NOT NULL,

        [PublicId]                  CHAR(26)             NOT NULL,

        [CommentId]                 BIGINT               NOT NULL,
        [CommentModerationCaseId]   BIGINT               NOT NULL,

        [ReporterUserId]            BIGINT               NOT NULL,

        [ReasonCode]                NVARCHAR(40)         NOT NULL,
        [Description]               NVARCHAR(1000)       NULL,

        [Status]                    NVARCHAR(30)         NOT NULL
            CONSTRAINT [DF_CommentReport_Status]
            DEFAULT (N'Pending'),

        [CreatedAtUtc]              DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_CommentReport_CreatedAtUtc]
            DEFAULT (SYSUTCDATETIME()),

        [ResolvedAtUtc]             DATETIME2(3)         NULL,

        CONSTRAINT [PK_CommentReport]
            PRIMARY KEY CLUSTERED ([CommentReportId] ASC),

        CONSTRAINT [UQ_CommentReport_PublicId]
            UNIQUE ([PublicId]),

        CONSTRAINT [UQ_CommentReport_CommentId_ReporterUserId]
            UNIQUE ([CommentId], [ReporterUserId]),

        CONSTRAINT [FK_CommentReport_Comment]
            FOREIGN KEY ([CommentId])
            REFERENCES [interaction].[Comment]([CommentId]),

        CONSTRAINT [FK_CommentReport_CommentModerationCase_Comment]
            FOREIGN KEY ([CommentModerationCaseId], [CommentId])
            REFERENCES [interaction].[CommentModerationCase]
            (
                [CommentModerationCaseId],
                [CommentId]
            ),

        CONSTRAINT [CK_CommentReport_PublicId_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([PublicId]))) > 0),

        CONSTRAINT [CK_CommentReport_ReporterUserId]
            CHECK ([ReporterUserId] > 0),

        CONSTRAINT [CK_CommentReport_ReasonCode]
            CHECK (
                [ReasonCode] IN (
                    N'Spam',
                    N'Harassment',
                    N'HateSpeech',
                    N'Violence',
                    N'SexualContent',
                    N'PersonalInformation',
                    N'Misinformation',
                    N'OffTopic',
                    N'Other'
                )
            ),

        CONSTRAINT [CK_CommentReport_Description_NotBlank]
            CHECK (
                [Description] IS NULL
                OR LEN(LTRIM(RTRIM([Description]))) > 0
            ),

        CONSTRAINT [CK_CommentReport_OtherDescriptionRequired]
            CHECK (
                [ReasonCode] <> N'Other'
                OR (
                    [Description] IS NOT NULL
                    AND LEN(LTRIM(RTRIM([Description]))) > 0
                )
            ),

        CONSTRAINT [CK_CommentReport_Status]
            CHECK (
                [Status] IN (
                    N'Pending',
                    N'Dismissed',
                    N'Actioned',
                    N'ClosedByAuthorDeletion'
                )
            ),

        CONSTRAINT [CK_CommentReport_ResolutionState]
            CHECK (
                ([Status] = N'Pending' AND [ResolvedAtUtc] IS NULL)
                OR
                ([Status] <> N'Pending' AND [ResolvedAtUtc] IS NOT NULL)
            ),

        CONSTRAINT [CK_CommentReport_ResolvedAtUtc]
            CHECK (
                [ResolvedAtUtc] IS NULL
                OR [ResolvedAtUtc] >= [CreatedAtUtc]
            )
    );

    PRINT N'Created table: [interaction].[CommentReport]';
END
ELSE
BEGIN
    PRINT N'Table exists: [interaction].[CommentReport]';
END
GO

/* =========================================================
   7) [interaction].[CommentModerationActionHistory]
   Local admin operational moderation history.
   Audit canonical evidence is handled asynchronously.
   ========================================================= */
IF OBJECT_ID(N'[interaction].[CommentModerationActionHistory]', N'U') IS NULL
BEGIN
    CREATE TABLE [interaction].[CommentModerationActionHistory]
    (
        [CommentModerationActionHistoryId] BIGINT IDENTITY(1,1) NOT NULL,

        [PublicId]                  CHAR(26)             NOT NULL,

        [CommentId]                 BIGINT               NOT NULL,
        [CommentModerationCaseId]   BIGINT               NULL,

        [ActionType]                NVARCHAR(40)         NOT NULL,

        [FromStatus]                NVARCHAR(30)         NULL,
        [ToStatus]                  NVARCHAR(30)         NULL,

        [ActorUserId]               BIGINT               NULL,
        [ActorType]                 NVARCHAR(30)         NOT NULL,

        [ReasonCode]                NVARCHAR(40)         NULL,
        [Note]                      NVARCHAR(1000)       NULL,

        [OccurredAtUtc]             DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_CommentModerationActionHistory_OccurredAtUtc]
            DEFAULT (SYSUTCDATETIME()),

        [CorrelationId]             CHAR(26)             NULL,

        CONSTRAINT [PK_CommentModerationActionHistory]
            PRIMARY KEY CLUSTERED ([CommentModerationActionHistoryId] ASC),

        CONSTRAINT [UQ_CommentModerationActionHistory_PublicId]
            UNIQUE ([PublicId]),

        CONSTRAINT [FK_CommentModerationActionHistory_Comment]
            FOREIGN KEY ([CommentId])
            REFERENCES [interaction].[Comment]([CommentId]),

        CONSTRAINT [FK_CommentModerationActionHistory_CommentModerationCase_Comment]
            FOREIGN KEY ([CommentModerationCaseId], [CommentId])
            REFERENCES [interaction].[CommentModerationCase]
            (
                [CommentModerationCaseId],
                [CommentId]
            ),

        CONSTRAINT [CK_CommentModerationActionHistory_PublicId_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([PublicId]))) > 0),

        CONSTRAINT [CK_CommentModerationActionHistory_ActionType]
            CHECK (
                [ActionType] IN (
                    N'Approve',
                    N'Reject',
                    N'Hide',
                    N'Restore',
                    N'DismissReportedCase',
                    N'HideReportedComment',
                    N'CloseCaseByAuthorDeletion'
                )
            ),

        CONSTRAINT [CK_CommentModerationActionHistory_CaseRequiredByActionType]
            CHECK (
                (
                    [ActionType] IN (
                        N'DismissReportedCase',
                        N'HideReportedComment',
                        N'CloseCaseByAuthorDeletion'
                    )
                    AND [CommentModerationCaseId] IS NOT NULL
                )
                OR
                (
                    [ActionType] IN (
                        N'Approve',
                        N'Reject',
                        N'Hide',
                        N'Restore'
                    )
                    AND [CommentModerationCaseId] IS NULL
                )
            ),

        CONSTRAINT [CK_CommentModerationActionHistory_FromStatus]
            CHECK (
                [FromStatus] IS NULL
                OR [FromStatus] IN (
                    N'Pending',
                    N'Visible',
                    N'Rejected',
                    N'Hidden',
                    N'Deleted'
                )
            ),

        CONSTRAINT [CK_CommentModerationActionHistory_ToStatus]
            CHECK (
                [ToStatus] IS NULL
                OR [ToStatus] IN (
                    N'Pending',
                    N'Visible',
                    N'Rejected',
                    N'Hidden',
                    N'Deleted'
                )
            ),

        CONSTRAINT [CK_CommentModerationActionHistory_ActorType_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([ActorType]))) > 0),

        CONSTRAINT [CK_CommentModerationActionHistory_ReasonCode]
            CHECK (
                [ReasonCode] IS NULL
                OR [ReasonCode] IN (
                    N'Spam',
                    N'Harassment',
                    N'HateSpeech',
                    N'Violence',
                    N'SexualContent',
                    N'PersonalInformation',
                    N'Misinformation',
                    N'OffTopic',
                    N'PolicyViolation',
                    N'Other'
                )
            ),

        CONSTRAINT [CK_CommentModerationActionHistory_ReasonRequiredByActionType]
            CHECK (
                (
                    [ActionType] IN (
                        N'Reject',
                        N'Hide',
                        N'DismissReportedCase',
                        N'HideReportedComment'
                    )
                    AND [ReasonCode] IS NOT NULL
                )
                OR
                (
                    [ActionType] IN (
                        N'Approve',
                        N'Restore'
                    )
                )
                OR
                (
                    [ActionType] = N'CloseCaseByAuthorDeletion'
                    AND [ReasonCode] IS NULL
                    AND [Note] IS NULL
                )
            ),

        CONSTRAINT [CK_CommentModerationActionHistory_Note_NotBlank]
            CHECK (
                [Note] IS NULL
                OR LEN(LTRIM(RTRIM([Note]))) > 0
            ),

        CONSTRAINT [CK_CommentModerationActionHistory_OtherNoteRequired]
            CHECK (
                [ReasonCode] IS NULL
                OR [ReasonCode] <> N'Other'
                OR (
                    [Note] IS NOT NULL
                    AND LEN(LTRIM(RTRIM([Note]))) > 0
                )
            ),

        CONSTRAINT [CK_CommentModerationActionHistory_CorrelationId_NotBlank]
            CHECK (
                [CorrelationId] IS NULL
                OR LEN(LTRIM(RTRIM([CorrelationId]))) > 0
            )
    );

    PRINT N'Created table: [interaction].[CommentModerationActionHistory]';
END
ELSE
BEGIN
    PRINT N'Table exists: [interaction].[CommentModerationActionHistory]';
END
GO

/* =========================================================
   8) [interaction].[ArticleInteractionStats]
   Derived public counter snapshot published asynchronously
   to Reading by StatsVersion.
   ========================================================= */
IF OBJECT_ID(N'[interaction].[ArticleInteractionStats]', N'U') IS NULL
BEGIN
    CREATE TABLE [interaction].[ArticleInteractionStats]
    (
        [ArticleInteractionStatsId] BIGINT IDENTITY(1,1) NOT NULL,

        [ArticlePublicId]           CHAR(26)             NOT NULL,

        [ViewCount]                 BIGINT               NOT NULL
            CONSTRAINT [DF_ArticleInteractionStats_ViewCount]
            DEFAULT (0),

        [LikeCount]                 BIGINT               NOT NULL
            CONSTRAINT [DF_ArticleInteractionStats_LikeCount]
            DEFAULT (0),

        [VisibleCommentCount]       BIGINT               NOT NULL
            CONSTRAINT [DF_ArticleInteractionStats_VisibleCommentCount]
            DEFAULT (0),

        [StatsVersion]              BIGINT               NOT NULL
            CONSTRAINT [DF_ArticleInteractionStats_StatsVersion]
            DEFAULT (0),

        [LastMaterializedAtUtc]     DATETIME2(3)         NULL,
        [LastPublishedMessageId]    CHAR(26)             NULL,
        [LastPublishedAtUtc]        DATETIME2(3)         NULL,

        [CreatedAtUtc]              DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_ArticleInteractionStats_CreatedAtUtc]
            DEFAULT (SYSUTCDATETIME()),

        [UpdatedAtUtc]              DATETIME2(3)         NULL,

        CONSTRAINT [PK_ArticleInteractionStats]
            PRIMARY KEY CLUSTERED ([ArticleInteractionStatsId] ASC),

        CONSTRAINT [UQ_ArticleInteractionStats_ArticlePublicId]
            UNIQUE ([ArticlePublicId]),

        CONSTRAINT [CK_ArticleInteractionStats_ArticlePublicId_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([ArticlePublicId]))) > 0),

        CONSTRAINT [CK_ArticleInteractionStats_ViewCount]
            CHECK ([ViewCount] >= 0),

        CONSTRAINT [CK_ArticleInteractionStats_LikeCount]
            CHECK ([LikeCount] >= 0),

        CONSTRAINT [CK_ArticleInteractionStats_VisibleCommentCount]
            CHECK ([VisibleCommentCount] >= 0),

        CONSTRAINT [CK_ArticleInteractionStats_StatsVersion]
            CHECK ([StatsVersion] >= 0),

        CONSTRAINT [CK_ArticleInteractionStats_LastPublishedMessageId_NotBlank]
            CHECK (
                [LastPublishedMessageId] IS NULL
                OR LEN(LTRIM(RTRIM([LastPublishedMessageId]))) > 0
            ),

        CONSTRAINT [CK_ArticleInteractionStats_UpdatedAtUtc]
            CHECK (
                [UpdatedAtUtc] IS NULL
                OR [UpdatedAtUtc] >= [CreatedAtUtc]
            ),

        CONSTRAINT [CK_ArticleInteractionStats_LastMaterializedAtUtc]
            CHECK (
                [LastMaterializedAtUtc] IS NULL
                OR [LastMaterializedAtUtc] >= [CreatedAtUtc]
            ),

        CONSTRAINT [CK_ArticleInteractionStats_LastPublishedAtUtc]
            CHECK (
                [LastPublishedAtUtc] IS NULL
                OR [LastPublishedAtUtc] >= [CreatedAtUtc]
            ),

        CONSTRAINT [CK_ArticleInteractionStats_PublicationState]
            CHECK (
                (
                    [LastPublishedMessageId] IS NULL
                    AND [LastPublishedAtUtc] IS NULL
                )
                OR
                (
                    [LastPublishedMessageId] IS NOT NULL
                    AND [LastPublishedAtUtc] IS NOT NULL
                )
            )
    );

    PRINT N'Created table: [interaction].[ArticleInteractionStats]';
END
ELSE
BEGIN
    PRINT N'Table exists: [interaction].[ArticleInteractionStats]';
END
GO
