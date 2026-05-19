IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 53201, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

USE [CommercialNews];
GO

IF SCHEMA_ID(N'media') IS NULL
BEGIN
    THROW 53202, 'Schema [media] does not exist. Run bootstrap scripts first.', 1;
END
GO

SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

/*==============================================================*/
/* INDEXES: [media].[MediaAsset]                                */
/*==============================================================*/

IF OBJECT_ID(N'[media].[MediaAsset]', N'U') IS NULL
BEGIN
    THROW 53210, 'Table [media].[MediaAsset] does not exist. Run 001_tables.sql first.', 1;
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_MediaAsset_IsDeleted_CreatedAt'
      AND [object_id] = OBJECT_ID(N'[media].[MediaAsset]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_MediaAsset_IsDeleted_CreatedAt]
    ON [media].[MediaAsset] ([IsDeleted] ASC, [CreatedAt] DESC)
    INCLUDE
    (
        [MediaId],
        [PublicId],
        [MediaType],
        [Url],
        [FileName],
        [MimeType],
        [CreatedBy],
        [Version]
    );
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_MediaAsset_MediaType_IsDeleted_CreatedAt'
      AND [object_id] = OBJECT_ID(N'[media].[MediaAsset]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_MediaAsset_MediaType_IsDeleted_CreatedAt]
    ON [media].[MediaAsset] ([MediaType] ASC, [IsDeleted] ASC, [CreatedAt] DESC)
    INCLUDE
    (
        [MediaId],
        [PublicId],
        [Url],
        [FileName],
        [MimeType],
        [CreatedBy],
        [Version]
    );
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_MediaAsset_MimeType'
      AND [object_id] = OBJECT_ID(N'[media].[MediaAsset]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_MediaAsset_MimeType]
    ON [media].[MediaAsset] ([MimeType] ASC)
    INCLUDE
    (
        [MediaId],
        [PublicId],
        [MediaType],
        [Url],
        [IsDeleted],
        [CreatedAt],
        [Version]
    );
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_MediaAsset_ContentHash'
      AND [object_id] = OBJECT_ID(N'[media].[MediaAsset]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_MediaAsset_ContentHash]
    ON [media].[MediaAsset] ([ContentHash] ASC)
    INCLUDE
    (
        [MediaId],
        [PublicId],
        [MediaType],
        [Url],
        [IsDeleted],
        [CreatedAt],
        [Version]
    )
    WHERE [ContentHash] IS NOT NULL;
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_MediaAsset_CreatedBy_CreatedAt'
      AND [object_id] = OBJECT_ID(N'[media].[MediaAsset]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_MediaAsset_CreatedBy_CreatedAt]
    ON [media].[MediaAsset] ([CreatedBy] ASC, [CreatedAt] DESC)
    INCLUDE
    (
        [MediaId],
        [PublicId],
        [MediaType],
        [Url],
        [IsDeleted],
        [Version]
    )
    WHERE [CreatedBy] IS NOT NULL;
END
GO

/*==============================================================*/
/* INDEXES: [media].[ArticleMediaSet]                           */
/*==============================================================*/

IF OBJECT_ID(N'[media].[ArticleMediaSet]', N'U') IS NULL
BEGIN
    THROW 53211, 'Table [media].[ArticleMediaSet] does not exist. Run 001_tables.sql first.', 1;
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_ArticleMediaSet_UpdatedAt'
      AND [object_id] = OBJECT_ID(N'[media].[ArticleMediaSet]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleMediaSet_UpdatedAt]
    ON [media].[ArticleMediaSet] ([UpdatedAt] DESC)
    INCLUDE
    (
        [ArticleId],
        [Version],
        [UpdatedBy]
    );
END
GO

/*==============================================================*/
/* INDEXES: [media].[ArticleMedia]                              */
/*==============================================================*/

IF OBJECT_ID(N'[media].[ArticleMedia]', N'U') IS NULL
BEGIN
    THROW 53212, 'Table [media].[ArticleMedia] does not exist. Run 001_tables.sql first.', 1;
END
GO

/*
    Hot path:
    list active attachments for one article in stable order.
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_ArticleMedia_ArticleId_IsDeleted_SortOrder'
      AND [object_id] = OBJECT_ID(N'[media].[ArticleMedia]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleMedia_ArticleId_IsDeleted_SortOrder]
    ON [media].[ArticleMedia] ([ArticleId] ASC, [IsDeleted] ASC, [SortOrder] ASC)
    INCLUDE
    (
        [ArticleMediaId],
        [MediaId],
        [IsPrimary],
        [AltTextOverride],
        [Caption],
        [CreatedAt],
        [UpdatedAt],
        [Version]
    );
END
GO

/*
    Primary lookup support.

    The filtered unique index below enforces correctness.
    This index supports lookup patterns that include IsDeleted / IsPrimary.
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_ArticleMedia_ArticleId_IsDeleted_IsPrimary'
      AND [object_id] = OBJECT_ID(N'[media].[ArticleMedia]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleMedia_ArticleId_IsDeleted_IsPrimary]
    ON [media].[ArticleMedia] ([ArticleId] ASC, [IsDeleted] ASC, [IsPrimary] ASC)
    INCLUDE
    (
        [ArticleMediaId],
        [MediaId],
        [SortOrder],
        [AltTextOverride],
        [Caption],
        [CreatedAt],
        [UpdatedAt],
        [Version]
    );
END
GO

/*
    Reverse usage lookup:
    find all articles using a given media asset.
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_ArticleMedia_MediaId_IsDeleted_ArticleId'
      AND [object_id] = OBJECT_ID(N'[media].[ArticleMedia]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleMedia_MediaId_IsDeleted_ArticleId]
    ON [media].[ArticleMedia] ([MediaId] ASC, [IsDeleted] ASC, [ArticleId] ASC)
    INCLUDE
    (
        [ArticleMediaId],
        [IsPrimary],
        [SortOrder],
        [CreatedAt],
        [UpdatedAt],
        [Version]
    );
END
GO

/*
    Non-negotiable invariant:
    at most one active primary media per article.
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'UX_ArticleMedia_ActivePrimaryPerArticle'
      AND [object_id] = OBJECT_ID(N'[media].[ArticleMedia]')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UX_ArticleMedia_ActivePrimaryPerArticle]
    ON [media].[ArticleMedia] ([ArticleId] ASC)
    WHERE [IsPrimary] = 1 AND [IsDeleted] = 0;
END
GO

/*
    Optional helper index:
    active attachments only, optimized for read composition / admin display.
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_ArticleMedia_ActiveByArticleId'
      AND [object_id] = OBJECT_ID(N'[media].[ArticleMedia]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleMedia_ActiveByArticleId]
    ON [media].[ArticleMedia] ([ArticleId] ASC, [SortOrder] ASC)
    INCLUDE
    (
        [ArticleMediaId],
        [MediaId],
        [IsPrimary],
        [AltTextOverride],
        [Caption],
        [Version],
        [UpdatedAt]
    )
    WHERE [IsDeleted] = 0;
END
GO

/*
    Optional helper:
    list recently changed attachment rows for ops/debug/reconciliation.
*/
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_ArticleMedia_UpdatedAt'
      AND [object_id] = OBJECT_ID(N'[media].[ArticleMedia]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ArticleMedia_UpdatedAt]
    ON [media].[ArticleMedia] ([UpdatedAt] DESC)
    INCLUDE
    (
        [ArticleMediaId],
        [ArticleId],
        [MediaId],
        [IsDeleted],
        [IsPrimary],
        [Version]
    );
END
GO

/*==============================================================*/
/* INDEXES: [media].[MediaVariant] -- V2 HOOK                   */
/*==============================================================*/

IF OBJECT_ID(N'[media].[MediaVariant]', N'U') IS NULL
BEGIN
    THROW 53213, 'Table [media].[MediaVariant] does not exist. Run 001_tables.sql first.', 1;
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_MediaVariant_MediaId'
      AND [object_id] = OBJECT_ID(N'[media].[MediaVariant]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_MediaVariant_MediaId]
    ON [media].[MediaVariant] ([MediaId] ASC)
    INCLUDE
    (
        [VariantId],
        [VariantType],
        [Url],
        [Width],
        [Height],
        [FileSizeBytes],
        [CreatedAt]
    );
END
GO