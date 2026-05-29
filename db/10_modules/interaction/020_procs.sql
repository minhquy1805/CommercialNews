/*
  File: db/10_modules/interaction/020_procs.sql
  Module: Interaction
  Purpose:
  - Create stored procedures for Interaction V1 Async + Admin Moderation.
  - Support:
      * Content-derived article interaction eligibility projection
      * durable accepted-view counting through ArticleViewCount
      * idempotent like / unlike relationship truth
      * top-level visible comment creation and public comment queries
      * admin comment moderation and author delete-own-comment
      * comment report and moderation-case workflow
      * local moderation action history queries
      * versioned ArticleInteractionStats materialization for Reading
      * durable consumer message reservation / apply completion
  - Idempotent: safe to re-run through CREATE OR ALTER PROCEDURE.

  Notes:
  - Interaction does NOT query Content or Reading truth on ordinary command paths.
  - New view / like / comment / report operations depend on
    [interaction].[ArticleInteractionTargetProjection].
  - New public interaction fails closed when eligibility is missing,
    disabled or requires resync.
  - V1 does NOT store raw ArticleViewEvent history.
  - V1 does NOT support comment editing or reply creation.
  - Comment.ParentCommentId remains NULL for all V1-created comments.
  - ArticleInteractionStats is derived public snapshot state.
  - Reading consumes interaction.article_counters_projection_published
    using StatsVersion.
  - Notifications consumes interaction.comment_report_alert_triggered.
  - Audit consumes moderation-relevant Interaction events asynchronously.
  - Application layer must coordinate:
      Interaction mutation + required OutboxMessage
    inside one shared transaction where async propagation is required.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 58201, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

USE [CommercialNews];
GO

IF SCHEMA_ID(N'interaction') IS NULL
BEGIN
    THROW 58202, 'Schema [interaction] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

IF OBJECT_ID(N'[interaction].[ArticleInteractionTargetProjection]', N'U') IS NULL
BEGIN
    THROW 58203, 'Table [interaction].[ArticleInteractionTargetProjection] does not exist. Run interaction/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[interaction].[ArticleViewCount]', N'U') IS NULL
BEGIN
    THROW 58204, 'Table [interaction].[ArticleViewCount] does not exist. Run interaction/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[interaction].[ArticleLike]', N'U') IS NULL
BEGIN
    THROW 58205, 'Table [interaction].[ArticleLike] does not exist. Run interaction/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[interaction].[Comment]', N'U') IS NULL
BEGIN
    THROW 58206, 'Table [interaction].[Comment] does not exist. Run interaction/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[interaction].[CommentReport]', N'U') IS NULL
BEGIN
    THROW 58207, 'Table [interaction].[CommentReport] does not exist. Run interaction/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[interaction].[CommentModerationCase]', N'U') IS NULL
BEGIN
    THROW 58208, 'Table [interaction].[CommentModerationCase] does not exist. Run interaction/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[interaction].[CommentModerationActionHistory]', N'U') IS NULL
BEGIN
    THROW 58209, 'Table [interaction].[CommentModerationActionHistory] does not exist. Run interaction/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[interaction].[ArticleInteractionStats]', N'U') IS NULL
BEGIN
    THROW 58210, 'Table [interaction].[ArticleInteractionStats] does not exist. Run interaction/001_tables.sql first.', 1;
END
GO

/* =========================================================
   ARTICLE INTERACTION TARGET PROJECTION
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_ArticleInteractionTargetProjection_SelectByArticlePublicId]
    @ArticlePublicId CHAR(26)
AS
BEGIN
    SET NOCOUNT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@ArticlePublicId, '')))) = 0
        THROW 58220, 'ArticlePublicId is required.', 1;

    SELECT TOP (1)
        [ArticleInteractionTargetProjectionId],
        [ArticlePublicId],
        [SourceStatus],
        [IsInteractionEnabled],
        [LastSourceVersion],
        [LastSourceMessageId],
        [LastSourceOccurredAtUtc],
        [LastSyncedAtUtc],
        [RequiresResync],
        [CreatedAtUtc],
        [UpdatedAtUtc]
    FROM [interaction].[ArticleInteractionTargetProjection]
    WHERE [ArticlePublicId] = @ArticlePublicId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/*
  Consumer apply procedure for Content -> Interaction eligibility projection.

  @RequiresResync = 1 is used when the consumer has detected an unsafe gap.
  In that case new interaction is disabled until repaired.
*/
CREATE OR ALTER PROCEDURE [interaction].[Interaction_ArticleInteractionTargetProjection_Apply]
    @ArticlePublicId          CHAR(26),
    @SourceStatus             NVARCHAR(30),
    @IsInteractionEnabled     BIT,
    @SourceVersion            BIGINT,
    @SourceMessageId          CHAR(26),
    @SourceOccurredAtUtc      DATETIME2(3) = NULL,
    @RequiresResync           BIT = 0,
    @ApplyDecision            NVARCHAR(30) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @ApplyDecision = N'Failed';

    IF LEN(LTRIM(RTRIM(ISNULL(@ArticlePublicId, '')))) = 0
        THROW 58221, 'ArticlePublicId is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@SourceStatus, N'')))) = 0
        THROW 58222, 'SourceStatus is required.', 1;

    IF @SourceVersion IS NULL OR @SourceVersion < 0
        THROW 58223, 'SourceVersion must be >= 0.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@SourceMessageId, '')))) = 0
        THROW 58224, 'SourceMessageId is required.', 1;

    DECLARE
        @CurrentVersion BIGINT,
        @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    SELECT
        @CurrentVersion = [LastSourceVersion]
    FROM [interaction].[ArticleInteractionTargetProjection] WITH (UPDLOCK, HOLDLOCK)
    WHERE [ArticlePublicId] = @ArticlePublicId;

    IF @CurrentVersion IS NOT NULL
       AND @SourceVersion <= @CurrentVersion
    BEGIN
        SET @ApplyDecision = N'StaleIgnored';
        RETURN;
    END

    IF @RequiresResync = 1
    BEGIN
        IF @CurrentVersion IS NULL
        BEGIN
            INSERT INTO [interaction].[ArticleInteractionTargetProjection]
            (
                [ArticlePublicId],
                [SourceStatus],
                [IsInteractionEnabled],
                [LastSourceVersion],
                [LastSourceMessageId],
                [LastSourceOccurredAtUtc],
                [LastSyncedAtUtc],
                [RequiresResync],
                [CreatedAtUtc],
                [UpdatedAtUtc]
            )
            VALUES
            (
                @ArticlePublicId,
                @SourceStatus,
                0,
                0,
                NULL,
                NULL,
                @NowUtc,
                1,
                @NowUtc,
                @NowUtc
            );
        END
        ELSE
        BEGIN
            UPDATE [interaction].[ArticleInteractionTargetProjection]
            SET
                [IsInteractionEnabled] = 0,
                [RequiresResync] = 1,
                [LastSyncedAtUtc] = @NowUtc,
                [UpdatedAtUtc] = @NowUtc
            WHERE [ArticlePublicId] = @ArticlePublicId;
        END

        SET @ApplyDecision = N'ResyncRequired';
        RETURN;
    END

    IF @CurrentVersion IS NULL
    BEGIN
        INSERT INTO [interaction].[ArticleInteractionTargetProjection]
        (
            [ArticlePublicId],
            [SourceStatus],
            [IsInteractionEnabled],
            [LastSourceVersion],
            [LastSourceMessageId],
            [LastSourceOccurredAtUtc],
            [LastSyncedAtUtc],
            [RequiresResync],
            [CreatedAtUtc],
            [UpdatedAtUtc]
        )
        VALUES
        (
            @ArticlePublicId,
            @SourceStatus,
            @IsInteractionEnabled,
            @SourceVersion,
            @SourceMessageId,
            @SourceOccurredAtUtc,
            @NowUtc,
            0,
            @NowUtc,
            NULL
        );
    END
    ELSE
    BEGIN
        UPDATE [interaction].[ArticleInteractionTargetProjection]
        SET
            [SourceStatus] = @SourceStatus,
            [IsInteractionEnabled] = @IsInteractionEnabled,
            [LastSourceVersion] = @SourceVersion,
            [LastSourceMessageId] = @SourceMessageId,
            [LastSourceOccurredAtUtc] = @SourceOccurredAtUtc,
            [LastSyncedAtUtc] = @NowUtc,
            [RequiresResync] = 0,
            [UpdatedAtUtc] = @NowUtc
        WHERE [ArticlePublicId] = @ArticlePublicId;
    END

    SET @ApplyDecision = N'Applied';

    SELECT TOP (1)
        [ArticleInteractionTargetProjectionId],
        [ArticlePublicId],
        [SourceStatus],
        [IsInteractionEnabled],
        [LastSourceVersion],
        [LastSourceMessageId],
        [LastSourceOccurredAtUtc],
        [LastSyncedAtUtc],
        [RequiresResync],
        [CreatedAtUtc],
        [UpdatedAtUtc]
    FROM [interaction].[ArticleInteractionTargetProjection]
    WHERE [ArticlePublicId] = @ArticlePublicId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_ArticleInteractionTargetProjection_MarkRequiresResync]
    @ArticlePublicId CHAR(26),
    @AffectedRows    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @AffectedRows = 0;

    IF LEN(LTRIM(RTRIM(ISNULL(@ArticlePublicId, '')))) = 0
        THROW 58225, 'ArticlePublicId is required.', 1;

    UPDATE [interaction].[ArticleInteractionTargetProjection]
    SET
        [IsInteractionEnabled] = 0,
        [RequiresResync] = 1,
        [UpdatedAtUtc] = SYSUTCDATETIME()
    WHERE [ArticlePublicId] = @ArticlePublicId
      AND [RequiresResync] = 0;

    SET @AffectedRows = @@ROWCOUNT;
END
GO

/* =========================================================
   ARTICLE VIEW COUNT
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_ArticleViewCount_SelectByArticlePublicId]
    @ArticlePublicId CHAR(26)
AS
BEGIN
    SET NOCOUNT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@ArticlePublicId, '')))) = 0
        THROW 58230, 'ArticlePublicId is required.', 1;

    SELECT TOP (1)
        [ArticleViewCountId],
        [ArticlePublicId],
        [ViewCount],
        [ViewVersion],
        [LastAcceptedViewAtUtc],
        [CreatedAtUtc],
        [UpdatedAtUtc]
    FROM [interaction].[ArticleViewCount]
    WHERE [ArticlePublicId] = @ArticlePublicId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/*
  Called only after rate-limit / repeat-view / abuse policy accepts
  the contribution. This procedure persists the durable accepted count.
*/
CREATE OR ALTER PROCEDURE [interaction].[Interaction_ArticleViewCount_IncrementAccepted]
    @ArticlePublicId CHAR(26)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@ArticlePublicId, '')))) = 0
        THROW 58231, 'ArticlePublicId is required.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [interaction].[ArticleInteractionTargetProjection]
        WHERE [ArticlePublicId] = @ArticlePublicId
          AND [IsInteractionEnabled] = 1
          AND [RequiresResync] = 0
    )
        THROW 58232, 'Article is not currently available for interaction.', 1;

    DECLARE @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    UPDATE [interaction].[ArticleViewCount]
    SET
        [ViewCount] = [ViewCount] + 1,
        [ViewVersion] = [ViewVersion] + 1,
        [LastAcceptedViewAtUtc] = @NowUtc,
        [UpdatedAtUtc] = @NowUtc
    WHERE [ArticlePublicId] = @ArticlePublicId;

    IF @@ROWCOUNT = 0
    BEGIN
        BEGIN TRY
            INSERT INTO [interaction].[ArticleViewCount]
            (
                [ArticlePublicId],
                [ViewCount],
                [ViewVersion],
                [LastAcceptedViewAtUtc],
                [CreatedAtUtc],
                [UpdatedAtUtc]
            )
            VALUES
            (
                @ArticlePublicId,
                1,
                1,
                @NowUtc,
                @NowUtc,
                @NowUtc
            );
        END TRY
        BEGIN CATCH
            IF ERROR_NUMBER() IN (2601, 2627)
            BEGIN
                UPDATE [interaction].[ArticleViewCount]
                SET
                    [ViewCount] = [ViewCount] + 1,
                    [ViewVersion] = [ViewVersion] + 1,
                    [LastAcceptedViewAtUtc] = @NowUtc,
                    [UpdatedAtUtc] = @NowUtc
                WHERE [ArticlePublicId] = @ArticlePublicId;
            END
            ELSE
            BEGIN
                THROW;
            END
        END CATCH
    END

    SELECT TOP (1)
        [ArticleViewCountId],
        [ArticlePublicId],
        [ViewCount],
        [ViewVersion],
        [LastAcceptedViewAtUtc],
        [CreatedAtUtc],
        [UpdatedAtUtc]
    FROM [interaction].[ArticleViewCount]
    WHERE [ArticlePublicId] = @ArticlePublicId;
END
GO

/* =========================================================
   ARTICLE LIKE
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_ArticleLike_SelectByArticlePublicIdAndUserId]
    @ArticlePublicId CHAR(26),
    @UserId          BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@ArticlePublicId, '')))) = 0
        THROW 58240, 'ArticlePublicId is required.', 1;

    IF @UserId IS NULL OR @UserId <= 0
        THROW 58241, 'UserId must be > 0.', 1;

    SELECT TOP (1)
        [ArticleLikeId],
        [PublicId],
        [ArticlePublicId],
        [UserId],
        [IsActive],
        [LikedAtUtc],
        [UnlikedAtUtc],
        [Version],
        [CreatedAtUtc],
        [UpdatedAtUtc]
    FROM [interaction].[ArticleLike]
    WHERE [ArticlePublicId] = @ArticlePublicId
      AND [UserId] = @UserId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/*
  Like is idempotent:
  - new relationship -> insert active row, @Changed = 1
  - inactive relationship -> activate row, @Changed = 1
  - already active -> return current state, @Changed = 0

  Application writes interaction.article_liked to Outbox only when
  @Changed = 1, in the same transaction.
*/
CREATE OR ALTER PROCEDURE [interaction].[Interaction_ArticleLike_SetLiked]
    @PublicId        CHAR(26),
    @ArticlePublicId CHAR(26),
    @UserId          BIGINT,
    @Changed         BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Changed = 0;

    IF LEN(LTRIM(RTRIM(ISNULL(@PublicId, '')))) = 0
        THROW 58242, 'PublicId is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@ArticlePublicId, '')))) = 0
        THROW 58243, 'ArticlePublicId is required.', 1;

    IF @UserId IS NULL OR @UserId <= 0
        THROW 58244, 'UserId must be > 0.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [interaction].[ArticleInteractionTargetProjection]
        WHERE [ArticlePublicId] = @ArticlePublicId
          AND [IsInteractionEnabled] = 1
          AND [RequiresResync] = 0
    )
        THROW 58245, 'Article is not currently available for interaction.', 1;

    DECLARE @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    UPDATE [interaction].[ArticleLike]
    SET
        [IsActive] = 1,
        [LikedAtUtc] = @NowUtc,
        [UnlikedAtUtc] = NULL,
        [Version] = [Version] + 1,
        [UpdatedAtUtc] = @NowUtc
    WHERE [ArticlePublicId] = @ArticlePublicId
      AND [UserId] = @UserId
      AND [IsActive] = 0;

    IF @@ROWCOUNT = 1
    BEGIN
        SET @Changed = 1;
    END
    ELSE IF NOT EXISTS
    (
        SELECT 1
        FROM [interaction].[ArticleLike]
        WHERE [ArticlePublicId] = @ArticlePublicId
          AND [UserId] = @UserId
    )
    BEGIN
        BEGIN TRY
            INSERT INTO [interaction].[ArticleLike]
            (
                [PublicId],
                [ArticlePublicId],
                [UserId],
                [IsActive],
                [LikedAtUtc],
                [UnlikedAtUtc],
                [Version],
                [CreatedAtUtc],
                [UpdatedAtUtc]
            )
            VALUES
            (
                @PublicId,
                @ArticlePublicId,
                @UserId,
                1,
                @NowUtc,
                NULL,
                1,
                @NowUtc,
                NULL
            );

            SET @Changed = 1;
        END TRY
        BEGIN CATCH
            IF ERROR_NUMBER() NOT IN (2601, 2627)
                THROW;

            /* Concurrent like won. Idempotent final state is still liked=true. */
            SET @Changed = 0;
        END CATCH
    END

    SELECT TOP (1)
        [ArticleLikeId],
        [PublicId],
        [ArticlePublicId],
        [UserId],
        [IsActive],
        [LikedAtUtc],
        [UnlikedAtUtc],
        [Version],
        [CreatedAtUtc],
        [UpdatedAtUtc]
    FROM [interaction].[ArticleLike]
    WHERE [ArticlePublicId] = @ArticlePublicId
      AND [UserId] = @UserId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/*
  Unlike remains allowed even when the article has become non-public.
  Application writes interaction.article_unliked only when @Changed = 1.
*/
CREATE OR ALTER PROCEDURE [interaction].[Interaction_ArticleLike_SetUnliked]
    @ArticlePublicId CHAR(26),
    @UserId          BIGINT,
    @Changed         BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Changed = 0;

    IF LEN(LTRIM(RTRIM(ISNULL(@ArticlePublicId, '')))) = 0
        THROW 58246, 'ArticlePublicId is required.', 1;

    IF @UserId IS NULL OR @UserId <= 0
        THROW 58247, 'UserId must be > 0.', 1;

    DECLARE @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    UPDATE [interaction].[ArticleLike]
    SET
        [IsActive] = 0,
        [UnlikedAtUtc] = @NowUtc,
        [Version] = [Version] + 1,
        [UpdatedAtUtc] = @NowUtc
    WHERE [ArticlePublicId] = @ArticlePublicId
      AND [UserId] = @UserId
      AND [IsActive] = 1;

    IF @@ROWCOUNT = 1
        SET @Changed = 1;

    SELECT TOP (1)
        [ArticleLikeId],
        [PublicId],
        [ArticlePublicId],
        [UserId],
        [IsActive],
        [LikedAtUtc],
        [UnlikedAtUtc],
        [Version],
        [CreatedAtUtc],
        [UpdatedAtUtc]
    FROM [interaction].[ArticleLike]
    WHERE [ArticlePublicId] = @ArticlePublicId
      AND [UserId] = @UserId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_ArticleLike_GetActiveCountByArticlePublicId]
    @ArticlePublicId CHAR(26)
AS
BEGIN
    SET NOCOUNT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@ArticlePublicId, '')))) = 0
        THROW 58248, 'ArticlePublicId is required.', 1;

    SELECT COUNT_BIG(1) AS [ActiveLikeCount]
    FROM [interaction].[ArticleLike]
    WHERE [ArticlePublicId] = @ArticlePublicId
      AND [IsActive] = 1;
END
GO

/* =========================================================
   COMMENT QUERY / CREATE
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/*
  V1 supports top-level comments only.
  ParentCommentId is intentionally not accepted as an input.
*/
IF OBJECT_ID(N'[interaction].[Interaction_Comment_InsertPending]', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE [interaction].[Interaction_Comment_InsertPending];
END
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_Comment_InsertVisible]
    @PublicId        CHAR(26),
    @ArticlePublicId CHAR(26),
    @AuthorUserId    BIGINT,
    @Content         NVARCHAR(2000)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@PublicId, '')))) = 0
        THROW 58250, 'PublicId is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@ArticlePublicId, '')))) = 0
        THROW 58251, 'ArticlePublicId is required.', 1;

    IF @AuthorUserId IS NULL OR @AuthorUserId <= 0
        THROW 58252, 'AuthorUserId must be > 0.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Content, N'')))) = 0
        THROW 58253, 'Content is required.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [interaction].[ArticleInteractionTargetProjection]
        WHERE [ArticlePublicId] = @ArticlePublicId
          AND [IsInteractionEnabled] = 1
          AND [RequiresResync] = 0
    )
        THROW 58254, 'Article is not currently available for interaction.', 1;

    INSERT INTO [interaction].[Comment]
    (
        [PublicId],
        [ArticlePublicId],
        [AuthorUserId],
        [ParentCommentId],
        [Content],
        [Status],
        [Version],
        [CreatedAtUtc],
        [UpdatedAtUtc],
        [DeletedAtUtc]
    )
    VALUES
    (
        @PublicId,
        @ArticlePublicId,
        @AuthorUserId,
        NULL,
        @Content,
        N'Visible',
        1,
        SYSUTCDATETIME(),
        NULL,
        NULL
    );

    SELECT TOP (1)
        [CommentId],
        [PublicId],
        [ArticlePublicId],
        [AuthorUserId],
        [ParentCommentId],
        [Content],
        [Status],
        [Version],
        [CreatedAtUtc],
        [UpdatedAtUtc],
        [DeletedAtUtc]
    FROM [interaction].[Comment]
    WHERE [CommentId] = SCOPE_IDENTITY();
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_Comment_SelectByPublicId]
    @CommentPublicId CHAR(26)
AS
BEGIN
    SET NOCOUNT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@CommentPublicId, '')))) = 0
        THROW 58255, 'CommentPublicId is required.', 1;

    SELECT TOP (1)
        [CommentId],
        [PublicId],
        [ArticlePublicId],
        [AuthorUserId],
        [ParentCommentId],
        [Content],
        [Status],
        [Version],
        [CreatedAtUtc],
        [UpdatedAtUtc],
        [DeletedAtUtc]
    FROM [interaction].[Comment]
    WHERE [PublicId] = @CommentPublicId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_Comment_SelectVisibleByArticlePublicId]
    @ArticlePublicId CHAR(26),
    @Skip            INT = 0,
    @Take            INT = 20,
    @SortDirection   NVARCHAR(4) = N'DESC'
AS
BEGIN
    SET NOCOUNT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@ArticlePublicId, '')))) = 0
        THROW 58256, 'ArticlePublicId is required.', 1;

    IF @Skip < 0
        SET @Skip = 0;

    IF @Take <= 0
        SET @Take = 20;

    IF @Take > 200
        SET @Take = 200;

    IF UPPER(@SortDirection) NOT IN (N'ASC', N'DESC')
        SET @SortDirection = N'DESC';

    IF NOT EXISTS
    (
        SELECT 1
        FROM [interaction].[ArticleInteractionTargetProjection]
        WHERE [ArticlePublicId] = @ArticlePublicId
          AND [IsInteractionEnabled] = 1
          AND [RequiresResync] = 0
    )
        THROW 58257, 'Article is not currently available for public comment query.', 1;

    ;WITH [VisibleComments] AS
    (
        SELECT
            [CommentId],
            [PublicId],
            [ArticlePublicId],
            [AuthorUserId],
            [ParentCommentId],
            [Content],
            [Status],
            [CreatedAtUtc],
            [UpdatedAtUtc],
            COUNT_BIG(1) OVER () AS [TotalCount]
        FROM [interaction].[Comment]
        WHERE [ArticlePublicId] = @ArticlePublicId
          AND [Status] = N'Visible'
          AND [ParentCommentId] IS NULL
    )
    SELECT
        [CommentId],
        [PublicId],
        [ArticlePublicId],
        [AuthorUserId],
        [ParentCommentId],
        [Content],
        [Status],
        [CreatedAtUtc],
        [UpdatedAtUtc],
        [TotalCount]
    FROM [VisibleComments]
    ORDER BY
        CASE WHEN UPPER(@SortDirection) = N'ASC' THEN [CreatedAtUtc] END ASC,
        CASE WHEN UPPER(@SortDirection) = N'DESC' THEN [CreatedAtUtc] END DESC,
        CASE WHEN UPPER(@SortDirection) = N'ASC' THEN [CommentId] END ASC,
        CASE WHEN UPPER(@SortDirection) = N'DESC' THEN [CommentId] END DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_Comment_SelectAdminPaged]
    @Status          NVARCHAR(20) = NULL,
    @ArticlePublicId CHAR(26) = NULL,
    @AuthorUserId    BIGINT = NULL,
    @Skip            INT = 0,
    @Take            INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    IF @Status IS NOT NULL
       AND @Status NOT IN (N'Pending', N'Visible', N'Rejected', N'Hidden', N'Deleted')
        THROW 58258, 'Status is invalid.', 1;

    IF @AuthorUserId IS NOT NULL AND @AuthorUserId <= 0
        THROW 58259, 'AuthorUserId must be > 0 when provided.', 1;

    IF @Skip < 0
        SET @Skip = 0;

    IF @Take <= 0
        SET @Take = 20;

    IF @Take > 200
        SET @Take = 200;

    ;WITH [Filtered] AS
    (
        SELECT
            [CommentId],
            [PublicId],
            [ArticlePublicId],
            [AuthorUserId],
            [ParentCommentId],
            [Content],
            [Status],
            [Version],
            [CreatedAtUtc],
            [UpdatedAtUtc],
            [DeletedAtUtc],
            COUNT_BIG(1) OVER () AS [TotalCount]
        FROM [interaction].[Comment]
        WHERE (@Status IS NULL OR [Status] = @Status)
          AND (@ArticlePublicId IS NULL OR [ArticlePublicId] = @ArticlePublicId)
          AND (@AuthorUserId IS NULL OR [AuthorUserId] = @AuthorUserId)
    )
    SELECT
        [CommentId],
        [PublicId],
        [ArticlePublicId],
        [AuthorUserId],
        [ParentCommentId],
        [Content],
        [Status],
        [Version],
        [CreatedAtUtc],
        [UpdatedAtUtc],
        [DeletedAtUtc],
        [TotalCount]
    FROM [Filtered]
    ORDER BY [CreatedAtUtc] DESC, [CommentId] DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_Comment_GetVisibleCountByArticlePublicId]
    @ArticlePublicId CHAR(26)
AS
BEGIN
    SET NOCOUNT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@ArticlePublicId, '')))) = 0
        THROW 58260, 'ArticlePublicId is required.', 1;

    SELECT COUNT_BIG(1) AS [VisibleCommentCount]
    FROM [interaction].[Comment]
    WHERE [ArticlePublicId] = @ArticlePublicId
      AND [Status] = N'Visible';
END
GO

/* =========================================================
   COMMENT ADMIN MODERATION
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_Comment_Approve]
    @CommentPublicId CHAR(26),
    @ExpectedVersion BIGINT,
    @HistoryPublicId CHAR(26),
    @ActorUserId     BIGINT,
    @ActorType       NVARCHAR(30) = N'Moderator',
    @Note            NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@CommentPublicId, '')))) = 0
        THROW 58270, 'CommentPublicId is required.', 1;

    IF @ExpectedVersion IS NULL OR @ExpectedVersion < 1
        THROW 58271, 'ExpectedVersion must be >= 1.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@HistoryPublicId, '')))) = 0
        THROW 58272, 'HistoryPublicId is required.', 1;

    IF @ActorUserId IS NULL OR @ActorUserId <= 0
        THROW 58273, 'ActorUserId must be > 0.', 1;

    DECLARE
        @CommentId BIGINT,
        @CurrentVersion BIGINT,
        @CurrentStatus NVARCHAR(20),
        @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    SELECT
        @CommentId = [CommentId],
        @CurrentVersion = [Version],
        @CurrentStatus = [Status]
    FROM [interaction].[Comment] WITH (UPDLOCK, HOLDLOCK)
    WHERE [PublicId] = @CommentPublicId;

    IF @CommentId IS NULL
        THROW 58274, 'Comment does not exist.', 1;

    IF @CurrentVersion <> @ExpectedVersion
        THROW 58275, 'Comment version conflict.', 1;

    IF @CurrentStatus <> N'Pending'
        THROW 58276, 'Comment status does not allow approval.', 1;

    UPDATE [interaction].[Comment]
    SET
        [Status] = N'Visible',
        [Version] = [Version] + 1,
        [UpdatedAtUtc] = @NowUtc
    WHERE [CommentId] = @CommentId
      AND [Version] = @ExpectedVersion
      AND [Status] = N'Pending';

    IF @@ROWCOUNT <> 1
        THROW 58277, 'Comment approval failed because current state changed.', 1;

    INSERT INTO [interaction].[CommentModerationActionHistory]
    (
        [PublicId],
        [CommentId],
        [CommentModerationCaseId],
        [ActionType],
        [FromStatus],
        [ToStatus],
        [ActorUserId],
        [ActorType],
        [ReasonCode],
        [Note],
        [OccurredAtUtc],
        [CorrelationId]
    )
    VALUES
    (
        @HistoryPublicId,
        @CommentId,
        NULL,
        N'Approve',
        N'Pending',
        N'Visible',
        @ActorUserId,
        @ActorType,
        NULL,
        @Note,
        @NowUtc,
        NULL
    );

    SELECT TOP (1)
        [CommentId],
        [PublicId],
        [ArticlePublicId],
        [AuthorUserId],
        [Status],
        [Version],
        [UpdatedAtUtc]
    FROM [interaction].[Comment]
    WHERE [CommentId] = @CommentId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_Comment_Reject]
    @CommentPublicId CHAR(26),
    @ExpectedVersion BIGINT,
    @HistoryPublicId CHAR(26),
    @ActorUserId     BIGINT,
    @ReasonCode      NVARCHAR(40),
    @Note            NVARCHAR(1000) = NULL,
    @ActorType       NVARCHAR(30) = N'Moderator'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@CommentPublicId, '')))) = 0
        THROW 58280, 'CommentPublicId is required.', 1;

    IF @ExpectedVersion IS NULL OR @ExpectedVersion < 1
        THROW 58281, 'ExpectedVersion must be >= 1.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@HistoryPublicId, '')))) = 0
        THROW 58282, 'HistoryPublicId is required.', 1;

    IF @ActorUserId IS NULL OR @ActorUserId <= 0
        THROW 58283, 'ActorUserId must be > 0.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@ReasonCode, N'')))) = 0
        THROW 58284, 'ReasonCode is required.', 1;

    DECLARE
        @CommentId BIGINT,
        @CurrentVersion BIGINT,
        @CurrentStatus NVARCHAR(20),
        @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    SELECT
        @CommentId = [CommentId],
        @CurrentVersion = [Version],
        @CurrentStatus = [Status]
    FROM [interaction].[Comment] WITH (UPDLOCK, HOLDLOCK)
    WHERE [PublicId] = @CommentPublicId;

    IF @CommentId IS NULL
        THROW 58285, 'Comment does not exist.', 1;

    IF @CurrentVersion <> @ExpectedVersion
        THROW 58286, 'Comment version conflict.', 1;

    IF @CurrentStatus <> N'Pending'
        THROW 58287, 'Comment status does not allow rejection.', 1;

    UPDATE [interaction].[Comment]
    SET
        [Status] = N'Rejected',
        [Version] = [Version] + 1,
        [UpdatedAtUtc] = @NowUtc
    WHERE [CommentId] = @CommentId
      AND [Version] = @ExpectedVersion
      AND [Status] = N'Pending';

    IF @@ROWCOUNT <> 1
        THROW 58288, 'Comment rejection failed because current state changed.', 1;

    INSERT INTO [interaction].[CommentModerationActionHistory]
    (
        [PublicId],
        [CommentId],
        [CommentModerationCaseId],
        [ActionType],
        [FromStatus],
        [ToStatus],
        [ActorUserId],
        [ActorType],
        [ReasonCode],
        [Note],
        [OccurredAtUtc],
        [CorrelationId]
    )
    VALUES
    (
        @HistoryPublicId,
        @CommentId,
        NULL,
        N'Reject',
        N'Pending',
        N'Rejected',
        @ActorUserId,
        @ActorType,
        @ReasonCode,
        @Note,
        @NowUtc,
        NULL
    );

    SELECT TOP (1)
        [CommentId],
        [PublicId],
        [ArticlePublicId],
        [AuthorUserId],
        [Status],
        [Version],
        [UpdatedAtUtc]
    FROM [interaction].[Comment]
    WHERE [CommentId] = @CommentId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/*
  Direct hide is allowed only when there is no Open moderation case.
  If an Open case exists, the caller must use
  Interaction_CommentModerationCase_HideComment.
*/
CREATE OR ALTER PROCEDURE [interaction].[Interaction_Comment_Hide]
    @CommentPublicId CHAR(26),
    @ExpectedVersion BIGINT,
    @HistoryPublicId CHAR(26),
    @ActorUserId     BIGINT,
    @ReasonCode      NVARCHAR(40),
    @Note            NVARCHAR(1000) = NULL,
    @ActorType       NVARCHAR(30) = N'Moderator',
    @CorrelationId   CHAR(26) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@CommentPublicId, '')))) = 0
        THROW 58290, 'CommentPublicId is required.', 1;

    IF @ExpectedVersion IS NULL OR @ExpectedVersion < 1
        THROW 58291, 'ExpectedVersion must be >= 1.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@HistoryPublicId, '')))) = 0
        THROW 58292, 'HistoryPublicId is required.', 1;

    IF @ActorUserId IS NULL OR @ActorUserId <= 0
        THROW 58293, 'ActorUserId must be > 0.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@ReasonCode, N'')))) = 0
        THROW 58294, 'ReasonCode is required.', 1;

    DECLARE
        @CommentId BIGINT,
        @CurrentVersion BIGINT,
        @CurrentStatus NVARCHAR(20),
        @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    SELECT
        @CommentId = [CommentId],
        @CurrentVersion = [Version],
        @CurrentStatus = [Status]
    FROM [interaction].[Comment] WITH (UPDLOCK, HOLDLOCK)
    WHERE [PublicId] = @CommentPublicId;

    IF @CommentId IS NULL
        THROW 58295, 'Comment does not exist.', 1;

    IF @CurrentVersion <> @ExpectedVersion
        THROW 58296, 'Comment version conflict.', 1;

    IF @CurrentStatus <> N'Visible'
        THROW 58297, 'Comment status does not allow hide.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [interaction].[CommentModerationCase]
        WHERE [CommentId] = @CommentId
          AND [Status] = N'Open'
    )
        THROW 58298, 'Open moderation case requires case-resolution hide flow.', 1;

    UPDATE [interaction].[Comment]
    SET
        [Status] = N'Hidden',
        [Version] = [Version] + 1,
        [UpdatedAtUtc] = @NowUtc
    WHERE [CommentId] = @CommentId
      AND [Version] = @ExpectedVersion
      AND [Status] = N'Visible';

    IF @@ROWCOUNT <> 1
        THROW 58299, 'Comment hide failed because current state changed.', 1;

    INSERT INTO [interaction].[CommentModerationActionHistory]
    (
        [PublicId],
        [CommentId],
        [CommentModerationCaseId],
        [ActionType],
        [FromStatus],
        [ToStatus],
        [ActorUserId],
        [ActorType],
        [ReasonCode],
        [Note],
        [OccurredAtUtc],
        [CorrelationId]
    )
    VALUES
    (
        @HistoryPublicId,
        @CommentId,
        NULL,
        N'Hide',
        N'Visible',
        N'Hidden',
        @ActorUserId,
        @ActorType,
        @ReasonCode,
        @Note,
        @NowUtc,
        @CorrelationId
    );

    SELECT TOP (1)
        [CommentId],
        [PublicId],
        [ArticlePublicId],
        [AuthorUserId],
        [Status],
        [Version],
        [UpdatedAtUtc]
    FROM [interaction].[Comment]
    WHERE [CommentId] = @CommentId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_Comment_Restore]
    @CommentPublicId CHAR(26),
    @ExpectedVersion BIGINT,
    @HistoryPublicId CHAR(26),
    @ActorUserId     BIGINT,
    @Note            NVARCHAR(1000) = NULL,
    @ActorType       NVARCHAR(30) = N'Moderator',
    @CorrelationId   CHAR(26) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@CommentPublicId, '')))) = 0
        THROW 58300, 'CommentPublicId is required.', 1;

    IF @ExpectedVersion IS NULL OR @ExpectedVersion < 1
        THROW 58301, 'ExpectedVersion must be >= 1.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@HistoryPublicId, '')))) = 0
        THROW 58302, 'HistoryPublicId is required.', 1;

    IF @ActorUserId IS NULL OR @ActorUserId <= 0
        THROW 58303, 'ActorUserId must be > 0.', 1;

    DECLARE
        @CommentId BIGINT,
        @CurrentVersion BIGINT,
        @CurrentStatus NVARCHAR(20),
        @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    SELECT
        @CommentId = [CommentId],
        @CurrentVersion = [Version],
        @CurrentStatus = [Status]
    FROM [interaction].[Comment] WITH (UPDLOCK, HOLDLOCK)
    WHERE [PublicId] = @CommentPublicId;

    IF @CommentId IS NULL
        THROW 58304, 'Comment does not exist.', 1;

    IF @CurrentVersion <> @ExpectedVersion
        THROW 58305, 'Comment version conflict.', 1;

    IF @CurrentStatus <> N'Hidden'
        THROW 58306, 'Comment status does not allow restore.', 1;

    UPDATE [interaction].[Comment]
    SET
        [Status] = N'Visible',
        [Version] = [Version] + 1,
        [UpdatedAtUtc] = @NowUtc
    WHERE [CommentId] = @CommentId
      AND [Version] = @ExpectedVersion
      AND [Status] = N'Hidden';

    IF @@ROWCOUNT <> 1
        THROW 58307, 'Comment restore failed because current state changed.', 1;

    INSERT INTO [interaction].[CommentModerationActionHistory]
    (
        [PublicId],
        [CommentId],
        [CommentModerationCaseId],
        [ActionType],
        [FromStatus],
        [ToStatus],
        [ActorUserId],
        [ActorType],
        [ReasonCode],
        [Note],
        [OccurredAtUtc],
        [CorrelationId]
    )
    VALUES
    (
        @HistoryPublicId,
        @CommentId,
        NULL,
        N'Restore',
        N'Hidden',
        N'Visible',
        @ActorUserId,
        @ActorType,
        NULL,
        @Note,
        @NowUtc,
        @CorrelationId
    );

    SELECT TOP (1)
        [CommentId],
        [PublicId],
        [ArticlePublicId],
        [AuthorUserId],
        [Status],
        [Version],
        [UpdatedAtUtc]
    FROM [interaction].[Comment]
    WHERE [CommentId] = @CommentId;
END
GO

/* =========================================================
   AUTHOR DELETE OWN COMMENT
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/*
  If the author's comment has an Open moderation case, this procedure closes:
    Comment -> Deleted
    Case -> ClosedByAuthorDeletion
    Pending Reports -> ClosedByAuthorDeletion
    History -> CloseCaseByAuthorDeletion

  Application writes required outbox messages in the same outer transaction.
*/
CREATE OR ALTER PROCEDURE [interaction].[Interaction_Comment_DeleteOwn]
    @CommentPublicId        CHAR(26),
    @AuthorUserId           BIGINT,
    @ExpectedVersion        BIGINT = NULL,
    @CaseCloseHistoryPublicId CHAR(26) = NULL,
    @CorrelationId          CHAR(26) = NULL,
    @Changed                BIT OUTPUT,
    @ClosedOpenCase         BIT OUTPUT,
    @WasVisible             BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Changed = 0;
    SET @ClosedOpenCase = 0;
    SET @WasVisible = 0;

    IF LEN(LTRIM(RTRIM(ISNULL(@CommentPublicId, '')))) = 0
        THROW 58310, 'CommentPublicId is required.', 1;

    IF @AuthorUserId IS NULL OR @AuthorUserId <= 0
        THROW 58311, 'AuthorUserId must be > 0.', 1;

    DECLARE
        @CommentId BIGINT,
        @CurrentStatus NVARCHAR(20),
        @CurrentVersion BIGINT,
        @OpenCaseId BIGINT,
        @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    SELECT
        @CommentId = [CommentId],
        @CurrentStatus = [Status],
        @CurrentVersion = [Version]
    FROM [interaction].[Comment] WITH (UPDLOCK, HOLDLOCK)
    WHERE [PublicId] = @CommentPublicId
      AND [AuthorUserId] = @AuthorUserId;

    IF @CommentId IS NULL
        THROW 58312, 'Comment does not exist.', 1;

    IF @CurrentStatus = N'Deleted'
    BEGIN
        SELECT TOP (1)
            [CommentId],
            [PublicId],
            [ArticlePublicId],
            [AuthorUserId],
            [Status],
            [Version],
            [DeletedAtUtc]
        FROM [interaction].[Comment]
        WHERE [CommentId] = @CommentId;

        RETURN;
    END

    IF @ExpectedVersion IS NOT NULL
       AND @ExpectedVersion <> @CurrentVersion
        THROW 58313, 'Comment version conflict.', 1;

    IF @CurrentStatus = N'Visible'
        SET @WasVisible = 1;

    SELECT TOP (1)
        @OpenCaseId = [CommentModerationCaseId]
    FROM [interaction].[CommentModerationCase] WITH (UPDLOCK, HOLDLOCK)
    WHERE [CommentId] = @CommentId
      AND [Status] = N'Open';

    IF @OpenCaseId IS NOT NULL
       AND LEN(LTRIM(RTRIM(ISNULL(@CaseCloseHistoryPublicId, '')))) = 0
        THROW 58314, 'CaseCloseHistoryPublicId is required when an open moderation case exists.', 1;

    UPDATE [interaction].[Comment]
    SET
        [Status] = N'Deleted',
        [DeletedAtUtc] = @NowUtc,
        [UpdatedAtUtc] = @NowUtc,
        [Version] = [Version] + 1
    WHERE [CommentId] = @CommentId
      AND [Status] <> N'Deleted'
      AND [Version] = @CurrentVersion;

    IF @@ROWCOUNT <> 1
        THROW 58315, 'Comment delete failed because current state changed.', 1;

    SET @Changed = 1;

    IF @OpenCaseId IS NOT NULL
    BEGIN
        UPDATE [interaction].[CommentModerationCase]
        SET
            [Status] = N'ClosedByAuthorDeletion',
            [ResolvedAtUtc] = @NowUtc,
            [ResolvedByUserId] = @AuthorUserId,
            [ResolutionType] = N'CloseCaseByAuthorDeletion',
            [Version] = [Version] + 1
        WHERE [CommentModerationCaseId] = @OpenCaseId
          AND [Status] = N'Open';

        IF @@ROWCOUNT <> 1
            THROW 58316, 'Open moderation case changed before author deletion could close it.', 1;

        UPDATE [interaction].[CommentReport]
        SET
            [Status] = N'ClosedByAuthorDeletion',
            [ResolvedAtUtc] = @NowUtc
        WHERE [CommentModerationCaseId] = @OpenCaseId
          AND [Status] = N'Pending';

        INSERT INTO [interaction].[CommentModerationActionHistory]
        (
            [PublicId],
            [CommentId],
            [CommentModerationCaseId],
            [ActionType],
            [FromStatus],
            [ToStatus],
            [ActorUserId],
            [ActorType],
            [ReasonCode],
            [Note],
            [OccurredAtUtc],
            [CorrelationId]
        )
        VALUES
        (
            @CaseCloseHistoryPublicId,
            @CommentId,
            @OpenCaseId,
            N'CloseCaseByAuthorDeletion',
            @CurrentStatus,
            N'Deleted',
            @AuthorUserId,
            N'Author',
            NULL,
            NULL,
            @NowUtc,
            @CorrelationId
        );

        SET @ClosedOpenCase = 1;
    END

    SELECT TOP (1)
        [CommentId],
        [PublicId],
        [ArticlePublicId],
        [AuthorUserId],
        [Status],
        [Version],
        [DeletedAtUtc],
        @ClosedOpenCase AS [ClosedOpenCase],
        @WasVisible AS [WasVisible]
    FROM [interaction].[Comment]
    WHERE [CommentId] = @CommentId;
END
GO

/* =========================================================
   COMMENT REPORT CREATION / CASE OPENING
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/*
  @EvaluatedSeverity is calculated by Application configuration from ReasonCode.
  It is not stored on CommentReport; the case stores aggregate priority/severity.

  Application should pre-generate @AlertMessageIdCandidate and write
  interaction.comment_report_alert_triggered to Outbox only when
  @AlertTriggered = 1, in the same outer transaction.
*/
CREATE OR ALTER PROCEDURE [interaction].[Interaction_CommentReport_Create]
    @ReportPublicId          CHAR(26),
    @NewCasePublicId         CHAR(26),
    @CommentPublicId         CHAR(26),
    @ReporterUserId          BIGINT,
    @ReasonCode              NVARCHAR(40),
    @Description             NVARCHAR(1000) = NULL,
    @EvaluatedSeverity       NVARCHAR(20) = N'Normal',
    @NormalAlertThreshold    INT = 3,
    @AlertMessageIdCandidate CHAR(26) = NULL,
    @AlertTriggered          BIT OUTPUT,
    @CreatedNewCase          BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @AlertTriggered = 0;
    SET @CreatedNewCase = 0;

    IF LEN(LTRIM(RTRIM(ISNULL(@ReportPublicId, '')))) = 0
        THROW 58320, 'ReportPublicId is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@CommentPublicId, '')))) = 0
        THROW 58321, 'CommentPublicId is required.', 1;

    IF @ReporterUserId IS NULL OR @ReporterUserId <= 0
        THROW 58322, 'ReporterUserId must be > 0.', 1;

    IF @ReasonCode NOT IN
    (
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
        THROW 58323, 'ReasonCode is invalid.', 1;

    IF @ReasonCode = N'Other'
       AND LEN(LTRIM(RTRIM(ISNULL(@Description, N'')))) = 0
        THROW 58324, 'Description is required when ReasonCode is Other.', 1;

    IF @EvaluatedSeverity NOT IN (N'Normal', N'High', N'Critical')
        THROW 58325, 'EvaluatedSeverity is invalid.', 1;

    IF @NormalAlertThreshold IS NULL OR @NormalAlertThreshold <= 0
        THROW 58326, 'NormalAlertThreshold must be > 0.', 1;

    DECLARE
        @CommentId BIGINT,
        @ArticlePublicId CHAR(26),
        @AuthorUserId BIGINT,
        @CommentStatus NVARCHAR(20),
        @CaseId BIGINT,
        @CurrentPriority NVARCHAR(20),
        @CurrentHighestSeverity NVARCHAR(20),
        @PendingReportCount BIGINT,
        @ShouldAlert BIT = 0,
        @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    SELECT
        @CommentId = [CommentId],
        @ArticlePublicId = [ArticlePublicId],
        @AuthorUserId = [AuthorUserId],
        @CommentStatus = [Status]
    FROM [interaction].[Comment] WITH (UPDLOCK, HOLDLOCK)
    WHERE [PublicId] = @CommentPublicId;

    IF @CommentId IS NULL
        THROW 58327, 'Comment does not exist.', 1;

    IF @CommentStatus <> N'Visible'
        THROW 58328, 'Comment is not reportable.', 1;

    IF @AuthorUserId = @ReporterUserId
        THROW 58329, 'A user cannot report their own comment.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [interaction].[ArticleInteractionTargetProjection]
        WHERE [ArticlePublicId] = @ArticlePublicId
          AND [IsInteractionEnabled] = 1
          AND [RequiresResync] = 0
    )
        THROW 58330, 'Article is not currently available for interaction.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [interaction].[CommentReport]
        WHERE [CommentId] = @CommentId
          AND [ReporterUserId] = @ReporterUserId
    )
        THROW 58331, 'Report has already been submitted for this comment by this user.', 1;

    SELECT TOP (1)
        @CaseId = [CommentModerationCaseId],
        @CurrentPriority = [Priority],
        @CurrentHighestSeverity = [HighestSeverity]
    FROM [interaction].[CommentModerationCase] WITH (UPDLOCK, HOLDLOCK)
    WHERE [CommentId] = @CommentId
      AND [Status] = N'Open';

    IF @CaseId IS NULL
    BEGIN
        IF LEN(LTRIM(RTRIM(ISNULL(@NewCasePublicId, '')))) = 0
            THROW 58332, 'NewCasePublicId is required when no open moderation case exists.', 1;

        INSERT INTO [interaction].[CommentModerationCase]
        (
            [PublicId],
            [CommentId],
            [Status],
            [Priority],
            [HighestSeverity],
            [OpenedAtUtc],
            [Version]
        )
        VALUES
        (
            @NewCasePublicId,
            @CommentId,
            N'Open',
            @EvaluatedSeverity,
            @EvaluatedSeverity,
            @NowUtc,
            1
        );

        SET @CaseId = SCOPE_IDENTITY();
        SET @CreatedNewCase = 1;
    END
    ELSE
    BEGIN
        UPDATE [interaction].[CommentModerationCase]
        SET
            [Priority] =
                CASE
                    WHEN [Priority] = N'Critical' OR @EvaluatedSeverity = N'Critical' THEN N'Critical'
                    WHEN [Priority] = N'High' OR @EvaluatedSeverity = N'High' THEN N'High'
                    ELSE N'Normal'
                END,
            [HighestSeverity] =
                CASE
                    WHEN [HighestSeverity] = N'Critical' OR @EvaluatedSeverity = N'Critical' THEN N'Critical'
                    WHEN [HighestSeverity] = N'High' OR @EvaluatedSeverity = N'High' THEN N'High'
                    ELSE N'Normal'
                END,
            [Version] = [Version] + 1
        WHERE [CommentModerationCaseId] = @CaseId
          AND [Status] = N'Open';
    END

    INSERT INTO [interaction].[CommentReport]
    (
        [PublicId],
        [CommentId],
        [CommentModerationCaseId],
        [ReporterUserId],
        [ReasonCode],
        [Description],
        [Status],
        [CreatedAtUtc],
        [ResolvedAtUtc]
    )
    VALUES
    (
        @ReportPublicId,
        @CommentId,
        @CaseId,
        @ReporterUserId,
        @ReasonCode,
        @Description,
        N'Pending',
        @NowUtc,
        NULL
    );

    SELECT
        @PendingReportCount = COUNT_BIG(1)
    FROM [interaction].[CommentReport]
    WHERE [CommentModerationCaseId] = @CaseId
      AND [Status] = N'Pending';

    IF @EvaluatedSeverity IN (N'High', N'Critical')
       OR @PendingReportCount >= @NormalAlertThreshold
    BEGIN
        SET @ShouldAlert = 1;
    END

    IF @ShouldAlert = 1
       AND NOT EXISTS
       (
           SELECT 1
           FROM [interaction].[CommentModerationCase]
           WHERE [CommentModerationCaseId] = @CaseId
             AND [AlertTriggeredAtUtc] IS NOT NULL
       )
    BEGIN
        IF LEN(LTRIM(RTRIM(ISNULL(@AlertMessageIdCandidate, '')))) = 0
            THROW 58333, 'AlertMessageIdCandidate is required when alert policy is triggered.', 1;

        UPDATE [interaction].[CommentModerationCase]
        SET
            [Priority] =
                CASE
                    WHEN @EvaluatedSeverity = N'Critical' THEN N'Critical'
                    WHEN [Priority] = N'Critical' THEN N'Critical'
                    ELSE N'High'
                END,
            [HighestSeverity] =
                CASE
                    WHEN [HighestSeverity] = N'Critical' OR @EvaluatedSeverity = N'Critical' THEN N'Critical'
                    WHEN [HighestSeverity] = N'High' OR @EvaluatedSeverity = N'High' THEN N'High'
                    ELSE N'Normal'
                END,
            [AlertTriggeredAtUtc] = @NowUtc,
            [AlertLevel] =
                CASE
                    WHEN @EvaluatedSeverity = N'Critical' THEN N'Critical'
                    ELSE N'High'
                END,
            [AlertMessageId] = @AlertMessageIdCandidate,
            [Version] = [Version] + 1
        WHERE [CommentModerationCaseId] = @CaseId
          AND [Status] = N'Open'
          AND [AlertTriggeredAtUtc] IS NULL;

        IF @@ROWCOUNT = 1
            SET @AlertTriggered = 1;
    END

    SELECT TOP (1)
        [r].[CommentReportId],
        [r].[PublicId] AS [CommentReportPublicId],

        @CommentPublicId AS [CommentPublicId],
        @ArticlePublicId AS [ArticlePublicId],

        [r].[CommentId],
        [r].[CommentModerationCaseId],
        [r].[ReporterUserId],
        [r].[ReasonCode],
        [r].[Description],
        [r].[Status] AS [ReportStatus],
        [r].[CreatedAtUtc],

        [c].[PublicId] AS [CommentModerationCasePublicId],
        [c].[Status] AS [CaseStatus],
        [c].[Priority],
        [c].[HighestSeverity],

        @PendingReportCount AS [DistinctReporterCount],

        [c].[AlertTriggeredAtUtc],
        [c].[AlertLevel],
        [c].[AlertMessageId],
        [c].[Version] AS [CaseVersion],

        @AlertTriggered AS [AlertTriggered],
        @CreatedNewCase AS [CreatedNewCase]
    FROM [interaction].[CommentReport] AS [r]
    INNER JOIN [interaction].[CommentModerationCase] AS [c]
        ON [c].[CommentModerationCaseId] = [r].[CommentModerationCaseId]
    WHERE [r].[PublicId] = @ReportPublicId;
END
GO

/* =========================================================
   MODERATION CASE QUERY / RESOLUTION
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_CommentModerationCase_SelectPaged]
    @Status          NVARCHAR(30) = NULL,
    @Priority        NVARCHAR(20) = NULL,
    @ArticlePublicId CHAR(26) = NULL,
    @CommentPublicId CHAR(26) = NULL,
    @AlertTriggered  BIT = NULL,
    @Skip            INT = 0,
    @Take            INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    IF @Status IS NOT NULL
       AND @Status NOT IN (N'Open', N'Dismissed', N'Actioned', N'ClosedByAuthorDeletion')
        THROW 58340, 'Status is invalid.', 1;

    IF @Priority IS NOT NULL
       AND @Priority NOT IN (N'Normal', N'High', N'Critical')
        THROW 58341, 'Priority is invalid.', 1;

    IF @Skip < 0
        SET @Skip = 0;

    IF @Take <= 0
        SET @Take = 20;

    IF @Take > 200
        SET @Take = 200;

    ;WITH [Filtered] AS
    (
        SELECT
            [mc].[CommentModerationCaseId],
            [mc].[PublicId] AS [CommentModerationCasePublicId],
            [cm].[PublicId] AS [CommentPublicId],
            [cm].[ArticlePublicId],
            [mc].[Status],
            [mc].[Priority],
            [mc].[HighestSeverity],
            [mc].[AlertTriggeredAtUtc],
            [mc].[AlertLevel],
            [mc].[OpenedAtUtc],
            [mc].[ResolvedAtUtc],
            [mc].[ResolutionType],
            [mc].[Version],
            (
                SELECT COUNT_BIG(1)
                FROM [interaction].[CommentReport] AS [r]
                WHERE [r].[CommentModerationCaseId] = [mc].[CommentModerationCaseId]
                  AND [r].[Status] = N'Pending'
            ) AS [PendingReportCount],
            (
                SELECT COUNT_BIG(1)
                FROM [interaction].[CommentReport] AS [r]
                WHERE [r].[CommentModerationCaseId] = [mc].[CommentModerationCaseId]
            ) AS [TotalReportCount],
            COUNT_BIG(1) OVER () AS [TotalCount]
        FROM [interaction].[CommentModerationCase] AS [mc]
        INNER JOIN [interaction].[Comment] AS [cm]
            ON [cm].[CommentId] = [mc].[CommentId]
        WHERE (@Status IS NULL OR [mc].[Status] = @Status)
          AND (@Priority IS NULL OR [mc].[Priority] = @Priority)
          AND (@ArticlePublicId IS NULL OR [cm].[ArticlePublicId] = @ArticlePublicId)
          AND (@CommentPublicId IS NULL OR [cm].[PublicId] = @CommentPublicId)
          AND
          (
              @AlertTriggered IS NULL
              OR (@AlertTriggered = 1 AND [mc].[AlertTriggeredAtUtc] IS NOT NULL)
              OR (@AlertTriggered = 0 AND [mc].[AlertTriggeredAtUtc] IS NULL)
          )
    )
    SELECT *
    FROM [Filtered]
    ORDER BY [OpenedAtUtc] DESC, [CommentModerationCaseId] DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_CommentModerationCase_SelectByPublicId]
    @CasePublicId CHAR(26)
AS
BEGIN
    SET NOCOUNT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@CasePublicId, '')))) = 0
        THROW 58342, 'CasePublicId is required.', 1;

    SELECT TOP (1)
        [mc].[CommentModerationCaseId],
        [mc].[PublicId] AS [CommentModerationCasePublicId],
        [mc].[Status] AS [CaseStatus],
        [mc].[Priority],
        [mc].[HighestSeverity],
        [mc].[AlertTriggeredAtUtc],
        [mc].[AlertLevel],
        [mc].[AlertMessageId],
        [mc].[OpenedAtUtc],
        [mc].[ResolvedAtUtc],
        [mc].[ResolvedByUserId],
        [mc].[ResolutionType],
        [mc].[ResolutionReasonCode],
        [mc].[ResolutionNote],
        [mc].[Version] AS [CaseVersion],
        [cm].[PublicId] AS [CommentPublicId],
        [cm].[ArticlePublicId],
        [cm].[AuthorUserId],
        [cm].[Content],
        [cm].[Status] AS [CommentStatus],
        [cm].[Version] AS [CommentVersion],
        [cm].[CreatedAtUtc] AS [CommentCreatedAtUtc]
    FROM [interaction].[CommentModerationCase] AS [mc]
    INNER JOIN [interaction].[Comment] AS [cm]
        ON [cm].[CommentId] = [mc].[CommentId]
    WHERE [mc].[PublicId] = @CasePublicId;

    SELECT
        [r].[CommentReportId],
        [r].[PublicId] AS [CommentReportPublicId],
        [r].[ReporterUserId],
        [r].[ReasonCode],
        [r].[Description],
        [r].[Status],
        [r].[CreatedAtUtc],
        [r].[ResolvedAtUtc]
    FROM [interaction].[CommentReport] AS [r]
    INNER JOIN [interaction].[CommentModerationCase] AS [mc]
        ON [mc].[CommentModerationCaseId] = [r].[CommentModerationCaseId]
    WHERE [mc].[PublicId] = @CasePublicId
    ORDER BY [r].[CreatedAtUtc] ASC, [r].[CommentReportId] ASC;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_CommentModerationCase_Dismiss]
    @CasePublicId    CHAR(26),
    @ExpectedVersion BIGINT,
    @HistoryPublicId CHAR(26),
    @ActorUserId     BIGINT,
    @ReasonCode      NVARCHAR(40),
    @Note            NVARCHAR(1000) = NULL,
    @CorrelationId   CHAR(26) = NULL,
    @ActorType       NVARCHAR(30) = N'Moderator'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@CasePublicId, '')))) = 0
        THROW 58350, 'CasePublicId is required.', 1;

    IF @ExpectedVersion IS NULL OR @ExpectedVersion < 1
        THROW 58351, 'ExpectedVersion must be >= 1.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@HistoryPublicId, '')))) = 0
        THROW 58352, 'HistoryPublicId is required.', 1;

    IF @ActorUserId IS NULL OR @ActorUserId <= 0
        THROW 58353, 'ActorUserId must be > 0.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@ReasonCode, N'')))) = 0
        THROW 58354, 'ReasonCode is required.', 1;

    DECLARE
        @CaseId BIGINT,
        @CommentId BIGINT,
        @CaseStatus NVARCHAR(30),
        @CaseVersion BIGINT,
        @CommentStatus NVARCHAR(20),
        @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    SELECT
        @CaseId = [mc].[CommentModerationCaseId],
        @CommentId = [mc].[CommentId],
        @CaseStatus = [mc].[Status],
        @CaseVersion = [mc].[Version],
        @CommentStatus = [cm].[Status]
    FROM [interaction].[CommentModerationCase] AS [mc] WITH (UPDLOCK, HOLDLOCK)
    INNER JOIN [interaction].[Comment] AS [cm] WITH (UPDLOCK, HOLDLOCK)
        ON [cm].[CommentId] = [mc].[CommentId]
    WHERE [mc].[PublicId] = @CasePublicId;

    IF @CaseId IS NULL
        THROW 58355, 'Moderation case does not exist.', 1;

    IF @CaseVersion <> @ExpectedVersion
        THROW 58356, 'Moderation case version conflict.', 1;

    IF @CaseStatus <> N'Open'
        THROW 58357, 'Moderation case is no longer open.', 1;

    IF @CommentStatus <> N'Visible'
        THROW 58358, 'Reported comment is no longer visible.', 1;

    UPDATE [interaction].[CommentModerationCase]
    SET
        [Status] = N'Dismissed',
        [ResolvedAtUtc] = @NowUtc,
        [ResolvedByUserId] = @ActorUserId,
        [ResolutionType] = N'DismissReportedCase',
        [ResolutionReasonCode] = @ReasonCode,
        [ResolutionNote] = @Note,
        [Version] = [Version] + 1
    WHERE [CommentModerationCaseId] = @CaseId
      AND [Status] = N'Open'
      AND [Version] = @ExpectedVersion;

    IF @@ROWCOUNT <> 1
        THROW 58359, 'Moderation case dismissal failed because current state changed.', 1;

    UPDATE [interaction].[CommentReport]
    SET
        [Status] = N'Dismissed',
        [ResolvedAtUtc] = @NowUtc
    WHERE [CommentModerationCaseId] = @CaseId
      AND [Status] = N'Pending';

    INSERT INTO [interaction].[CommentModerationActionHistory]
    (
        [PublicId],
        [CommentId],
        [CommentModerationCaseId],
        [ActionType],
        [FromStatus],
        [ToStatus],
        [ActorUserId],
        [ActorType],
        [ReasonCode],
        [Note],
        [OccurredAtUtc],
        [CorrelationId]
    )
    VALUES
    (
        @HistoryPublicId,
        @CommentId,
        @CaseId,
        N'DismissReportedCase',
        N'Visible',
        N'Visible',
        @ActorUserId,
        @ActorType,
        @ReasonCode,
        @Note,
        @NowUtc,
        @CorrelationId
    );

    SELECT TOP (1)
        [PublicId] AS [CommentModerationCasePublicId],
        [Status] AS [CaseStatus],
        [Version] AS [CaseVersion],
        [ResolvedAtUtc],
        [ResolvedByUserId],
        [ResolutionType],
        [ResolutionReasonCode]
    FROM [interaction].[CommentModerationCase]
    WHERE [CommentModerationCaseId] = @CaseId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_CommentModerationCase_HideComment]
    @CasePublicId           CHAR(26),
    @ExpectedCaseVersion    BIGINT,
    @ExpectedCommentVersion BIGINT,
    @HistoryPublicId        CHAR(26),
    @ActorUserId            BIGINT,
    @ReasonCode             NVARCHAR(40),
    @Note                   NVARCHAR(1000) = NULL,
    @CorrelationId          CHAR(26) = NULL,
    @ActorType              NVARCHAR(30) = N'Moderator'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@CasePublicId, '')))) = 0
        THROW 58360, 'CasePublicId is required.', 1;

    IF @ExpectedCaseVersion IS NULL OR @ExpectedCaseVersion < 1
        THROW 58361, 'ExpectedCaseVersion must be >= 1.', 1;

    IF @ExpectedCommentVersion IS NULL OR @ExpectedCommentVersion < 1
        THROW 58362, 'ExpectedCommentVersion must be >= 1.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@HistoryPublicId, '')))) = 0
        THROW 58363, 'HistoryPublicId is required.', 1;

    IF @ActorUserId IS NULL OR @ActorUserId <= 0
        THROW 58364, 'ActorUserId must be > 0.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@ReasonCode, N'')))) = 0
        THROW 58365, 'ReasonCode is required.', 1;

    DECLARE
        @CaseId BIGINT,
        @CommentId BIGINT,
        @CaseStatus NVARCHAR(30),
        @CaseVersion BIGINT,
        @CommentStatus NVARCHAR(20),
        @CommentVersion BIGINT,
        @ResolvedReportCount BIGINT = 0,
        @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    SELECT
        @CaseId = [mc].[CommentModerationCaseId],
        @CommentId = [mc].[CommentId],
        @CaseStatus = [mc].[Status],
        @CaseVersion = [mc].[Version],
        @CommentStatus = [cm].[Status],
        @CommentVersion = [cm].[Version]
    FROM [interaction].[CommentModerationCase] AS [mc] WITH (UPDLOCK, HOLDLOCK)
    INNER JOIN [interaction].[Comment] AS [cm] WITH (UPDLOCK, HOLDLOCK)
        ON [cm].[CommentId] = [mc].[CommentId]
    WHERE [mc].[PublicId] = @CasePublicId;

    IF @CaseId IS NULL
        THROW 58366, 'Moderation case does not exist.', 1;

    IF @CaseVersion <> @ExpectedCaseVersion
        THROW 58367, 'Moderation case version conflict.', 1;

    IF @CommentVersion <> @ExpectedCommentVersion
        THROW 58368, 'Comment version conflict.', 1;

    IF @CaseStatus <> N'Open'
        THROW 58369, 'Moderation case is no longer open.', 1;

    IF @CommentStatus <> N'Visible'
        THROW 58370, 'Reported comment is no longer visible.', 1;

    UPDATE [interaction].[Comment]
    SET
        [Status] = N'Hidden',
        [Version] = [Version] + 1,
        [UpdatedAtUtc] = @NowUtc
    WHERE [CommentId] = @CommentId
      AND [Status] = N'Visible'
      AND [Version] = @ExpectedCommentVersion;

    IF @@ROWCOUNT <> 1
        THROW 58371, 'Comment hide failed because current state changed.', 1;

    UPDATE [interaction].[CommentModerationCase]
    SET
        [Status] = N'Actioned',
        [ResolvedAtUtc] = @NowUtc,
        [ResolvedByUserId] = @ActorUserId,
        [ResolutionType] = N'HideReportedComment',
        [ResolutionReasonCode] = @ReasonCode,
        [ResolutionNote] = @Note,
        [Version] = [Version] + 1
    WHERE [CommentModerationCaseId] = @CaseId
      AND [Status] = N'Open'
      AND [Version] = @ExpectedCaseVersion;

    IF @@ROWCOUNT <> 1
        THROW 58372, 'Moderation case action failed because current state changed.', 1;

    UPDATE [interaction].[CommentReport]
    SET
        [Status] = N'Actioned',
        [ResolvedAtUtc] = @NowUtc
    WHERE [CommentModerationCaseId] = @CaseId
      AND [Status] = N'Pending';

    SET @ResolvedReportCount = @@ROWCOUNT;

    INSERT INTO [interaction].[CommentModerationActionHistory]
    (
        [PublicId],
        [CommentId],
        [CommentModerationCaseId],
        [ActionType],
        [FromStatus],
        [ToStatus],
        [ActorUserId],
        [ActorType],
        [ReasonCode],
        [Note],
        [OccurredAtUtc],
        [CorrelationId]
    )
    VALUES
    (
        @HistoryPublicId,
        @CommentId,
        @CaseId,
        N'HideReportedComment',
        N'Visible',
        N'Hidden',
        @ActorUserId,
        @ActorType,
        @ReasonCode,
        @Note,
        @NowUtc,
        @CorrelationId
    );

    SELECT
        [mc].[PublicId] AS [CommentModerationCasePublicId],
        [mc].[Status] AS [CaseStatus],
        [mc].[Version] AS [CaseVersion],
        [mc].[ResolvedAtUtc],
        [mc].[ResolvedByUserId],
        [mc].[ResolutionType],
        [mc].[ResolutionReasonCode],

        [cm].[PublicId] AS [CommentPublicId],
        [cm].[ArticlePublicId],
        [cm].[Status] AS [CommentStatus],
        [cm].[Version] AS [CommentVersion],
        [cm].[UpdatedAtUtc] AS [HiddenAtUtc],

        @ResolvedReportCount AS [ResolvedReportCount]
    FROM [interaction].[CommentModerationCase] AS [mc]
    INNER JOIN [interaction].[Comment] AS [cm]
        ON [cm].[CommentId] = [mc].[CommentId]
    WHERE [mc].[CommentModerationCaseId] = @CaseId;
END
GO

/* =========================================================
   MODERATION ACTION HISTORY QUERY
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_CommentModerationActionHistory_SelectByCommentPublicId]
    @CommentPublicId CHAR(26),
    @Skip            INT = 0,
    @Take            INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@CommentPublicId, '')))) = 0
        THROW 58380, 'CommentPublicId is required.', 1;

    IF @Skip < 0
        SET @Skip = 0;

    IF @Take <= 0
        SET @Take = 20;

    IF @Take > 200
        SET @Take = 200;

    ;WITH [HistoryRows] AS
    (
        SELECT
            [h].[CommentModerationActionHistoryId],
            [h].[PublicId] AS [HistoryPublicId],
            [c].[PublicId] AS [CommentPublicId],
            [mc].[PublicId] AS [CommentModerationCasePublicId],
            [h].[ActionType],
            [h].[FromStatus],
            [h].[ToStatus],
            [h].[ActorUserId],
            [h].[ActorType],
            [h].[ReasonCode],
            [h].[Note],
            [h].[OccurredAtUtc],
            [h].[CorrelationId],
            COUNT_BIG(1) OVER () AS [TotalCount]
        FROM [interaction].[CommentModerationActionHistory] AS [h]
        INNER JOIN [interaction].[Comment] AS [c]
            ON [c].[CommentId] = [h].[CommentId]
        LEFT JOIN [interaction].[CommentModerationCase] AS [mc]
            ON [mc].[CommentModerationCaseId] = [h].[CommentModerationCaseId]
        WHERE [c].[PublicId] = @CommentPublicId
    )
    SELECT *
    FROM [HistoryRows]
    ORDER BY [OccurredAtUtc] DESC, [CommentModerationActionHistoryId] DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END
GO

/* =========================================================
   ARTICLE INTERACTION STATS
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_ArticleInteractionStats_SelectByArticlePublicId]
    @ArticlePublicId CHAR(26)
AS
BEGIN
    SET NOCOUNT ON;

    IF LEN(LTRIM(RTRIM(ISNULL(@ArticlePublicId, '')))) = 0
        THROW 58390, 'ArticlePublicId is required.', 1;

    SELECT TOP (1)
        [ArticleInteractionStatsId],
        [ArticlePublicId],
        [ViewCount],
        [LikeCount],
        [VisibleCommentCount],
        [StatsVersion],
        [LastMaterializedAtUtc],
        [LastPublishedMessageId],
        [LastPublishedAtUtc],
        [CreatedAtUtc],
        [UpdatedAtUtc]
    FROM [interaction].[ArticleInteractionStats]
    WHERE [ArticlePublicId] = @ArticlePublicId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/*
  Materializes known-value counters from Interaction-owned state.

  Application flow:
  1. Begin transaction.
  2. Generate @PublicationMessageIdCandidate.
  3. Call this procedure.
  4. If @SnapshotChanged = 1, write
       interaction.article_counters_projection_published
     using returned values and StatsVersion with the same MessageId.
  5. Commit.

  View publication may be coalesced by application/worker policy;
  this procedure does not force one event per accepted view.
*/
CREATE OR ALTER PROCEDURE [interaction].[Interaction_ArticleInteractionStats_Materialize]
    @ArticlePublicId              CHAR(26),
    @PublicationMessageIdCandidate CHAR(26),
    @SnapshotChanged              BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @SnapshotChanged = 0;

    IF LEN(LTRIM(RTRIM(ISNULL(@ArticlePublicId, '')))) = 0
        THROW 58391, 'ArticlePublicId is required.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@PublicationMessageIdCandidate, '')))) = 0
        THROW 58392, 'PublicationMessageIdCandidate is required.', 1;

    DECLARE
        @ViewCount BIGINT = 0,
        @LikeCount BIGINT = 0,
        @VisibleCommentCount BIGINT = 0,
        @CurrentViewCount BIGINT,
        @CurrentLikeCount BIGINT,
        @CurrentVisibleCommentCount BIGINT,
        @CurrentStatsVersion BIGINT,
        @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    SELECT
        @ViewCount = [ViewCount]
    FROM [interaction].[ArticleViewCount]
    WHERE [ArticlePublicId] = @ArticlePublicId;

    SELECT
        @LikeCount = COUNT_BIG(1)
    FROM [interaction].[ArticleLike]
    WHERE [ArticlePublicId] = @ArticlePublicId
      AND [IsActive] = 1;

    SELECT
        @VisibleCommentCount = COUNT_BIG(1)
    FROM [interaction].[Comment]
    WHERE [ArticlePublicId] = @ArticlePublicId
      AND [Status] = N'Visible';

    SELECT
        @CurrentViewCount = [ViewCount],
        @CurrentLikeCount = [LikeCount],
        @CurrentVisibleCommentCount = [VisibleCommentCount],
        @CurrentStatsVersion = [StatsVersion]
    FROM [interaction].[ArticleInteractionStats] WITH (UPDLOCK, HOLDLOCK)
    WHERE [ArticlePublicId] = @ArticlePublicId;

    IF @CurrentStatsVersion IS NULL
    BEGIN
        INSERT INTO [interaction].[ArticleInteractionStats]
        (
            [ArticlePublicId],
            [ViewCount],
            [LikeCount],
            [VisibleCommentCount],
            [StatsVersion],
            [LastMaterializedAtUtc],
            [LastPublishedMessageId],
            [LastPublishedAtUtc],
            [CreatedAtUtc],
            [UpdatedAtUtc]
        )
        VALUES
        (
            @ArticlePublicId,
            ISNULL(@ViewCount, 0),
            ISNULL(@LikeCount, 0),
            ISNULL(@VisibleCommentCount, 0),
            1,
            @NowUtc,
            @PublicationMessageIdCandidate,
            @NowUtc,
            @NowUtc,
            NULL
        );

        SET @SnapshotChanged = 1;
    END
    ELSE IF
    (
        @CurrentViewCount <> ISNULL(@ViewCount, 0)
        OR @CurrentLikeCount <> ISNULL(@LikeCount, 0)
        OR @CurrentVisibleCommentCount <> ISNULL(@VisibleCommentCount, 0)
    )
    BEGIN
        UPDATE [interaction].[ArticleInteractionStats]
        SET
            [ViewCount] = ISNULL(@ViewCount, 0),
            [LikeCount] = ISNULL(@LikeCount, 0),
            [VisibleCommentCount] = ISNULL(@VisibleCommentCount, 0),
            [StatsVersion] = [StatsVersion] + 1,
            [LastMaterializedAtUtc] = @NowUtc,
            [LastPublishedMessageId] = @PublicationMessageIdCandidate,
            [LastPublishedAtUtc] = @NowUtc,
            [UpdatedAtUtc] = @NowUtc
        WHERE [ArticlePublicId] = @ArticlePublicId;

        SET @SnapshotChanged = 1;
    END
    ELSE
    BEGIN
        UPDATE [interaction].[ArticleInteractionStats]
        SET
            [LastMaterializedAtUtc] = @NowUtc,
            [UpdatedAtUtc] = @NowUtc
        WHERE [ArticlePublicId] = @ArticlePublicId;
    END

    SELECT TOP (1)
        [ArticleInteractionStatsId],
        [ArticlePublicId],
        [ViewCount],
        [LikeCount],
        [VisibleCommentCount],
        [StatsVersion],
        [LastMaterializedAtUtc],
        [LastPublishedMessageId],
        [LastPublishedAtUtc],
        [CreatedAtUtc],
        [UpdatedAtUtc],
        @SnapshotChanged AS [SnapshotChanged]
    FROM [interaction].[ArticleInteractionStats]
    WHERE [ArticlePublicId] = @ArticlePublicId;
END
GO

/*
    Procedure: [interaction].[Interaction_ArticleViewCount_SelectPendingStatsMaterializationBatch]
    Purpose:
    - Select a bounded batch of articles whose accumulated accepted view count
      has not yet been reflected in the materialized public interaction stats snapshot.
    - Used by Interaction View Stats Materialization background processing.

    Notes:
    - [interaction].[ArticleViewCount] stores durable accepted-view accumulation state.
    - [interaction].[ArticleInteractionStats] stores the materialized public counter snapshot.
    - This procedure only selects pending article public ids.
    - It does not update stats and does not write outbox messages.
    - Materialization must re-read the latest counter state before publishing a snapshot.
*/
CREATE OR ALTER PROCEDURE [interaction].[Interaction_ArticleViewCount_SelectPendingStatsMaterializationBatch]
    @BatchSize INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @BatchSize < 1 OR @BatchSize > 500
    BEGIN
        THROW 58211, 'BatchSize must be between 1 and 500.', 1;
    END;

    SELECT TOP (@BatchSize)
        [v].[ArticlePublicId]
    FROM [interaction].[ArticleViewCount] AS [v]
    LEFT JOIN [interaction].[ArticleInteractionStats] AS [s]
        ON [s].[ArticlePublicId] = [v].[ArticlePublicId]
    WHERE [s].[ArticleInteractionStatsId] IS NULL
       OR [s].[ViewCount] <> [v].[ViewCount]
    ORDER BY
        [v].[UpdatedAtUtc] ASC,
        [v].[ArticleViewCountId] ASC;
END
GO
