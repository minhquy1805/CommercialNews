/*
  File: db/10_modules/interaction/020_procs.sql
  Module: Interaction
  Purpose:
  - Create stored procedures for Interaction truth and derived model in CommercialNews V1.
  - Support:
      * ArticleViewEvent signal persistence / counting / retention cleanup
      * ArticleLike truth lookup / insert / activate / deactivate / count
      * Comment create / read / paging / update / soft delete / count
      * ArticleInteractionStats lookup / upsert
  - Idempotent: safe to re-run.

  Notes:
  - Truth remains in [interaction] tables.
  - App layer should still orchestrate transaction boundaries where multiple procs
    must succeed atomically (for example: like truth change + outbox write).
  - ArticleInteractionStats is derived state, not correctness truth.
  - Public read model / projections / cache / async aggregation are outside this file.
*/

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

/* =========================================================
   ARTICLE VIEW EVENT
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_ArticleViewEvent_Insert]
    @ArticleId       BIGINT,
    @UserId          BIGINT = NULL,
    @VisitorKey      NVARCHAR(100) = NULL,
    @IpAddress       NVARCHAR(64) = NULL,
    @UserAgent       NVARCHAR(512) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 58210, 'ArticleId must be > 0.', 1;

    IF @UserId IS NOT NULL AND @UserId <= 0
        THROW 58211, 'UserId must be > 0 when provided.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@VisitorKey, N'')))) = 0
        SET @VisitorKey = NULL;

    IF LEN(LTRIM(RTRIM(ISNULL(@IpAddress, N'')))) = 0
        SET @IpAddress = NULL;

    IF LEN(LTRIM(RTRIM(ISNULL(@UserAgent, N'')))) = 0
        SET @UserAgent = NULL;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [content].[Article]
        WHERE [ArticleId] = @ArticleId
          AND [IsDeleted] = 0
    )
        THROW 58212, 'Article does not exist or was deleted.', 1;

    IF @UserId IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1
           FROM [identity].[UserAccount]
           WHERE [UserId] = @UserId
       )
        THROW 58213, 'User does not exist.', 1;

    INSERT INTO [interaction].[ArticleViewEvent]
    (
        [ArticleId],
        [UserId],
        [VisitorKey],
        [IpAddress],
        [UserAgent]
    )
    VALUES
    (
        @ArticleId,
        @UserId,
        @VisitorKey,
        @IpAddress,
        @UserAgent
    );

    SELECT TOP (1) *
    FROM [interaction].[ArticleViewEvent]
    WHERE [ArticleViewEventId] = SCOPE_IDENTITY();
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_ArticleViewEvent_GetRecordCountByArticleId]
    @ArticleId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 58220, 'ArticleId must be > 0.', 1;

    SELECT COUNT_BIG(1) AS [RecordCount]
    FROM [interaction].[ArticleViewEvent]
    WHERE [ArticleId] = @ArticleId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_ArticleViewEvent_DeleteBeforeDate]
    @DeleteBeforeUtc  DATETIME2(3),
    @AffectedRows     INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @AffectedRows = 0;

    IF @DeleteBeforeUtc IS NULL
        THROW 58230, 'DeleteBeforeUtc is required.', 1;

    DELETE FROM [interaction].[ArticleViewEvent]
    WHERE [ViewedAt] < @DeleteBeforeUtc;

    SET @AffectedRows = @@ROWCOUNT;
END
GO

/* =========================================================
   ARTICLE LIKE
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_ArticleLike_SelectByArticleIdAndUserId]
    @ArticleId BIGINT,
    @UserId    BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 58240, 'ArticleId must be > 0.', 1;

    IF @UserId IS NULL OR @UserId <= 0
        THROW 58241, 'UserId must be > 0.', 1;

    SELECT TOP (1) *
    FROM [interaction].[ArticleLike]
    WHERE [ArticleId] = @ArticleId
      AND [UserId] = @UserId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_ArticleLike_Insert]
    @ArticleId BIGINT,
    @UserId    BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 58250, 'ArticleId must be > 0.', 1;

    IF @UserId IS NULL OR @UserId <= 0
        THROW 58251, 'UserId must be > 0.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [content].[Article]
        WHERE [ArticleId] = @ArticleId
          AND [IsDeleted] = 0
    )
        THROW 58252, 'Article does not exist or was deleted.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [identity].[UserAccount]
        WHERE [UserId] = @UserId
    )
        THROW 58253, 'User does not exist.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [interaction].[ArticleLike]
        WHERE [ArticleId] = @ArticleId
          AND [UserId] = @UserId
    )
        THROW 58254, 'ArticleLike already exists for this article and user.', 1;

    INSERT INTO [interaction].[ArticleLike]
    (
        [ArticleId],
        [UserId],
        [IsActive],
        [LikedAt]
    )
    VALUES
    (
        @ArticleId,
        @UserId,
        1,
        SYSUTCDATETIME()
    );

    SELECT TOP (1) *
    FROM [interaction].[ArticleLike]
    WHERE [ArticleLikeId] = SCOPE_IDENTITY();
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_ArticleLike_Activate]
    @ArticleId        BIGINT,
    @UserId           BIGINT,
    @AffectedRows     INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @AffectedRows = 0;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 58260, 'ArticleId must be > 0.', 1;

    IF @UserId IS NULL OR @UserId <= 0
        THROW 58261, 'UserId must be > 0.', 1;

    UPDATE [interaction].[ArticleLike]
    SET
        [IsActive] = 1,
        [LikedAt] = SYSUTCDATETIME(),
        [UnlikedAt] = NULL,
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [ArticleId] = @ArticleId
      AND [UserId] = @UserId
      AND [IsActive] = 0;

    SET @AffectedRows = @@ROWCOUNT;

    IF @AffectedRows = 0
        RETURN;

    SELECT TOP (1) *
    FROM [interaction].[ArticleLike]
    WHERE [ArticleId] = @ArticleId
      AND [UserId] = @UserId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_ArticleLike_Deactivate]
    @ArticleId        BIGINT,
    @UserId           BIGINT,
    @AffectedRows     INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @AffectedRows = 0;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 58270, 'ArticleId must be > 0.', 1;

    IF @UserId IS NULL OR @UserId <= 0
        THROW 58271, 'UserId must be > 0.', 1;

    UPDATE [interaction].[ArticleLike]
    SET
        [IsActive] = 0,
        [UnlikedAt] = SYSUTCDATETIME(),
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [ArticleId] = @ArticleId
      AND [UserId] = @UserId
      AND [IsActive] = 1;

    SET @AffectedRows = @@ROWCOUNT;

    IF @AffectedRows = 0
        RETURN;

    SELECT TOP (1) *
    FROM [interaction].[ArticleLike]
    WHERE [ArticleId] = @ArticleId
      AND [UserId] = @UserId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_ArticleLike_GetActiveCountByArticleId]
    @ArticleId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 58280, 'ArticleId must be > 0.', 1;

    SELECT COUNT_BIG(1) AS [ActiveCount]
    FROM [interaction].[ArticleLike]
    WHERE [ArticleId] = @ArticleId
      AND [IsActive] = 1;
END
GO

/* =========================================================
   COMMENT
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_Comment_Insert]
    @ArticleId          BIGINT,
    @UserId             BIGINT,
    @ParentCommentId    BIGINT = NULL,
    @Content            NVARCHAR(2000)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 58290, 'ArticleId must be > 0.', 1;

    IF @UserId IS NULL OR @UserId <= 0
        THROW 58291, 'UserId must be > 0.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Content, N'')))) = 0
        THROW 58292, 'Content is required.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [content].[Article]
        WHERE [ArticleId] = @ArticleId
          AND [IsDeleted] = 0
    )
        THROW 58293, 'Article does not exist or was deleted.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [identity].[UserAccount]
        WHERE [UserId] = @UserId
    )
        THROW 58294, 'User does not exist.', 1;

    IF @ParentCommentId IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1
           FROM [interaction].[Comment]
           WHERE [CommentId] = @ParentCommentId
             AND [ArticleId] = @ArticleId
             AND [Status] <> N'Deleted'
       )
        THROW 58295, 'Parent comment does not exist, belongs to another article, or was deleted.', 1;

    INSERT INTO [interaction].[Comment]
    (
        [ArticleId],
        [UserId],
        [ParentCommentId],
        [Content],
        [Status]
    )
    VALUES
    (
        @ArticleId,
        @UserId,
        @ParentCommentId,
        @Content,
        N'Visible'
    );

    SELECT TOP (1) *
    FROM [interaction].[Comment]
    WHERE [CommentId] = SCOPE_IDENTITY();
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_Comment_SelectById]
    @CommentId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    IF @CommentId IS NULL OR @CommentId <= 0
        THROW 58300, 'CommentId must be > 0.', 1;

    SELECT TOP (1) *
    FROM [interaction].[Comment]
    WHERE [CommentId] = @CommentId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_Comment_SelectVisibleByArticleId]
    @ArticleId          BIGINT,
    @Skip               INT = 0,
    @Take               INT = 20,
    @ParentCommentId    BIGINT = NULL,
    @SortBy             NVARCHAR(30) = N'CreatedAt',
    @SortDirection      NVARCHAR(4) = N'DESC'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 58310, 'ArticleId must be > 0.', 1;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;
    IF @Take > 200 SET @Take = 200;

    IF @SortBy NOT IN (N'CreatedAt', N'CommentId')
        SET @SortBy = N'CreatedAt';

    IF UPPER(@SortDirection) NOT IN (N'ASC', N'DESC')
        SET @SortDirection = N'DESC';

    ;WITH [Filtered] AS
    (
        SELECT
            [c].[CommentId],
            [c].[ArticleId],
            [c].[UserId],
            [c].[ParentCommentId],
            [c].[Content],
            [c].[Status],
            [c].[CreatedAt],
            [c].[UpdatedAt],
            [c].[DeletedAt],
            [c].[DeletedByUserId],
            [c].[EditCount],
            COUNT(1) OVER() AS [TotalCount]
        FROM [interaction].[Comment] AS [c]
        WHERE [c].[ArticleId] = @ArticleId
          AND [c].[Status] = N'Visible'
          AND
          (
              (@ParentCommentId IS NULL AND [c].[ParentCommentId] IS NULL)
              OR [c].[ParentCommentId] = @ParentCommentId
          )
    )
    SELECT *
    FROM [Filtered]
    ORDER BY
        CASE WHEN @SortBy = N'CreatedAt' AND @SortDirection = N'ASC'  THEN [CreatedAt] END ASC,
        CASE WHEN @SortBy = N'CreatedAt' AND @SortDirection = N'DESC' THEN [CreatedAt] END DESC,
        CASE WHEN @SortBy = N'CommentId' AND @SortDirection = N'ASC'  THEN [CommentId] END ASC,
        CASE WHEN @SortBy = N'CommentId' AND @SortDirection = N'DESC' THEN [CommentId] END DESC,
        [CommentId] DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_Comment_Update]
    @CommentId        BIGINT,
    @Content          NVARCHAR(2000),
    @ExpectedUserId   BIGINT = NULL,
    @AffectedRows     INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @AffectedRows = 0;

    IF @CommentId IS NULL OR @CommentId <= 0
        THROW 58320, 'CommentId must be > 0.', 1;

    IF LEN(LTRIM(RTRIM(ISNULL(@Content, N'')))) = 0
        THROW 58321, 'Content is required.', 1;

    UPDATE [interaction].[Comment]
    SET
        [Content] = @Content,
        [UpdatedAt] = SYSUTCDATETIME(),
        [EditCount] = [EditCount] + 1
    WHERE [CommentId] = @CommentId
      AND [Status] <> N'Deleted'
      AND (@ExpectedUserId IS NULL OR [UserId] = @ExpectedUserId);

    SET @AffectedRows = @@ROWCOUNT;

    IF @AffectedRows = 0
        RETURN;

    SELECT TOP (1) *
    FROM [interaction].[Comment]
    WHERE [CommentId] = @CommentId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_Comment_SoftDelete]
    @CommentId          BIGINT,
    @DeletedByUserId    BIGINT = NULL,
    @ExpectedUserId     BIGINT = NULL,
    @AffectedRows       INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @AffectedRows = 0;

    IF @CommentId IS NULL OR @CommentId <= 0
        THROW 58330, 'CommentId must be > 0.', 1;

    IF @DeletedByUserId IS NOT NULL AND @DeletedByUserId <= 0
        THROW 58331, 'DeletedByUserId must be > 0 when provided.', 1;

    UPDATE [interaction].[Comment]
    SET
        [Status] = N'Deleted',
        [DeletedAt] = SYSUTCDATETIME(),
        [DeletedByUserId] = @DeletedByUserId,
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [CommentId] = @CommentId
      AND [Status] <> N'Deleted'
      AND (@ExpectedUserId IS NULL OR [UserId] = @ExpectedUserId);

    SET @AffectedRows = @@ROWCOUNT;

    IF @AffectedRows = 0
        RETURN;

    SELECT TOP (1) *
    FROM [interaction].[Comment]
    WHERE [CommentId] = @CommentId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_Comment_GetVisibleCountByArticleId]
    @ArticleId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 58340, 'ArticleId must be > 0.', 1;

    SELECT COUNT_BIG(1) AS [VisibleCount]
    FROM [interaction].[Comment]
    WHERE [ArticleId] = @ArticleId
      AND [Status] = N'Visible';
END
GO

/* =========================================================
   ARTICLE INTERACTION STATS
   ========================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_ArticleInteractionStats_SelectByArticleId]
    @ArticleId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 58350, 'ArticleId must be > 0.', 1;

    SELECT TOP (1) *
    FROM [interaction].[ArticleInteractionStats]
    WHERE [ArticleId] = @ArticleId;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [interaction].[Interaction_ArticleInteractionStats_Upsert]
    @ArticleId          BIGINT,
    @ViewsTotal         BIGINT,
    @LikesTotal         BIGINT,
    @CommentsTotal      BIGINT,
    @PopularityScore    DECIMAL(18,4) = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ArticleId IS NULL OR @ArticleId <= 0
        THROW 58360, 'ArticleId must be > 0.', 1;

    IF @ViewsTotal IS NULL OR @ViewsTotal < 0
        THROW 58361, 'ViewsTotal must be >= 0.', 1;

    IF @LikesTotal IS NULL OR @LikesTotal < 0
        THROW 58362, 'LikesTotal must be >= 0.', 1;

    IF @CommentsTotal IS NULL OR @CommentsTotal < 0
        THROW 58363, 'CommentsTotal must be >= 0.', 1;

    IF @PopularityScore IS NULL OR @PopularityScore < 0
        THROW 58364, 'PopularityScore must be >= 0.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [content].[Article]
        WHERE [ArticleId] = @ArticleId
          AND [IsDeleted] = 0
    )
        THROW 58365, 'Article does not exist or was deleted.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [interaction].[ArticleInteractionStats]
        WHERE [ArticleId] = @ArticleId
    )
    BEGIN
        UPDATE [interaction].[ArticleInteractionStats]
        SET
            [ViewsTotal] = @ViewsTotal,
            [LikesTotal] = @LikesTotal,
            [CommentsTotal] = @CommentsTotal,
            [PopularityScore] = @PopularityScore,
            [UpdatedAt] = SYSUTCDATETIME(),
            [LastAggregatedAt] = SYSUTCDATETIME()
        WHERE [ArticleId] = @ArticleId;
    END
    ELSE
    BEGIN
        INSERT INTO [interaction].[ArticleInteractionStats]
        (
            [ArticleId],
            [ViewsTotal],
            [LikesTotal],
            [CommentsTotal],
            [PopularityScore],
            [UpdatedAt],
            [LastAggregatedAt]
        )
        VALUES
        (
            @ArticleId,
            @ViewsTotal,
            @LikesTotal,
            @CommentsTotal,
            @PopularityScore,
            SYSUTCDATETIME(),
            SYSUTCDATETIME()
        );
    END

    SELECT TOP (1) *
    FROM [interaction].[ArticleInteractionStats]
    WHERE [ArticleId] = @ArticleId;
END
GO