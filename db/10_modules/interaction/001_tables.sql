/*
  File: db/10_modules/interaction/001_tables.sql
  Module: Interaction
  Purpose:
  - Create Interaction truth and derived tables for CommercialNews V1.
  - Core truth model:
      * ArticleViewEvent
      * ArticleLike
      * Comment
  - Derived support:
      * ArticleInteractionStats
  - Support:
      * non-blocking view signal persistence
      * deterministic like/unlike truth
      * comment lifecycle truth
      * future async aggregation / replay / rebuild readiness
      * later Reading enrichment via Interaction stats projection
  - Idempotent: safe to re-run.

  Notes:
  - Interaction owns likes/comments/raw interaction events truth.
  - Interaction does NOT own Content publication truth or Reading response truth.
  - ArticleInteractionStats is derived state, not correctness truth.
  - Redis / outbox / broker / worker / projections are NOT modeled here in 001_tables.sql.
  - Index-heavy tuning belongs in 010_indexes.sql.
  - Stored procedures belong in 020_procs.sql.
*/

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

IF SCHEMA_ID(N'content') IS NULL
BEGIN
    THROW 58003, 'Schema [content] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

IF SCHEMA_ID(N'identity') IS NULL
BEGIN
    THROW 58004, 'Schema [identity] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

IF OBJECT_ID(N'[content].[Article]', N'U') IS NULL
BEGIN
    THROW 58005, 'Table [content].[Article] does not exist. Run content/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[identity].[UserAccount]', N'U') IS NULL
BEGIN
    THROW 58006, 'Table [identity].[UserAccount] does not exist. Run identity/001_tables.sql first.', 1;
END
GO

/* =========================================================
   1) [interaction].[ArticleViewEvent]
   ========================================================= */
IF OBJECT_ID(N'[interaction].[ArticleViewEvent]', N'U') IS NULL
BEGIN
    CREATE TABLE [interaction].[ArticleViewEvent]
    (
        [ArticleViewEventId]    BIGINT IDENTITY(1,1) NOT NULL,

        [ArticleId]             BIGINT               NOT NULL,
        [UserId]                BIGINT               NULL,
        [VisitorKey]            NVARCHAR(100)        NULL,

        [ViewedAt]              DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_ArticleViewEvent_ViewedAt] DEFAULT (SYSUTCDATETIME()),

        [IpAddress]             NVARCHAR(64)         NULL,
        [UserAgent]             NVARCHAR(512)        NULL,

        CONSTRAINT [PK_ArticleViewEvent]
            PRIMARY KEY CLUSTERED ([ArticleViewEventId] ASC),

        CONSTRAINT [FK_ArticleViewEvent_Article]
            FOREIGN KEY ([ArticleId])
            REFERENCES [content].[Article]([ArticleId]),

        CONSTRAINT [FK_ArticleViewEvent_User]
            FOREIGN KEY ([UserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [CK_ArticleViewEvent_VisitorKey_NotBlank]
            CHECK ([VisitorKey] IS NULL OR LEN(LTRIM(RTRIM([VisitorKey]))) > 0),

        CONSTRAINT [CK_ArticleViewEvent_IpAddress_NotBlank]
            CHECK ([IpAddress] IS NULL OR LEN(LTRIM(RTRIM([IpAddress]))) > 0),

        CONSTRAINT [CK_ArticleViewEvent_UserAgent_NotBlank]
            CHECK ([UserAgent] IS NULL OR LEN(LTRIM(RTRIM([UserAgent]))) > 0)
    );

    PRINT N'Created table: [interaction].[ArticleViewEvent]';
END
ELSE
BEGIN
    PRINT N'Table exists: [interaction].[ArticleViewEvent]';
END
GO

/* =========================================================
   2) [interaction].[ArticleLike]
   ========================================================= */
IF OBJECT_ID(N'[interaction].[ArticleLike]', N'U') IS NULL
BEGIN
    CREATE TABLE [interaction].[ArticleLike]
    (
        [ArticleLikeId]         BIGINT IDENTITY(1,1) NOT NULL,

        [ArticleId]             BIGINT               NOT NULL,
        [UserId]                BIGINT               NOT NULL,

        [IsActive]              BIT                  NOT NULL
            CONSTRAINT [DF_ArticleLike_IsActive] DEFAULT (1),

        [LikedAt]               DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_ArticleLike_LikedAt] DEFAULT (SYSUTCDATETIME()),
        [UnlikedAt]             DATETIME2(3)         NULL,

        [CreatedAt]             DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_ArticleLike_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt]             DATETIME2(3)         NULL,

        CONSTRAINT [PK_ArticleLike]
            PRIMARY KEY CLUSTERED ([ArticleLikeId] ASC),

        CONSTRAINT [UQ_ArticleLike_ArticleId_UserId]
            UNIQUE ([ArticleId], [UserId]),

        CONSTRAINT [FK_ArticleLike_Article]
            FOREIGN KEY ([ArticleId])
            REFERENCES [content].[Article]([ArticleId]),

        CONSTRAINT [FK_ArticleLike_User]
            FOREIGN KEY ([UserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [CK_ArticleLike_UpdatedAt]
            CHECK ([UpdatedAt] IS NULL OR [UpdatedAt] >= [CreatedAt]),

        CONSTRAINT [CK_ArticleLike_UnlikedAt]
            CHECK ([UnlikedAt] IS NULL OR [UnlikedAt] >= [LikedAt]),

        CONSTRAINT [CK_ArticleLike_State]
            CHECK (
                ([IsActive] = 1 AND [UnlikedAt] IS NULL)
                OR
                ([IsActive] = 0)
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
   3) [interaction].[Comment]
   ========================================================= */
IF OBJECT_ID(N'[interaction].[Comment]', N'U') IS NULL
BEGIN
    CREATE TABLE [interaction].[Comment]
    (
        [CommentId]             BIGINT IDENTITY(1,1) NOT NULL,

        [ArticleId]             BIGINT               NOT NULL,
        [UserId]                BIGINT               NOT NULL,
        [ParentCommentId]       BIGINT               NULL,

        [Content]               NVARCHAR(2000)       NOT NULL,

        [Status]                NVARCHAR(20)         NOT NULL
            CONSTRAINT [DF_Comment_Status] DEFAULT (N'Visible'),

        [CreatedAt]             DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_Comment_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt]             DATETIME2(3)         NULL,

        [DeletedAt]             DATETIME2(3)         NULL,
        [DeletedByUserId]       BIGINT               NULL,

        [EditCount]             INT                  NOT NULL
            CONSTRAINT [DF_Comment_EditCount] DEFAULT (0),

        CONSTRAINT [PK_Comment]
            PRIMARY KEY CLUSTERED ([CommentId] ASC),

        CONSTRAINT [FK_Comment_Article]
            FOREIGN KEY ([ArticleId])
            REFERENCES [content].[Article]([ArticleId]),

        CONSTRAINT [FK_Comment_User]
            FOREIGN KEY ([UserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [FK_Comment_ParentComment]
            FOREIGN KEY ([ParentCommentId])
            REFERENCES [interaction].[Comment]([CommentId]),

        CONSTRAINT [FK_Comment_DeletedByUser]
            FOREIGN KEY ([DeletedByUserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [CK_Comment_Content_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([Content]))) > 0),

        CONSTRAINT [CK_Comment_Status]
            CHECK ([Status] IN (N'Visible', N'Hidden', N'Deleted', N'Pending')),

        CONSTRAINT [CK_Comment_EditCount]
            CHECK ([EditCount] >= 0),

        CONSTRAINT [CK_Comment_UpdatedAt]
            CHECK ([UpdatedAt] IS NULL OR [UpdatedAt] >= [CreatedAt]),

        CONSTRAINT [CK_Comment_DeletedAt]
            CHECK ([DeletedAt] IS NULL OR [DeletedAt] >= [CreatedAt]),

        CONSTRAINT [CK_Comment_DeletedState]
            CHECK (
                ([Status] = N'Deleted' AND [DeletedAt] IS NOT NULL)
                OR
                ([Status] <> N'Deleted')
            )
    );

    PRINT N'Created table: [interaction].[Comment]';
END
ELSE
BEGIN
    PRINT N'Table exists: [interaction].[Comment]';
END
GO

/* =========================================================
   4) [interaction].[ArticleInteractionStats]
   ========================================================= */
IF OBJECT_ID(N'[interaction].[ArticleInteractionStats]', N'U') IS NULL
BEGIN
    CREATE TABLE [interaction].[ArticleInteractionStats]
    (
        [ArticleId]             BIGINT               NOT NULL,

        [ViewsTotal]            BIGINT               NOT NULL
            CONSTRAINT [DF_ArticleInteractionStats_ViewsTotal] DEFAULT (0),
        [LikesTotal]            BIGINT               NOT NULL
            CONSTRAINT [DF_ArticleInteractionStats_LikesTotal] DEFAULT (0),
        [CommentsTotal]         BIGINT               NOT NULL
            CONSTRAINT [DF_ArticleInteractionStats_CommentsTotal] DEFAULT (0),

        [PopularityScore]       DECIMAL(18,4)        NOT NULL
            CONSTRAINT [DF_ArticleInteractionStats_PopularityScore] DEFAULT (0),

        [CreatedAt]             DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_ArticleInteractionStats_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt]             DATETIME2(3)         NULL,
        [LastAggregatedAt]      DATETIME2(3)         NULL,

        CONSTRAINT [PK_ArticleInteractionStats]
            PRIMARY KEY CLUSTERED ([ArticleId] ASC),

        CONSTRAINT [FK_ArticleInteractionStats_Article]
            FOREIGN KEY ([ArticleId])
            REFERENCES [content].[Article]([ArticleId]),

        CONSTRAINT [CK_ArticleInteractionStats_ViewsTotal]
            CHECK ([ViewsTotal] >= 0),

        CONSTRAINT [CK_ArticleInteractionStats_LikesTotal]
            CHECK ([LikesTotal] >= 0),

        CONSTRAINT [CK_ArticleInteractionStats_CommentsTotal]
            CHECK ([CommentsTotal] >= 0),

        CONSTRAINT [CK_ArticleInteractionStats_PopularityScore]
            CHECK ([PopularityScore] >= 0),

        CONSTRAINT [CK_ArticleInteractionStats_UpdatedAt]
            CHECK ([UpdatedAt] IS NULL OR [UpdatedAt] >= [CreatedAt]),

        CONSTRAINT [CK_ArticleInteractionStats_LastAggregatedAt]
            CHECK ([LastAggregatedAt] IS NULL OR [LastAggregatedAt] >= [CreatedAt])
    );

    PRINT N'Created table: [interaction].[ArticleInteractionStats]';
END
ELSE
BEGIN
    PRINT N'Table exists: [interaction].[ArticleInteractionStats]';
END
GO