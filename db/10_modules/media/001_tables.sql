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

IF SCHEMA_ID(N'content') IS NULL
BEGIN
    THROW 53203, 'Schema [content] does not exist. Run bootstrap scripts first.', 1;
END
GO

IF SCHEMA_ID(N'identity') IS NULL
BEGIN
    THROW 53204, 'Schema [identity] does not exist. Run bootstrap scripts first.', 1;
END
GO

SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

/*==============================================================*/
/* DROP EXISTING TABLES - CHILDREN FIRST                         */
/*==============================================================*/
IF OBJECT_ID(N'[media].[MediaVariant]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [media].[MediaVariant];
END
GO

IF OBJECT_ID(N'[media].[ArticleMedia]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [media].[ArticleMedia];
END
GO

IF OBJECT_ID(N'[media].[ArticleMediaSet]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [media].[ArticleMediaSet];
END
GO

IF OBJECT_ID(N'[media].[MediaAsset]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [media].[MediaAsset];
END
GO

/*==============================================================*/
/* TABLE: [media].[MediaAsset]                                  */
/*==============================================================*/
CREATE TABLE [media].[MediaAsset]
(
    [MediaId]            BIGINT IDENTITY(1,1) NOT NULL,
    [PublicId]           VARCHAR(26) NOT NULL,

    [StorageProvider]    VARCHAR(30) NOT NULL
        CONSTRAINT [DF_MediaAsset_StorageProvider] DEFAULT ('local'),

    [Url]                NVARCHAR(800) NOT NULL,
    [StoragePath]        NVARCHAR(800) NULL,
    [FileName]           NVARCHAR(255) NULL,

    [MediaType]          VARCHAR(20) NOT NULL,
    [MimeType]           NVARCHAR(100) NULL,

    [FileSizeBytes]      BIGINT NULL,
    [Width]              INT NULL,
    [Height]             INT NULL,
    [DurationSeconds]    INT NULL,

    [AltText]            NVARCHAR(300) NULL,
    [MetadataJson]       NVARCHAR(MAX) NULL,
    [ContentHash]        VARBINARY(32) NULL,

    [Version]            INT NOT NULL
        CONSTRAINT [DF_MediaAsset_Version] DEFAULT (1),

    [CreatedAt]          DATETIME2(3) NOT NULL
        CONSTRAINT [DF_MediaAsset_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]          BIGINT NULL,

    [UpdatedAt]          DATETIME2(3) NOT NULL
        CONSTRAINT [DF_MediaAsset_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedBy]          BIGINT NULL,

    [IsDeleted]          BIT NOT NULL
        CONSTRAINT [DF_MediaAsset_IsDeleted] DEFAULT (0),
    [DeletedAt]          DATETIME2(3) NULL,
    [DeletedBy]          BIGINT NULL,
    [RestoreUntil]       DATETIME2(3) NULL,
    [RestoredAt]         DATETIME2(3) NULL,
    [RestoredBy]         BIGINT NULL,

    CONSTRAINT [PK_MediaAsset] PRIMARY KEY CLUSTERED ([MediaId] ASC),

    CONSTRAINT [UQ_MediaAsset_PublicId] UNIQUE ([PublicId]),

    CONSTRAINT [CK_MediaAsset_PublicId_NotBlank]
        CHECK (LEN(LTRIM(RTRIM([PublicId]))) > 0),

    CONSTRAINT [CK_MediaAsset_StorageProvider_NotBlank]
        CHECK (LEN(LTRIM(RTRIM([StorageProvider]))) > 0),

    CONSTRAINT [CK_MediaAsset_Url_NotBlank]
        CHECK (LEN(LTRIM(RTRIM([Url]))) > 0),

    CONSTRAINT [CK_MediaAsset_MediaType_Allowed]
        CHECK ([MediaType] IN ('Image', 'Video', 'File')),

    CONSTRAINT [CK_MediaAsset_FileSizeBytes_NonNegative]
        CHECK ([FileSizeBytes] IS NULL OR [FileSizeBytes] >= 0),

    CONSTRAINT [CK_MediaAsset_Width_NonNegative]
        CHECK ([Width] IS NULL OR [Width] >= 0),

    CONSTRAINT [CK_MediaAsset_Height_NonNegative]
        CHECK ([Height] IS NULL OR [Height] >= 0),

    CONSTRAINT [CK_MediaAsset_DurationSeconds_NonNegative]
        CHECK ([DurationSeconds] IS NULL OR [DurationSeconds] >= 0),

    CONSTRAINT [CK_MediaAsset_Version_Positive]
        CHECK ([Version] >= 1),

    CONSTRAINT [CK_MediaAsset_DeleteColumns_Consistency]
        CHECK
        (
            ([IsDeleted] = 0 AND [DeletedAt] IS NULL AND [DeletedBy] IS NULL)
            OR
            ([IsDeleted] = 1 AND [DeletedAt] IS NOT NULL)
        ),

    CONSTRAINT [CK_MediaAsset_RestoreUntil_WhenDeleted]
        CHECK
        (
            [IsDeleted] = 0
            OR [RestoreUntil] IS NULL
            OR [RestoreUntil] >= [DeletedAt]
        ),

    CONSTRAINT [CK_MediaAsset_RestoreColumns_Consistency]
        CHECK
        (
            ([RestoredAt] IS NULL AND [RestoredBy] IS NULL)
            OR
            ([RestoredAt] IS NOT NULL)
        )
);
GO

/* Optional cross-module actor FKs */
IF OBJECT_ID(N'[identity].[UserAccount]', N'U') IS NOT NULL
BEGIN
    ALTER TABLE [media].[MediaAsset]
    ADD CONSTRAINT [FK_MediaAsset_CreatedBy_UserAccount]
        FOREIGN KEY ([CreatedBy]) REFERENCES [identity].[UserAccount]([UserId]);

    ALTER TABLE [media].[MediaAsset]
    ADD CONSTRAINT [FK_MediaAsset_UpdatedBy_UserAccount]
        FOREIGN KEY ([UpdatedBy]) REFERENCES [identity].[UserAccount]([UserId]);

    ALTER TABLE [media].[MediaAsset]
    ADD CONSTRAINT [FK_MediaAsset_DeletedBy_UserAccount]
        FOREIGN KEY ([DeletedBy]) REFERENCES [identity].[UserAccount]([UserId]);

    ALTER TABLE [media].[MediaAsset]
    ADD CONSTRAINT [FK_MediaAsset_RestoredBy_UserAccount]
        FOREIGN KEY ([RestoredBy]) REFERENCES [identity].[UserAccount]([UserId]);
END
GO

/*==============================================================*/
/* TABLE: [media].[ArticleMediaSet]                             */
/*==============================================================*/
CREATE TABLE [media].[ArticleMediaSet]
(
    [ArticleId]          BIGINT NOT NULL,

    [Version]            INT NOT NULL
        CONSTRAINT [DF_ArticleMediaSet_Version] DEFAULT (0),

    [CreatedAt]          DATETIME2(3) NOT NULL
        CONSTRAINT [DF_ArticleMediaSet_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]          BIGINT NULL,

    [UpdatedAt]          DATETIME2(3) NOT NULL
        CONSTRAINT [DF_ArticleMediaSet_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedBy]          BIGINT NULL,

    CONSTRAINT [PK_ArticleMediaSet] PRIMARY KEY CLUSTERED ([ArticleId] ASC),

    CONSTRAINT [CK_ArticleMediaSet_Version_NonNegative]
        CHECK ([Version] >= 0)
);
GO

/* Optional cross-module FKs for ArticleMediaSet */
IF OBJECT_ID(N'[content].[Article]', N'U') IS NOT NULL
BEGIN
    ALTER TABLE [media].[ArticleMediaSet]
    ADD CONSTRAINT [FK_ArticleMediaSet_Article]
        FOREIGN KEY ([ArticleId]) REFERENCES [content].[Article]([ArticleId]);
END
GO

IF OBJECT_ID(N'[identity].[UserAccount]', N'U') IS NOT NULL
BEGIN
    ALTER TABLE [media].[ArticleMediaSet]
    ADD CONSTRAINT [FK_ArticleMediaSet_CreatedBy_UserAccount]
        FOREIGN KEY ([CreatedBy]) REFERENCES [identity].[UserAccount]([UserId]);

    ALTER TABLE [media].[ArticleMediaSet]
    ADD CONSTRAINT [FK_ArticleMediaSet_UpdatedBy_UserAccount]
        FOREIGN KEY ([UpdatedBy]) REFERENCES [identity].[UserAccount]([UserId]);
END
GO

/*==============================================================*/
/* TABLE: [media].[ArticleMedia]                                */
/*==============================================================*/
CREATE TABLE [media].[ArticleMedia]
(
    [ArticleMediaId]     BIGINT IDENTITY(1,1) NOT NULL,

    [ArticleId]          BIGINT NOT NULL,
    [MediaId]            BIGINT NOT NULL,

    [SortOrder]          INT NOT NULL
        CONSTRAINT [DF_ArticleMedia_SortOrder] DEFAULT (0),
    [IsPrimary]          BIT NOT NULL
        CONSTRAINT [DF_ArticleMedia_IsPrimary] DEFAULT (0),

    [AltTextOverride]    NVARCHAR(300) NULL,
    [Caption]            NVARCHAR(300) NULL,

    [Version]            INT NOT NULL
        CONSTRAINT [DF_ArticleMedia_Version] DEFAULT (1),

    [CreatedAt]          DATETIME2(3) NOT NULL
        CONSTRAINT [DF_ArticleMedia_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]          BIGINT NULL,

    [UpdatedAt]          DATETIME2(3) NOT NULL
        CONSTRAINT [DF_ArticleMedia_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedBy]          BIGINT NULL,

    [IsDeleted]          BIT NOT NULL
        CONSTRAINT [DF_ArticleMedia_IsDeleted] DEFAULT (0),
    [DeletedAt]          DATETIME2(3) NULL,
    [DeletedBy]          BIGINT NULL,

    CONSTRAINT [PK_ArticleMedia] PRIMARY KEY CLUSTERED ([ArticleMediaId] ASC),

    CONSTRAINT [UQ_ArticleMedia_ArticleId_MediaId] UNIQUE ([ArticleId], [MediaId]),

    CONSTRAINT [CK_ArticleMedia_SortOrder_NonNegative]
        CHECK ([SortOrder] >= 0),

    CONSTRAINT [CK_ArticleMedia_Version_Positive]
        CHECK ([Version] >= 1),

    CONSTRAINT [CK_ArticleMedia_Primary_NotDeleted]
        CHECK (NOT ([IsPrimary] = 1 AND [IsDeleted] = 1)),

    CONSTRAINT [CK_ArticleMedia_DeleteColumns_Consistency]
        CHECK
        (
            ([IsDeleted] = 0 AND [DeletedAt] IS NULL AND [DeletedBy] IS NULL)
            OR
            ([IsDeleted] = 1 AND [DeletedAt] IS NOT NULL)
        ),

    CONSTRAINT [FK_ArticleMedia_ArticleMediaSet]
        FOREIGN KEY ([ArticleId]) REFERENCES [media].[ArticleMediaSet]([ArticleId]),

    CONSTRAINT [FK_ArticleMedia_MediaAsset]
        FOREIGN KEY ([MediaId]) REFERENCES [media].[MediaAsset]([MediaId])
);
GO

IF OBJECT_ID(N'[identity].[UserAccount]', N'U') IS NOT NULL
BEGIN
    ALTER TABLE [media].[ArticleMedia]
    ADD CONSTRAINT [FK_ArticleMedia_CreatedBy_UserAccount]
        FOREIGN KEY ([CreatedBy]) REFERENCES [identity].[UserAccount]([UserId]);

    ALTER TABLE [media].[ArticleMedia]
    ADD CONSTRAINT [FK_ArticleMedia_UpdatedBy_UserAccount]
        FOREIGN KEY ([UpdatedBy]) REFERENCES [identity].[UserAccount]([UserId]);

    ALTER TABLE [media].[ArticleMedia]
    ADD CONSTRAINT [FK_ArticleMedia_DeletedBy_UserAccount]
        FOREIGN KEY ([DeletedBy]) REFERENCES [identity].[UserAccount]([UserId]);
END
GO

/*==============================================================*/
/* TABLE: [media].[MediaVariant]  -- V2 HOOK                    */
/*==============================================================*/
CREATE TABLE [media].[MediaVariant]
(
    [VariantId]          BIGINT IDENTITY(1,1) NOT NULL,
    [MediaId]            BIGINT NOT NULL,

    [VariantType]        VARCHAR(30) NOT NULL,
    [Url]                NVARCHAR(800) NOT NULL,

    [Width]              INT NULL,
    [Height]             INT NULL,
    [FileSizeBytes]      BIGINT NULL,

    [CreatedAt]          DATETIME2(3) NOT NULL
        CONSTRAINT [DF_MediaVariant_CreatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_MediaVariant] PRIMARY KEY CLUSTERED ([VariantId] ASC),

    CONSTRAINT [UQ_MediaVariant_MediaId_VariantType] UNIQUE ([MediaId], [VariantType]),

    CONSTRAINT [CK_MediaVariant_VariantType_NotBlank]
        CHECK (LEN(LTRIM(RTRIM([VariantType]))) > 0),

    CONSTRAINT [CK_MediaVariant_Url_NotBlank]
        CHECK (LEN(LTRIM(RTRIM([Url]))) > 0),

    CONSTRAINT [CK_MediaVariant_Width_NonNegative]
        CHECK ([Width] IS NULL OR [Width] >= 0),

    CONSTRAINT [CK_MediaVariant_Height_NonNegative]
        CHECK ([Height] IS NULL OR [Height] >= 0),

    CONSTRAINT [CK_MediaVariant_FileSizeBytes_NonNegative]
        CHECK ([FileSizeBytes] IS NULL OR [FileSizeBytes] >= 0),

    CONSTRAINT [FK_MediaVariant_MediaAsset]
        FOREIGN KEY ([MediaId]) REFERENCES [media].[MediaAsset]([MediaId])
);
GO