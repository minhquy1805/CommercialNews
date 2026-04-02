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
    INCLUDE ([MediaId], [PublicId], [MediaType], [Url], [FileName], [MimeType], [CreatedBy]);
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
    INCLUDE ([MediaId], [PublicId], [Url], [FileName], [MimeType], [CreatedBy]);
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
    INCLUDE ([MediaId], [PublicId], [MediaType], [Url], [IsDeleted]);
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
    INCLUDE ([MediaId], [PublicId], [MediaType], [Url], [IsDeleted]);
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
    INCLUDE ([MediaId], [PublicId], [MediaType], [Url], [IsDeleted]);
END
GO

/*==============================================================*/
/* INDEXES: [media].[ArticleMedia]                              */
/*==============================================================*/

IF OBJECT_ID(N'[media].[ArticleMedia]', N'U') IS NULL
BEGIN
    THROW 53211, 'Table [media].[ArticleMedia] does not exist. Run 001_tables.sql first.', 1;
END
GO

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
    INCLUDE ([ArticleMediaId], [MediaId], [IsPrimary], [AltTextOverride], [Caption], [CreatedAt], [UpdatedAt]);
END
GO

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
    INCLUDE ([ArticleMediaId], [MediaId], [SortOrder], [AltTextOverride], [Caption], [CreatedAt], [UpdatedAt]);
END
GO

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
    INCLUDE ([ArticleMediaId], [IsPrimary], [SortOrder], [CreatedAt], [UpdatedAt]);
END
GO

/* Non-negotiable invariant: at most one active primary per article */
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

/* Optional helper index for active attachments only */
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
    INCLUDE ([ArticleMediaId], [MediaId], [IsPrimary], [AltTextOverride], [Caption])
    WHERE [IsDeleted] = 0;
END
GO

/*==============================================================*/
/* INDEXES: [media].[MediaVariant]                              */
/*==============================================================*/

IF OBJECT_ID(N'[media].[MediaVariant]', N'U') IS NULL
BEGIN
    THROW 53212, 'Table [media].[MediaVariant] does not exist. Run 001_tables.sql first.', 1;
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
    INCLUDE ([VariantId], [VariantType], [Url], [Width], [Height], [FileSizeBytes], [CreatedAt]);
END
GO