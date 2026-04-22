/*
  File: db/10_modules/identity/001_tables.sql
  Module: Identity
  Purpose:
  - Create Identity truth tables for CommercialNews V1.
  - Security-first design:
      * store token hashes only
      * never store raw tokens
      * keep account state explicit
  - Idempotent: safe to re-run.

  Tables:
  - [identity].[UserAccount]
  - [identity].[EmailVerificationToken]
  - [identity].[PasswordResetToken]
  - [identity].[RefreshToken]
  - [identity].[LoginHistory]

  Notes:
  - Index-heavy tuning belongs in 010_indexes.sql.
  - Stored procedures belong in 020_procs.sql.
  - Outbox is intentionally not created here; it belongs to Notifications.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 51001, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

USE [CommercialNews];
GO

IF SCHEMA_ID(N'identity') IS NULL
BEGIN
    THROW 51002, 'Schema [identity] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

/* =========================================================
   1) [identity].[UserAccount]
   ========================================================= */
IF OBJECT_ID(N'[identity].[UserAccount]', N'U') IS NULL
BEGIN
    CREATE TABLE [identity].[UserAccount]
    (
        [UserId]              BIGINT IDENTITY(1,1) NOT NULL,
        [PublicId]            CHAR(26)             NOT NULL, -- ULID
        [Email]               NVARCHAR(320)        NOT NULL,
        [EmailNormalized]     NVARCHAR(320)        NOT NULL,
        [PasswordHash]        NVARCHAR(500)        NOT NULL,

        [FullName]            NVARCHAR(200)        NULL,
        [AvatarUrl]           NVARCHAR(800)        NULL,

        [IsEmailVerified]     BIT                  NOT NULL
            CONSTRAINT [DF_UserAccount_IsEmailVerified] DEFAULT (0),
        [EmailVerifiedAt]     DATETIME2(3)         NULL,

        [Status]              VARCHAR(20)          NOT NULL
            CONSTRAINT [DF_UserAccount_Status] DEFAULT ('Unverified'),
        [LockedUntil]         DATETIME2(3)         NULL,

        [CreatedAt]           DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_UserAccount_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt]           DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_UserAccount_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
        [LastLoginAt]         DATETIME2(3)         NULL,

        [Version]             INT                  NOT NULL
            CONSTRAINT [DF_UserAccount_Version] DEFAULT (1),

        CONSTRAINT [PK_UserAccount] PRIMARY KEY CLUSTERED ([UserId] ASC),

        CONSTRAINT [UQ_UserAccount_PublicId]
            UNIQUE ([PublicId]),

        CONSTRAINT [UQ_UserAccount_EmailNormalized]
            UNIQUE ([EmailNormalized]),

        CONSTRAINT [CK_UserAccount_Status]
            CHECK ([Status] IN ('Unverified', 'Active', 'Locked', 'Disabled')),

        CONSTRAINT [CK_UserAccount_Version]
            CHECK ([Version] >= 1),

        CONSTRAINT [CK_UserAccount_EmailVerifiedAt]
            CHECK (
                [EmailVerifiedAt] IS NULL
                OR [EmailVerifiedAt] >= [CreatedAt]
            ),

        CONSTRAINT [CK_UserAccount_LockedUntil]
            CHECK (
                [LockedUntil] IS NULL
                OR [LockedUntil] >= [CreatedAt]
            )
    );

    PRINT N'Created table: [identity].[UserAccount]';
END
ELSE
BEGIN
    PRINT N'Table exists: [identity].[UserAccount]';
END
GO

/* =========================================================
   2) [identity].[EmailVerificationToken]
   ========================================================= */
IF OBJECT_ID(N'[identity].[EmailVerificationToken]', N'U') IS NULL
BEGIN
    CREATE TABLE [identity].[EmailVerificationToken]
    (
        [VerificationTokenId] BIGINT IDENTITY(1,1) NOT NULL,
        [UserId]              BIGINT               NOT NULL,

        [TokenHash]           VARBINARY(32)        NOT NULL,
        [ExpiresAt]           DATETIME2(3)         NOT NULL,
        [UsedAt]              DATETIME2(3)         NULL,

        [CreatedAt]           DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_EmailVerificationToken_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [CreatedIp]           NVARCHAR(45)         NULL,
        [CorrelationId]       NVARCHAR(100)        NULL,

        CONSTRAINT [PK_EmailVerificationToken]
            PRIMARY KEY CLUSTERED ([VerificationTokenId] ASC),

        CONSTRAINT [UQ_EmailVerificationToken_TokenHash]
            UNIQUE ([TokenHash]),

        CONSTRAINT [FK_EmailVerificationToken_UserAccount]
            FOREIGN KEY ([UserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [CK_EmailVerificationToken_ExpiresAt]
            CHECK ([ExpiresAt] > [CreatedAt]),

        CONSTRAINT [CK_EmailVerificationToken_UsedAt]
            CHECK (
                [UsedAt] IS NULL
                OR [UsedAt] >= [CreatedAt]
            )
    );

    PRINT N'Created table: [identity].[EmailVerificationToken]';
END
ELSE
BEGIN
    PRINT N'Table exists: [identity].[EmailVerificationToken]';
END
GO

/* =========================================================
   3) [identity].[PasswordResetToken]
   ========================================================= */
IF OBJECT_ID(N'[identity].[PasswordResetToken]', N'U') IS NULL
BEGIN
    CREATE TABLE [identity].[PasswordResetToken]
    (
        [ResetTokenId]        BIGINT IDENTITY(1,1) NOT NULL,
        [UserId]              BIGINT               NOT NULL,

        [TokenHash]           VARBINARY(32)        NOT NULL,
        [ExpiresAt]           DATETIME2(3)         NOT NULL,
        [UsedAt]              DATETIME2(3)         NULL,
        [RevokedAt]           DATETIME2(3)         NULL,

        [CreatedAt]           DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_PasswordResetToken_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [CreatedIp]           NVARCHAR(45)         NULL,
        [CorrelationId]       NVARCHAR(100)        NULL,

        CONSTRAINT [PK_PasswordResetToken]
            PRIMARY KEY CLUSTERED ([ResetTokenId] ASC),

        CONSTRAINT [UQ_PasswordResetToken_TokenHash]
            UNIQUE ([TokenHash]),

        CONSTRAINT [FK_PasswordResetToken_UserAccount]
            FOREIGN KEY ([UserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [CK_PasswordResetToken_ExpiresAt]
            CHECK ([ExpiresAt] > [CreatedAt]),

        CONSTRAINT [CK_PasswordResetToken_UsedAt]
            CHECK (
                [UsedAt] IS NULL
                OR [UsedAt] >= [CreatedAt]
            ),

        CONSTRAINT [CK_PasswordResetToken_RevokedAt]
            CHECK (
                [RevokedAt] IS NULL
                OR [RevokedAt] >= [CreatedAt]
            )
    );

    PRINT N'Created table: [identity].[PasswordResetToken]';
END
ELSE
BEGIN
    PRINT N'Table exists: [identity].[PasswordResetToken]';
END
GO

/* =========================================================
   4) [identity].[RefreshToken]
   ========================================================= */
IF OBJECT_ID(N'[identity].[RefreshToken]', N'U') IS NULL
BEGIN
    CREATE TABLE [identity].[RefreshToken]
    (
        [RefreshTokenId]      BIGINT IDENTITY(1,1) NOT NULL,
        [UserId]              BIGINT               NOT NULL,

        [TokenHash]           VARBINARY(32)        NOT NULL,

        [CreatedAt]           DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_RefreshToken_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [ExpiresAt]           DATETIME2(3)         NOT NULL,

        [RevokedAt]           DATETIME2(3)         NULL,
        [RevokedReason]       NVARCHAR(200)        NULL,

        [ReplacedByTokenHash] VARBINARY(32)        NULL,

        [CreatedIp]           NVARCHAR(45)         NULL,
        [UserAgent]           NVARCHAR(300)        NULL,
        [CorrelationId]       NVARCHAR(100)        NULL,

        CONSTRAINT [PK_RefreshToken]
            PRIMARY KEY CLUSTERED ([RefreshTokenId] ASC),

        CONSTRAINT [UQ_RefreshToken_TokenHash]
            UNIQUE ([TokenHash]),

        CONSTRAINT [FK_RefreshToken_UserAccount]
            FOREIGN KEY ([UserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [CK_RefreshToken_ExpiresAt]
            CHECK ([ExpiresAt] > [CreatedAt]),

        CONSTRAINT [CK_RefreshToken_RevokedAt]
            CHECK (
                [RevokedAt] IS NULL
                OR [RevokedAt] >= [CreatedAt]
            )
    );

    PRINT N'Created table: [identity].[RefreshToken]';
END
ELSE
BEGIN
    PRINT N'Table exists: [identity].[RefreshToken]';
END
GO

/* =========================================================
   5) [identity].[LoginHistory]
   ========================================================= */
IF OBJECT_ID(N'[identity].[LoginHistory]', N'U') IS NULL
BEGIN
    CREATE TABLE [identity].[LoginHistory]
    (
        [LoginId]                  BIGINT IDENTITY(1,1) NOT NULL,

        [UserId]                   BIGINT               NULL,
        [EmailNormalizedAttempted] NVARCHAR(320)        NULL,

        [Succeeded]                BIT                  NOT NULL
            CONSTRAINT [DF_LoginHistory_Succeeded] DEFAULT (0),
        [FailureReason]            NVARCHAR(100)        NULL,

        [AttemptedAt]              DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_LoginHistory_AttemptedAt] DEFAULT (SYSUTCDATETIME()),
        [IpAddress]                NVARCHAR(45)         NULL,
        [UserAgent]                NVARCHAR(300)        NULL,
        [CorrelationId]            NVARCHAR(100)        NULL,

        CONSTRAINT [PK_LoginHistory]
            PRIMARY KEY CLUSTERED ([LoginId] ASC),

        CONSTRAINT [FK_LoginHistory_UserAccount]
            FOREIGN KEY ([UserId])
            REFERENCES [identity].[UserAccount]([UserId])
    );

    PRINT N'Created table: [identity].[LoginHistory]';
END
ELSE
BEGIN
    PRINT N'Table exists: [identity].[LoginHistory]';
END
GO