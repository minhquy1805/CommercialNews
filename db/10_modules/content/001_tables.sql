/*
  File: db/10_modules/content/001_tables.sql
  Module: Content
  Purpose:
  - Create Content truth tables for CommercialNews V1.
  - Core OLTP truth model:
      * Category
      * Article
      * Tag
      * ArticleTag
      * ArticleRevision
      * ArticleLifecycleEvent
  - Support:
      * current article state
      * taxonomy ownership inside content
      * append-only edit history
      * append-only lifecycle / governance history
      * soft delete
      * optimistic concurrency via Version
  - Idempotent: safe to re-run.

  Notes:
  - SEO/read models/cache/search are NOT modeled here in 001_tables.sql.
  - Index-heavy tuning belongs in 010_indexes.sql.
  - Stored procedures belong in 020_procs.sql.
  - Media remains cross-module; CoverMediaId is stored as nullable reference only.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 54001, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

USE [CommercialNews];
GO

IF SCHEMA_ID(N'content') IS NULL
BEGIN
    THROW 54002, 'Schema [content] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

IF SCHEMA_ID(N'identity') IS NULL
BEGIN
    THROW 54003, 'Schema [identity] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

IF OBJECT_ID(N'[identity].[UserAccount]', N'U') IS NULL
BEGIN
    THROW 54004, 'Table [identity].[UserAccount] does not exist. Run identity/001_tables.sql first.', 1;
END
GO

/* =========================================================
   1) [content].[Category]
   ========================================================= */
IF OBJECT_ID(N'[content].[Category]', N'U') IS NULL
BEGIN
    CREATE TABLE [content].[Category]
    (
        [CategoryId]            BIGINT IDENTITY(1,1) NOT NULL,
        [PublicId]              CHAR(26)             NOT NULL, -- ULID

        [ParentCategoryId]      BIGINT               NULL,

        [Name]                  NVARCHAR(200)        NOT NULL,
        [NameNormalized]        NVARCHAR(200)        NOT NULL,
        [Description]           NVARCHAR(1000)       NULL,

        [IsActive]              BIT                  NOT NULL
            CONSTRAINT [DF_Category_IsActive] DEFAULT (1),

        [DisplayOrder]          INT                  NOT NULL
            CONSTRAINT [DF_Category_DisplayOrder] DEFAULT (0),

        [CreatedAt]             DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_Category_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt]             DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_Category_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

        [CreatedByUserId]       BIGINT               NULL,
        [UpdatedByUserId]       BIGINT               NULL,

        [IsDeleted]             BIT                  NOT NULL
            CONSTRAINT [DF_Category_IsDeleted] DEFAULT (0),
        [DeletedAt]             DATETIME2(3)         NULL,
        [DeletedByUserId]       BIGINT               NULL,

        [Version]               INT                  NOT NULL
            CONSTRAINT [DF_Category_Version] DEFAULT (1),

        CONSTRAINT [PK_Category]
            PRIMARY KEY CLUSTERED ([CategoryId] ASC),

        CONSTRAINT [UQ_Category_PublicId]
            UNIQUE ([PublicId]),

        CONSTRAINT [CK_Category_PublicId_Length]
            CHECK (LEN([PublicId]) = 26),

        CONSTRAINT [UQ_Category_NameNormalized]
            UNIQUE ([NameNormalized]),

        CONSTRAINT [FK_Category_ParentCategory]
            FOREIGN KEY ([ParentCategoryId])
            REFERENCES [content].[Category]([CategoryId]),

        CONSTRAINT [FK_Category_CreatedByUser]
            FOREIGN KEY ([CreatedByUserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [FK_Category_UpdatedByUser]
            FOREIGN KEY ([UpdatedByUserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [FK_Category_DeletedByUser]
            FOREIGN KEY ([DeletedByUserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [CK_Category_Name_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),

        CONSTRAINT [CK_Category_NameNormalized_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([NameNormalized]))) > 0),

        CONSTRAINT [CK_Category_DisplayOrder_NonNegative]
            CHECK ([DisplayOrder] >= 0),

        CONSTRAINT [CK_Category_Parent_NotSelf]
            CHECK ([ParentCategoryId] IS NULL OR [ParentCategoryId] <> [CategoryId]),

        CONSTRAINT [CK_Category_UpdatedAt]
            CHECK ([UpdatedAt] >= [CreatedAt]),

        CONSTRAINT [CK_Category_DeletedAt]
            CHECK ([DeletedAt] IS NULL OR [DeletedAt] >= [CreatedAt]),

        CONSTRAINT [CK_Category_DeletedByUser]
            CHECK (
                ([IsDeleted] = 0 AND [DeletedAt] IS NULL AND [DeletedByUserId] IS NULL)
                OR ([IsDeleted] = 1 AND [DeletedAt] IS NOT NULL)
            ),

        CONSTRAINT [CK_Category_Version_Positive]
            CHECK ([Version] > 0)
    );

    PRINT N'Created table: [content].[Category]';
END
ELSE
BEGIN
    PRINT N'Table exists: [content].[Category]';
END
GO

/* =========================================================
   2) [content].[Tag]
   ========================================================= */
IF OBJECT_ID(N'[content].[Tag]', N'U') IS NULL
BEGIN
    CREATE TABLE [content].[Tag]
    (
        [TagId]                 BIGINT IDENTITY(1,1) NOT NULL,
        [PublicId]              CHAR(26)             NOT NULL, -- ULID

        [Name]                  NVARCHAR(150)        NOT NULL,
        [NameNormalized]        NVARCHAR(150)        NOT NULL,
        [Description]           NVARCHAR(500)        NULL,

        [IsActive]              BIT                  NOT NULL
            CONSTRAINT [DF_Tag_IsActive] DEFAULT (1),

        [CreatedAt]             DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_Tag_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt]             DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_Tag_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

        [CreatedByUserId]       BIGINT               NULL,
        [UpdatedByUserId]       BIGINT               NULL,

        [IsDeleted]             BIT                  NOT NULL
            CONSTRAINT [DF_Tag_IsDeleted] DEFAULT (0),
        [DeletedAt]             DATETIME2(3)         NULL,
        [DeletedByUserId]       BIGINT               NULL,

        [Version]               INT                  NOT NULL
            CONSTRAINT [DF_Tag_Version] DEFAULT (1),

        CONSTRAINT [PK_Tag]
            PRIMARY KEY CLUSTERED ([TagId] ASC),

        CONSTRAINT [UQ_Tag_PublicId]
            UNIQUE ([PublicId]),

        CONSTRAINT [CK_Tag_PublicId_Length]
            CHECK (LEN([PublicId]) = 26),

        CONSTRAINT [UQ_Tag_NameNormalized]
            UNIQUE ([NameNormalized]),

        CONSTRAINT [FK_Tag_CreatedByUser]
            FOREIGN KEY ([CreatedByUserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [FK_Tag_UpdatedByUser]
            FOREIGN KEY ([UpdatedByUserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [FK_Tag_DeletedByUser]
            FOREIGN KEY ([DeletedByUserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [CK_Tag_Name_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),

        CONSTRAINT [CK_Tag_NameNormalized_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([NameNormalized]))) > 0),

        CONSTRAINT [CK_Tag_UpdatedAt]
            CHECK ([UpdatedAt] >= [CreatedAt]),

        CONSTRAINT [CK_Tag_DeletedAt]
            CHECK ([DeletedAt] IS NULL OR [DeletedAt] >= [CreatedAt]),

        CONSTRAINT [CK_Tag_DeletedByUser]
            CHECK (
                ([IsDeleted] = 0 AND [DeletedAt] IS NULL AND [DeletedByUserId] IS NULL)
                OR ([IsDeleted] = 1 AND [DeletedAt] IS NOT NULL)
            ),

        CONSTRAINT [CK_Tag_Version_Positive]
            CHECK ([Version] > 0)
    );

    PRINT N'Created table: [content].[Tag]';
END
ELSE
BEGIN
    PRINT N'Table exists: [content].[Tag]';
END
GO

/* =========================================================
   3) [content].[Article]
   ========================================================= */
IF OBJECT_ID(N'[content].[Article]', N'U') IS NULL
BEGIN
    CREATE TABLE [content].[Article]
    (
        [ArticleId]             BIGINT IDENTITY(1,1) NOT NULL,
        [ArticlePublicId]       CHAR(26)             NOT NULL, -- ULID

        [CategoryId]            BIGINT               NOT NULL,
        [AuthorUserId]          BIGINT               NOT NULL,

        [Title]                 NVARCHAR(300)        NOT NULL,
        [Summary]               NVARCHAR(1000)       NULL,
        [Body]                  NVARCHAR(MAX)        NOT NULL,

        [Status]                NVARCHAR(30)         NOT NULL
            CONSTRAINT [DF_Article_Status] DEFAULT (N'Draft'),

        [PublishedAt]           DATETIME2(3)         NULL,
        [UnpublishedAt]         DATETIME2(3)         NULL,
        [ArchivedAt]            DATETIME2(3)         NULL,

        [CoverMediaId]          BIGINT               NULL,

        [CreatedAt]             DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_Article_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt]             DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_Article_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

        [CreatedByUserId]       BIGINT               NULL,
        [UpdatedByUserId]       BIGINT               NULL,

        [IsDeleted]             BIT                  NOT NULL
            CONSTRAINT [DF_Article_IsDeleted] DEFAULT (0),
        [DeletedAt]             DATETIME2(3)         NULL,
        [DeletedByUserId]       BIGINT               NULL,

        [Version]               INT                  NOT NULL
            CONSTRAINT [DF_Article_Version] DEFAULT (1),

        CONSTRAINT [PK_Article]
            PRIMARY KEY CLUSTERED ([ArticleId] ASC),

        CONSTRAINT [UQ_Article_ArticlePublicId]
            UNIQUE ([ArticlePublicId]),

        CONSTRAINT [CK_Article_ArticlePublicId_Length]
            CHECK (LEN([ArticlePublicId]) = 26),

        CONSTRAINT [FK_Article_Category]
            FOREIGN KEY ([CategoryId])
            REFERENCES [content].[Category]([CategoryId]),

        CONSTRAINT [FK_Article_AuthorUser]
            FOREIGN KEY ([AuthorUserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [FK_Article_CreatedByUser]
            FOREIGN KEY ([CreatedByUserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [FK_Article_UpdatedByUser]
            FOREIGN KEY ([UpdatedByUserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [FK_Article_DeletedByUser]
            FOREIGN KEY ([DeletedByUserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [CK_Article_Title_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([Title]))) > 0),

        CONSTRAINT [CK_Article_Body_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([Body]))) > 0),

        CONSTRAINT [CK_Article_Status]
            CHECK ([Status] IN (N'Draft', N'Published', N'Archived')),

        CONSTRAINT [CK_Article_PublishedAt]
            CHECK (
                ([Status] <> N'Published')
                OR ([PublishedAt] IS NOT NULL)
            ),

        CONSTRAINT [CK_Article_ArchivedAt]
            CHECK (
                ([Status] <> N'Archived')
                OR ([ArchivedAt] IS NOT NULL)
            ),

        CONSTRAINT [CK_Article_UpdatedAt]
            CHECK ([UpdatedAt] >= [CreatedAt]),

        CONSTRAINT [CK_Article_PublishedAt_Order]
            CHECK ([PublishedAt] IS NULL OR [PublishedAt] >= [CreatedAt]),

        CONSTRAINT [CK_Article_UnpublishedAt_Order]
            CHECK ([UnpublishedAt] IS NULL OR [UnpublishedAt] >= [CreatedAt]),

        CONSTRAINT [CK_Article_ArchivedAt_Order]
            CHECK ([ArchivedAt] IS NULL OR [ArchivedAt] >= [CreatedAt]),

        CONSTRAINT [CK_Article_DeletedAt]
            CHECK ([DeletedAt] IS NULL OR [DeletedAt] >= [CreatedAt]),

        CONSTRAINT [CK_Article_DeletedByUser]
            CHECK (
                ([IsDeleted] = 0 AND [DeletedAt] IS NULL AND [DeletedByUserId] IS NULL)
                OR ([IsDeleted] = 1 AND [DeletedAt] IS NOT NULL)
            ),

        CONSTRAINT [CK_Article_Version_Positive]
            CHECK ([Version] > 0)
    );

    PRINT N'Created table: [content].[Article]';
END
ELSE
BEGIN
    PRINT N'Table exists: [content].[Article]';
END
GO

/* =========================================================
   4) [content].[ArticleTag]
   ========================================================= */
IF OBJECT_ID(N'[content].[ArticleTag]', N'U') IS NULL
BEGIN
    CREATE TABLE [content].[ArticleTag]
    (
        [ArticleId]             BIGINT               NOT NULL,
        [TagId]                 BIGINT               NOT NULL,

        [CreatedAt]             DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_ArticleTag_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [CreatedByUserId]       BIGINT               NULL,

        CONSTRAINT [PK_ArticleTag]
            PRIMARY KEY CLUSTERED ([ArticleId] ASC, [TagId] ASC),

        CONSTRAINT [FK_ArticleTag_Article]
            FOREIGN KEY ([ArticleId])
            REFERENCES [content].[Article]([ArticleId]),

        CONSTRAINT [FK_ArticleTag_Tag]
            FOREIGN KEY ([TagId])
            REFERENCES [content].[Tag]([TagId]),

        CONSTRAINT [FK_ArticleTag_CreatedByUser]
            FOREIGN KEY ([CreatedByUserId])
            REFERENCES [identity].[UserAccount]([UserId])
    );

    PRINT N'Created table: [content].[ArticleTag]';
END
ELSE
BEGIN
    PRINT N'Table exists: [content].[ArticleTag]';
END
GO

/* =========================================================
   5) [content].[ArticleRevision]
   ========================================================= */
IF OBJECT_ID(N'[content].[ArticleRevision]', N'U') IS NULL
BEGIN
    CREATE TABLE [content].[ArticleRevision]
    (
        [RevisionId]            BIGINT IDENTITY(1,1) NOT NULL,
        [ArticleId]             BIGINT               NOT NULL,

        [EditedAt]              DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_ArticleRevision_EditedAt] DEFAULT (SYSUTCDATETIME()),
        [EditedByUserId]        BIGINT               NOT NULL,
        [CorrelationId]         NVARCHAR(100)        NULL,
        [ChangeSummary]         NVARCHAR(300)        NULL,

        [OldTitle]              NVARCHAR(300)        NULL,
        [OldSummary]            NVARCHAR(1000)       NULL,
        [OldBody]               NVARCHAR(MAX)        NULL,

        [PatchJson]             NVARCHAR(MAX)        NULL,

        CONSTRAINT [PK_ArticleRevision]
            PRIMARY KEY CLUSTERED ([RevisionId] ASC),

        CONSTRAINT [FK_ArticleRevision_Article]
            FOREIGN KEY ([ArticleId])
            REFERENCES [content].[Article]([ArticleId]),

        CONSTRAINT [FK_ArticleRevision_EditedByUser]
            FOREIGN KEY ([EditedByUserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [CK_ArticleRevision_HasOldValueOrPatch]
            CHECK (
                [OldTitle] IS NOT NULL
                OR [OldSummary] IS NOT NULL
                OR [OldBody] IS NOT NULL
                OR [PatchJson] IS NOT NULL
            )
    );

    PRINT N'Created table: [content].[ArticleRevision]';
END
ELSE
BEGIN
    PRINT N'Table exists: [content].[ArticleRevision]';
END
GO

/* =========================================================
   6) [content].[ArticleLifecycleEvent]
   ========================================================= */
IF OBJECT_ID(N'[content].[ArticleLifecycleEvent]', N'U') IS NULL
BEGIN
    CREATE TABLE [content].[ArticleLifecycleEvent]
    (
        [EventId]                BIGINT IDENTITY(1,1) NOT NULL,
        [ArticleId]              BIGINT               NOT NULL,
        [ArticleVersion]         INT                  NOT NULL,

        [ActionType]             NVARCHAR(30)         NOT NULL,
        [FromStatus]             NVARCHAR(30)         NULL,
        [ToStatus]               NVARCHAR(30)         NULL,

        [Reason]                 NVARCHAR(500)        NULL,
        [ActorUserId]            BIGINT               NOT NULL,

        [OccurredAt]             DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_ArticleLifecycleEvent_OccurredAt] DEFAULT (SYSUTCDATETIME()),

        [CorrelationId]          NVARCHAR(100)        NULL,
        [MetadataJson]           NVARCHAR(MAX)        NULL,

        CONSTRAINT [PK_ArticleLifecycleEvent]
            PRIMARY KEY CLUSTERED ([EventId] ASC),

        CONSTRAINT [FK_ArticleLifecycleEvent_Article]
            FOREIGN KEY ([ArticleId])
            REFERENCES [content].[Article]([ArticleId]),

        CONSTRAINT [FK_ArticleLifecycleEvent_ActorUser]
            FOREIGN KEY ([ActorUserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [CK_ArticleLifecycleEvent_ArticleVersion_Positive]
            CHECK ([ArticleVersion] > 0),

        CONSTRAINT [CK_ArticleLifecycleEvent_ActionType]
            CHECK ([ActionType] IN (N'Publish', N'Unpublish', N'Archive', N'SoftDelete')),

        CONSTRAINT [CK_ArticleLifecycleEvent_FromStatus]
            CHECK ([FromStatus] IS NULL OR [FromStatus] IN (N'Draft', N'Published', N'Archived')),

        CONSTRAINT [CK_ArticleLifecycleEvent_ToStatus]
            CHECK ([ToStatus] IS NULL OR [ToStatus] IN (N'Draft', N'Published', N'Archived')),

        CONSTRAINT [CK_ArticleLifecycleEvent_UnpublishReason]
            CHECK (
                [ActionType] <> N'Unpublish'
                OR LEN(LTRIM(RTRIM(ISNULL([Reason], N'')))) > 0
            )
    );

    PRINT N'Created table: [content].[ArticleLifecycleEvent]';
END
ELSE
BEGIN
    PRINT N'Table exists: [content].[ArticleLifecycleEvent]';
END
GO
