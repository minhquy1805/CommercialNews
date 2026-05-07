/*
  File: db/10_modules/identity/020_procs.sql
  Module: Identity
  Purpose:
  - Create stored procedures for Identity V1.
  - Focus on practical OLTP operations for:
      * user account creation and lookup
      * email verification token lifecycle
      * password reset token lifecycle
      * refresh token rotation / revocation
      * login history insert / query
  - Idempotent: uses CREATE OR ALTER PROCEDURE

  Notes:
  - Business orchestration still belongs primarily in application/service layer.
  - Raw tokens are never stored here. Only TokenHash is persisted.
  - Notifications.OutboxMessage is intentionally handled outside this file.
*/

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 51201, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

USE [CommercialNews];
GO

IF SCHEMA_ID(N'identity') IS NULL
BEGIN
    THROW 51202, 'Schema [identity] does not exist. Run bootstrap scripts first.', 1;
END
GO

/* =========================================================
   UserAccount
   ========================================================= */

CREATE OR ALTER PROCEDURE [identity].[UserAccount_Insert]
    @PublicId            CHAR(26),
    @Email               NVARCHAR(320),
    @EmailNormalized     NVARCHAR(320),
    @PasswordHash        NVARCHAR(500),
    @FullName            NVARCHAR(200) = NULL,
    @AvatarUrl           NVARCHAR(800) = NULL,
    @Status              VARCHAR(20),
    @UserId              BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    INSERT INTO [identity].[UserAccount]
    (
        [PublicId],
        [Email],
        [EmailNormalized],
        [PasswordHash],
        [FullName],
        [AvatarUrl],
        [Status]
    )
    VALUES
    (
        @PublicId,
        @Email,
        @EmailNormalized,
        @PasswordHash,
        @FullName,
        @AvatarUrl,
        @Status
    );

    SET @UserId = SCOPE_IDENTITY();
END;
GO

CREATE OR ALTER PROCEDURE [identity].[UserAccount_InsertBootstrapAdmin]
    @PublicId            CHAR(26),
    @Email               NVARCHAR(320),
    @EmailNormalized     NVARCHAR(320),
    @PasswordHash        NVARCHAR(500),
    @FullName            NVARCHAR(200) = NULL,
    @AvatarUrl           NVARCHAR(800) = NULL,
    @UserId              BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    INSERT INTO [identity].[UserAccount]
    (
        [PublicId],
        [Email],
        [EmailNormalized],
        [PasswordHash],
        [FullName],
        [AvatarUrl],
        [IsEmailVerified],
        [EmailVerifiedAt],
        [Status]
    )
    VALUES
    (
        @PublicId,
        @Email,
        @EmailNormalized,
        @PasswordHash,
        @FullName,
        @AvatarUrl,
        1,
        SYSUTCDATETIME(),
        'Active'
    );

    SET @UserId = SCOPE_IDENTITY();
END;
GO

CREATE OR ALTER PROCEDURE [identity].[UserAccount_SelectById]
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [UserId],
        [PublicId],
        [Email],
        [EmailNormalized],
        [PasswordHash],
        [FullName],
        [AvatarUrl],
        [IsEmailVerified],
        [EmailVerifiedAt],
        [Status],
        [LockedUntil],
        [CreatedAt],
        [UpdatedAt],
        [LastLoginAt],
        [Version]
    FROM [identity].[UserAccount]
    WHERE [UserId] = @UserId;
END;
GO

CREATE OR ALTER PROCEDURE [identity].[UserAccount_SelectByPublicId]
    @PublicId CHAR(26)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [UserId],
        [PublicId],
        [Email],
        [EmailNormalized],
        [PasswordHash],
        [FullName],
        [AvatarUrl],
        [IsEmailVerified],
        [EmailVerifiedAt],
        [Status],
        [LockedUntil],
        [CreatedAt],
        [UpdatedAt],
        [LastLoginAt],
        [Version]
    FROM [identity].[UserAccount]
    WHERE [PublicId] = @PublicId;
END;
GO

CREATE OR ALTER PROCEDURE [identity].[UserAccount_SelectByEmailNormalized]
    @EmailNormalized NVARCHAR(320)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [UserId],
        [PublicId],
        [Email],
        [EmailNormalized],
        [PasswordHash],
        [FullName],
        [AvatarUrl],
        [IsEmailVerified],
        [EmailVerifiedAt],
        [Status],
        [LockedUntil],
        [CreatedAt],
        [UpdatedAt],
        [LastLoginAt],
        [Version]
    FROM [identity].[UserAccount]
    WHERE [EmailNormalized] = @EmailNormalized;
END;
GO

CREATE OR ALTER PROCEDURE [identity].[UserAccount_SelectSkipAndTake]
    @Skip INT,
    @Take INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;

    SELECT
        [UserId],
        [PublicId],
        [Email],
        [EmailNormalized],
        [FullName],
        [AvatarUrl],
        [IsEmailVerified],
        [EmailVerifiedAt],
        [Status],
        [LockedUntil],
        [CreatedAt],
        [UpdatedAt],
        [LastLoginAt],
        [Version]
    FROM [identity].[UserAccount]
    ORDER BY [CreatedAt] DESC, [UserId] DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE [identity].[UserAccount_SelectSkipAndTakeWhereDynamic]
    @FromCreatedAt      DATETIME2(3) = NULL,
    @ToCreatedAt        DATETIME2(3) = NULL,
    @Status             VARCHAR(20) = NULL,
    @IsEmailVerified    BIT = NULL,
    @Query              NVARCHAR(320) = NULL,
    @Skip               INT = 0,
    @Take               INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;

    IF @FromCreatedAt IS NOT NULL
       AND @ToCreatedAt IS NOT NULL
       AND @FromCreatedAt > @ToCreatedAt
        THROW 51213, 'User account created time range is invalid.', 1;

    IF @Status IS NOT NULL
        SET @Status = NULLIF(LTRIM(RTRIM(@Status)), '');

    IF @Status IS NOT NULL
       AND @Status NOT IN ('Unverified', 'Active', 'Locked', 'Disabled')
        THROW 51214, 'User account status filter is invalid.', 1;

    IF @Query IS NOT NULL
        SET @Query = NULLIF(LTRIM(RTRIM(@Query)), N'');

    SELECT
        [UserId],
        [PublicId],
        [Email],
        [EmailNormalized],
        [FullName],
        [AvatarUrl],
        [IsEmailVerified],
        [EmailVerifiedAt],
        [Status],
        [LockedUntil],
        [CreatedAt],
        [UpdatedAt],
        [LastLoginAt],
        [Version]
    FROM [identity].[UserAccount]
    WHERE
        (@FromCreatedAt IS NULL OR [CreatedAt] >= @FromCreatedAt)
        AND (@ToCreatedAt IS NULL OR [CreatedAt] <= @ToCreatedAt)
        AND (@Status IS NULL OR [Status] = @Status)
        AND (@IsEmailVerified IS NULL OR [IsEmailVerified] = @IsEmailVerified)
        AND
        (
            @Query IS NULL
            OR [PublicId] LIKE N'%' + @Query + N'%'
            OR [Email] LIKE N'%' + @Query + N'%'
            OR [EmailNormalized] LIKE N'%' + @Query + N'%'
            OR [FullName] LIKE N'%' + @Query + N'%'
        )
    ORDER BY [CreatedAt] DESC, [UserId] DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE [identity].[UserAccount_GetRecordCount]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT_BIG(1) AS [RecordCount]
    FROM [identity].[UserAccount];
END;
GO

CREATE OR ALTER PROCEDURE [identity].[UserAccount_GetRecordCountWhereDynamic]
    @FromCreatedAt      DATETIME2(3) = NULL,
    @ToCreatedAt        DATETIME2(3) = NULL,
    @Status             VARCHAR(20) = NULL,
    @IsEmailVerified    BIT = NULL,
    @Query              NVARCHAR(320) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromCreatedAt IS NOT NULL
       AND @ToCreatedAt IS NOT NULL
       AND @FromCreatedAt > @ToCreatedAt
        THROW 51213, 'User account created time range is invalid.', 1;

    IF @Status IS NOT NULL
        SET @Status = NULLIF(LTRIM(RTRIM(@Status)), '');

    IF @Status IS NOT NULL
       AND @Status NOT IN ('Unverified', 'Active', 'Locked', 'Disabled')
        THROW 51214, 'User account status filter is invalid.', 1;

    IF @Query IS NOT NULL
        SET @Query = NULLIF(LTRIM(RTRIM(@Query)), N'');

    SELECT COUNT_BIG(1) AS [RecordCount]
    FROM [identity].[UserAccount]
    WHERE
        (@FromCreatedAt IS NULL OR [CreatedAt] >= @FromCreatedAt)
        AND (@ToCreatedAt IS NULL OR [CreatedAt] <= @ToCreatedAt)
        AND (@Status IS NULL OR [Status] = @Status)
        AND (@IsEmailVerified IS NULL OR [IsEmailVerified] = @IsEmailVerified)
        AND
        (
            @Query IS NULL
            OR [PublicId] LIKE N'%' + @Query + N'%'
            OR [Email] LIKE N'%' + @Query + N'%'
            OR [EmailNormalized] LIKE N'%' + @Query + N'%'
            OR [FullName] LIKE N'%' + @Query + N'%'
        );
END;
GO

CREATE OR ALTER PROCEDURE [identity].[UserAccount_UpdateProfile]
    @UserId          BIGINT,
    @FullName        NVARCHAR(200) = NULL,
    @AvatarUrl       NVARCHAR(800) = NULL,
    @AffectedRows    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [identity].[UserAccount]
    SET
        [FullName] = @FullName,
        [AvatarUrl] = @AvatarUrl,
        [UpdatedAt] = SYSUTCDATETIME(),
        [Version] = [Version] + 1
    WHERE [UserId] = @UserId;

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [identity].[UserAccount_UpdatePassword]
    @UserId          BIGINT,
    @PasswordHash    NVARCHAR(500),
    @AffectedRows    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [identity].[UserAccount]
    SET
        [PasswordHash] = @PasswordHash,
        [UpdatedAt] = SYSUTCDATETIME(),
        [Version] = [Version] + 1
    WHERE [UserId] = @UserId;

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [identity].[UserAccount_UpdateLastLogin]
    @UserId          BIGINT,
    @LastLoginAt     DATETIME2(3),
    @AffectedRows    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [identity].[UserAccount]
    SET
        [LastLoginAt] = @LastLoginAt,
        [UpdatedAt] = @LastLoginAt,
        [Version] = [Version] + 1
    WHERE [UserId] = @UserId;

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [identity].[UserAccount_SetEmailVerified]
    @UserId             BIGINT,
    @EmailVerifiedAt    DATETIME2(3),
    @AffectedRows       INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [identity].[UserAccount]
    SET
        [IsEmailVerified] = 1,
        [EmailVerifiedAt] = @EmailVerifiedAt,
        [Status] = 'Active',
        [UpdatedAt] = @EmailVerifiedAt,
        [Version] = [Version] + 1
    WHERE [UserId] = @UserId
      AND [IsEmailVerified] = 0;

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [identity].[UserAccount_UpdateStatus]
    @UserId          BIGINT,
    @Status          VARCHAR(20),
    @LockedUntil     DATETIME2(3) = NULL,
    @AffectedRows    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [identity].[UserAccount]
    SET
        [Status] = @Status,
        [LockedUntil] = @LockedUntil,
        [UpdatedAt] = SYSUTCDATETIME(),
        [Version] = [Version] + 1
    WHERE [UserId] = @UserId;

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [identity].[UserAccount_Activate]
    @UserId          BIGINT,
    @AffectedRows    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [identity].[UserAccount]
    SET
        [Status] = 'Active',
        [LockedUntil] = NULL,
        [UpdatedAt] = SYSUTCDATETIME(),
        [Version] = [Version] + 1
    WHERE [UserId] = @UserId
      AND [IsEmailVerified] = 1
      AND
      (
          [Status] <> 'Active'
          OR [LockedUntil] IS NOT NULL
      );

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [identity].[UserAccount_Disable]
    @UserId          BIGINT,
    @AffectedRows    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [identity].[UserAccount]
    SET
        [Status] = 'Disabled',
        [LockedUntil] = NULL,
        [UpdatedAt] = SYSUTCDATETIME(),
        [Version] = [Version] + 1
    WHERE [UserId] = @UserId
      AND
      (
          [Status] <> 'Disabled'
          OR [LockedUntil] IS NOT NULL
      );

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [identity].[UserAccount_Lock]
    @UserId          BIGINT,
    @LockedUntil     DATETIME2(3),
    @AffectedRows    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    IF @LockedUntil IS NULL
        THROW 51215, 'LockedUntil is required.', 1;

    IF @LockedUntil <= @NowUtc
        THROW 51216, 'LockedUntil must be in the future.', 1;

    UPDATE [identity].[UserAccount]
    SET
        [Status] = 'Locked',
        [LockedUntil] = @LockedUntil,
        [UpdatedAt] = @NowUtc,
        [Version] = [Version] + 1
    WHERE [UserId] = @UserId
      AND
      (
          [Status] <> 'Locked'
          OR [LockedUntil] IS NULL
          OR [LockedUntil] <> @LockedUntil
      );

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [identity].[UserAccount_Unlock]
    @UserId          BIGINT,
    @AffectedRows    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [identity].[UserAccount]
    SET
        [Status] = CASE
            WHEN [IsEmailVerified] = 1 THEN 'Active'
            ELSE 'Unverified'
        END,
        [LockedUntil] = NULL,
        [UpdatedAt] = SYSUTCDATETIME(),
        [Version] = [Version] + 1
    WHERE [UserId] = @UserId
      AND
      (
          [Status] = 'Locked'
          OR [LockedUntil] IS NOT NULL
      );

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [identity].[UserAccount_MarkEmailVerified]
    @UserId          BIGINT,
    @AffectedRows    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    UPDATE [identity].[UserAccount]
    SET
        [IsEmailVerified] = 1,
        [EmailVerifiedAt] = @NowUtc,
        [Status] = CASE
            WHEN [Status] = 'Unverified' THEN 'Active'
            ELSE [Status]
        END,
        [UpdatedAt] = @NowUtc,
        [Version] = [Version] + 1
    WHERE [UserId] = @UserId
      AND [IsEmailVerified] = 0;

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

/* =========================================================
   EmailVerificationToken
   ========================================================= */

CREATE OR ALTER PROCEDURE [identity].[EmailVerificationToken_Insert]
    @UserId               BIGINT,
    @TokenHash            VARBINARY(32),
    @ExpiresAt            DATETIME2(3),
    @CreatedIp            NVARCHAR(45) = NULL,
    @CorrelationId        NVARCHAR(100) = NULL,
    @VerificationTokenId  BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    INSERT INTO [identity].[EmailVerificationToken]
    (
        [UserId],
        [TokenHash],
        [ExpiresAt],
        [CreatedIp],
        [CorrelationId]
    )
    VALUES
    (
        @UserId,
        @TokenHash,
        @ExpiresAt,
        @CreatedIp,
        @CorrelationId
    );

    SET @VerificationTokenId = SCOPE_IDENTITY();
END;
GO

CREATE OR ALTER PROCEDURE [identity].[EmailVerificationToken_SelectActiveByTokenHash]
    @TokenHash VARBINARY(32)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        [VerificationTokenId],
        [UserId],
        [TokenHash],
        [ExpiresAt],
        [UsedAt],
        [CreatedAt],
        [CreatedIp],
        [CorrelationId]
    FROM [identity].[EmailVerificationToken]
    WHERE [TokenHash] = @TokenHash
      AND [UsedAt] IS NULL
      AND [ExpiresAt] > SYSUTCDATETIME();
END;
GO

CREATE OR ALTER PROCEDURE [identity].[EmailVerificationToken_MarkUsed]
    @VerificationTokenId  BIGINT,
    @UsedAt               DATETIME2(3),
    @AffectedRows         INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [identity].[EmailVerificationToken]
    SET [UsedAt] = @UsedAt
    WHERE [VerificationTokenId] = @VerificationTokenId
      AND [UsedAt] IS NULL;

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [identity].[EmailVerificationToken_SelectByUserId]
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [VerificationTokenId],
        [UserId],
        [TokenHash],
        [ExpiresAt],
        [UsedAt],
        [CreatedAt],
        [CreatedIp],
        [CorrelationId]
    FROM [identity].[EmailVerificationToken]
    WHERE [UserId] = @UserId
    ORDER BY [CreatedAt] DESC;
END;
GO

/* =========================================================
   PasswordResetToken
   ========================================================= */

CREATE OR ALTER PROCEDURE [identity].[PasswordResetToken_RevokeActiveByUserId]
    @UserId          BIGINT,
    @RevokedAt       DATETIME2(3),
    @AffectedRows    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [identity].[PasswordResetToken]
    SET [RevokedAt] = @RevokedAt
    WHERE [UserId] = @UserId
      AND [UsedAt] IS NULL
      AND [RevokedAt] IS NULL
      AND [ExpiresAt] > @RevokedAt;

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [identity].[PasswordResetToken_Insert]
    @UserId         BIGINT,
    @TokenHash      VARBINARY(32),
    @ExpiresAt      DATETIME2(3),
    @CreatedIp      NVARCHAR(45) = NULL,
    @CorrelationId  NVARCHAR(100) = NULL,
    @ResetTokenId   BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    INSERT INTO [identity].[PasswordResetToken]
    (
        [UserId],
        [TokenHash],
        [ExpiresAt],
        [CreatedIp],
        [CorrelationId]
    )
    VALUES
    (
        @UserId,
        @TokenHash,
        @ExpiresAt,
        @CreatedIp,
        @CorrelationId
    );

    SET @ResetTokenId = SCOPE_IDENTITY();
END;
GO

CREATE OR ALTER PROCEDURE [identity].[PasswordResetToken_SelectActiveByTokenHash]
    @TokenHash VARBINARY(32)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        [ResetTokenId],
        [UserId],
        [TokenHash],
        [ExpiresAt],
        [UsedAt],
        [RevokedAt],
        [CreatedAt],
        [CreatedIp],
        [CorrelationId]
    FROM [identity].[PasswordResetToken]
    WHERE [TokenHash] = @TokenHash
      AND [UsedAt] IS NULL
      AND [RevokedAt] IS NULL
      AND [ExpiresAt] > SYSUTCDATETIME();
END;
GO

CREATE OR ALTER PROCEDURE [identity].[PasswordResetToken_MarkUsed]
    @ResetTokenId     BIGINT,
    @UsedAt           DATETIME2(3),
    @AffectedRows     INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [identity].[PasswordResetToken]
    SET [UsedAt] = @UsedAt
    WHERE [ResetTokenId] = @ResetTokenId
      AND [UsedAt] IS NULL
      AND [RevokedAt] IS NULL;

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [identity].[PasswordResetToken_SelectByUserId]
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [ResetTokenId],
        [UserId],
        [TokenHash],
        [ExpiresAt],
        [UsedAt],
        [RevokedAt],
        [CreatedAt],
        [CreatedIp],
        [CorrelationId]
    FROM [identity].[PasswordResetToken]
    WHERE [UserId] = @UserId
    ORDER BY [CreatedAt] DESC;
END;
GO

/* =========================================================
   RefreshToken
   ========================================================= */

CREATE OR ALTER PROCEDURE [identity].[RefreshToken_Insert]
    @UserId               BIGINT,
    @TokenHash            VARBINARY(32),
    @ExpiresAt            DATETIME2(3),
    @CreatedIp            NVARCHAR(45) = NULL,
    @UserAgent            NVARCHAR(300) = NULL,
    @CorrelationId        NVARCHAR(100) = NULL,
    @RefreshTokenId       BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    INSERT INTO [identity].[RefreshToken]
    (
        [UserId],
        [TokenHash],
        [ExpiresAt],
        [CreatedIp],
        [UserAgent],
        [CorrelationId]
    )
    VALUES
    (
        @UserId,
        @TokenHash,
        @ExpiresAt,
        @CreatedIp,
        @UserAgent,
        @CorrelationId
    );

    SET @RefreshTokenId = SCOPE_IDENTITY();
END;
GO

CREATE OR ALTER PROCEDURE [identity].[RefreshToken_SelectActiveByTokenHash]
    @TokenHash VARBINARY(32)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        [RefreshTokenId],
        [UserId],
        [TokenHash],
        [CreatedAt],
        [ExpiresAt],
        [RevokedAt],
        [RevokedReason],
        [ReplacedByTokenHash],
        [CreatedIp],
        [UserAgent],
        [CorrelationId]
    FROM [identity].[RefreshToken]
    WHERE [TokenHash] = @TokenHash
      AND [RevokedAt] IS NULL
      AND [ExpiresAt] > SYSUTCDATETIME();
END;
GO

CREATE OR ALTER PROCEDURE [identity].[RefreshToken_SelectByTokenHash]
    @TokenHash VARBINARY(32)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        [RefreshTokenId],
        [UserId],
        [TokenHash],
        [CreatedAt],
        [ExpiresAt],
        [RevokedAt],
        [RevokedReason],
        [ReplacedByTokenHash],
        [CreatedIp],
        [UserAgent],
        [CorrelationId]
    FROM [identity].[RefreshToken]
    WHERE [TokenHash] = @TokenHash;
END;
GO

CREATE OR ALTER PROCEDURE [identity].[RefreshToken_Revoke]
    @RefreshTokenId        BIGINT,
    @RevokedAt             DATETIME2(3),
    @RevokedReason         NVARCHAR(200) = NULL,
    @ReplacedByTokenHash   VARBINARY(32) = NULL,
    @AffectedRows          INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [identity].[RefreshToken]
    SET
        [RevokedAt] = @RevokedAt,
        [RevokedReason] = @RevokedReason,
        [ReplacedByTokenHash] = @ReplacedByTokenHash
    WHERE [RefreshTokenId] = @RefreshTokenId
      AND [RevokedAt] IS NULL;

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [identity].[RefreshToken_RevokeAllActiveByUserId]
    @UserId          BIGINT,
    @RevokedAt       DATETIME2(3),
    @RevokedReason   NVARCHAR(200) = NULL,
    @AffectedRows    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [identity].[RefreshToken]
    SET
        [RevokedAt] = @RevokedAt,
        [RevokedReason] = @RevokedReason
    WHERE [UserId] = @UserId
      AND [RevokedAt] IS NULL
      AND [ExpiresAt] > @RevokedAt;

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [identity].[RefreshToken_SelectByUserId]
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [RefreshTokenId],
        [UserId],
        [TokenHash],
        [CreatedAt],
        [ExpiresAt],
        [RevokedAt],
        [RevokedReason],
        [ReplacedByTokenHash],
        [CreatedIp],
        [UserAgent],
        [CorrelationId]
    FROM [identity].[RefreshToken]
    WHERE [UserId] = @UserId
    ORDER BY [CreatedAt] DESC;
END;
GO

/* =========================================================
   RefreshToken rotation (atomic helper)
   ========================================================= */

CREATE OR ALTER PROCEDURE [identity].[RefreshToken_Rotate]
    @CurrentTokenHash      VARBINARY(32),
    @RevokedAt             DATETIME2(3),
    @RevokedReason         NVARCHAR(200) = NULL,
    @NewTokenHash          VARBINARY(32),
    @NewCreatedAt          DATETIME2(3),
    @NewExpiresAt          DATETIME2(3),
    @CreatedIp             NVARCHAR(45) = NULL,
    @UserAgent             NVARCHAR(300) = NULL,
    @CorrelationId         NVARCHAR(100) = NULL,
    @NewRefreshTokenId     BIGINT OUTPUT,
    @UserId                BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @CurrentRefreshTokenId BIGINT;

    BEGIN TRANSACTION;

    SELECT TOP (1)
        @CurrentRefreshTokenId = [RefreshTokenId],
        @UserId = [UserId]
    FROM [identity].[RefreshToken] WITH (UPDLOCK, ROWLOCK)
    WHERE [TokenHash] = @CurrentTokenHash
      AND [RevokedAt] IS NULL
      AND [ExpiresAt] > @RevokedAt;

    IF @CurrentRefreshTokenId IS NULL
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51210, 'Active refresh token not found or no longer valid.', 1;
    END

    INSERT INTO [identity].[RefreshToken]
    (
        [UserId],
        [TokenHash],
        [CreatedAt],
        [ExpiresAt],
        [CreatedIp],
        [UserAgent],
        [CorrelationId]
    )
    VALUES
    (
        @UserId,
        @NewTokenHash,
        @NewCreatedAt,
        @NewExpiresAt,
        @CreatedIp,
        @UserAgent,
        @CorrelationId
    );

    SET @NewRefreshTokenId = SCOPE_IDENTITY();

    UPDATE [identity].[RefreshToken]
    SET
        [RevokedAt] = @RevokedAt,
        [RevokedReason] = @RevokedReason,
        [ReplacedByTokenHash] = @NewTokenHash
    WHERE [RefreshTokenId] = @CurrentRefreshTokenId
      AND [RevokedAt] IS NULL;

    COMMIT TRANSACTION;
END;
GO

/* =========================================================
   LoginHistory
   ========================================================= */

CREATE OR ALTER PROCEDURE [identity].[LoginHistory_Insert]
    @UserId                    BIGINT = NULL,
    @EmailNormalizedAttempted  NVARCHAR(320) = NULL,
    @Succeeded                 BIT,
    @FailureReason             NVARCHAR(100) = NULL,
    @IpAddress                 NVARCHAR(45) = NULL,
    @UserAgent                 NVARCHAR(300) = NULL,
    @CorrelationId             NVARCHAR(100) = NULL,
    @LoginId                   BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    INSERT INTO [identity].[LoginHistory]
    (
        [UserId],
        [EmailNormalizedAttempted],
        [Succeeded],
        [FailureReason],
        [IpAddress],
        [UserAgent],
        [CorrelationId]
    )
    VALUES
    (
        @UserId,
        @EmailNormalizedAttempted,
        @Succeeded,
        @FailureReason,
        @IpAddress,
        @UserAgent,
        @CorrelationId
    );

    SET @LoginId = SCOPE_IDENTITY();
END;
GO

CREATE OR ALTER PROCEDURE [identity].[LoginHistory_SelectByUserId]
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [LoginId],
        [UserId],
        [EmailNormalizedAttempted],
        [Succeeded],
        [FailureReason],
        [AttemptedAt],
        [IpAddress],
        [UserAgent],
        [CorrelationId]
    FROM [identity].[LoginHistory]
    WHERE [UserId] = @UserId
    ORDER BY [AttemptedAt] DESC;
END;
GO

CREATE OR ALTER PROCEDURE [identity].[LoginHistory_SelectRecent]
    @TopN INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    IF @TopN IS NULL OR @TopN <= 0
        SET @TopN = 100;

    SELECT TOP (@TopN)
        [LoginId],
        [UserId],
        [EmailNormalizedAttempted],
        [Succeeded],
        [FailureReason],
        [AttemptedAt],
        [IpAddress],
        [UserAgent],
        [CorrelationId]
    FROM [identity].[LoginHistory]
    ORDER BY [AttemptedAt] DESC;
END;
GO

CREATE OR ALTER PROCEDURE [identity].[LoginHistory_SelectSkipAndTakeByUserId]
    @UserId BIGINT,
    @Succeeded BIT = NULL,
    @FromAttemptedAt DATETIME2(3) = NULL,
    @ToAttemptedAt DATETIME2(3) = NULL,
    @Skip INT = 0,
    @Take INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;

    IF @FromAttemptedAt IS NOT NULL
       AND @ToAttemptedAt IS NOT NULL
       AND @FromAttemptedAt > @ToAttemptedAt
        THROW 51230, 'Login history time range is invalid.', 1;

    SELECT
        [LoginId],
        [UserId],
        [EmailNormalizedAttempted],
        [Succeeded],
        [FailureReason],
        [AttemptedAt],
        [IpAddress],
        [UserAgent],
        [CorrelationId]
    FROM [identity].[LoginHistory]
    WHERE [UserId] = @UserId
      AND (@Succeeded IS NULL OR [Succeeded] = @Succeeded)
      AND (@FromAttemptedAt IS NULL OR [AttemptedAt] >= @FromAttemptedAt)
      AND (@ToAttemptedAt IS NULL OR [AttemptedAt] <= @ToAttemptedAt)
    ORDER BY [AttemptedAt] DESC, [LoginId] DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE [identity].[LoginHistory_GetRecordCountByUserId]
    @UserId BIGINT,
    @Succeeded BIT = NULL,
    @FromAttemptedAt DATETIME2(3) = NULL,
    @ToAttemptedAt DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromAttemptedAt IS NOT NULL
       AND @ToAttemptedAt IS NOT NULL
       AND @FromAttemptedAt > @ToAttemptedAt
        THROW 51230, 'Login history time range is invalid.', 1;

    SELECT COUNT_BIG(1) AS [RecordCount]
    FROM [identity].[LoginHistory]
    WHERE [UserId] = @UserId
      AND (@Succeeded IS NULL OR [Succeeded] = @Succeeded)
      AND (@FromAttemptedAt IS NULL OR [AttemptedAt] >= @FromAttemptedAt)
      AND (@ToAttemptedAt IS NULL OR [AttemptedAt] <= @ToAttemptedAt);
END;
GO

/* =========================================================
   Compound atomic helper: Verify Email
   ========================================================= */

CREATE OR ALTER PROCEDURE [identity].[UserAccount_VerifyEmailByTokenHash]
    @TokenHash VARBINARY(32),
    @UserId BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @VerificationTokenId BIGINT;
    DECLARE @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    BEGIN TRANSACTION;

    SELECT TOP (1)
        @VerificationTokenId = [VerificationTokenId],
        @UserId = [UserId]
    FROM [identity].[EmailVerificationToken] WITH (UPDLOCK, ROWLOCK)
    WHERE [TokenHash] = @TokenHash
      AND [UsedAt] IS NULL
      AND [ExpiresAt] > @NowUtc;

    IF @VerificationTokenId IS NULL
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51211, 'Active verification token not found or no longer valid.', 1;
    END

    UPDATE [identity].[EmailVerificationToken]
    SET [UsedAt] = @NowUtc
    WHERE [VerificationTokenId] = @VerificationTokenId
      AND [UsedAt] IS NULL;

    UPDATE [identity].[UserAccount]
    SET
        [IsEmailVerified] = 1,
        [EmailVerifiedAt] = @NowUtc,
        [Status] = 'Active',
        [UpdatedAt] = @NowUtc,
        [Version] = [Version] + 1
    WHERE [UserId] = @UserId
      AND [IsEmailVerified] = 0;

    COMMIT TRANSACTION;
END;
GO

/* =========================================================
   Compound atomic helper: Reset Password By Token
   ========================================================= */

CREATE OR ALTER PROCEDURE [identity].[UserAccount_ResetPasswordByTokenHash]
    @TokenHash        VARBINARY(32),
    @PasswordHash     NVARCHAR(500),
    @UserId           BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ResetTokenId BIGINT;
    DECLARE @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    BEGIN TRANSACTION;

    SELECT TOP (1)
        @ResetTokenId = [ResetTokenId],
        @UserId = [UserId]
    FROM [identity].[PasswordResetToken] WITH (UPDLOCK, ROWLOCK)
    WHERE [TokenHash] = @TokenHash
      AND [UsedAt] IS NULL
      AND [RevokedAt] IS NULL
      AND [ExpiresAt] > @NowUtc;

    IF @ResetTokenId IS NULL
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51212, 'Active password reset token not found or no longer valid.', 1;
    END

    UPDATE [identity].[PasswordResetToken]
    SET [UsedAt] = @NowUtc
    WHERE [ResetTokenId] = @ResetTokenId
      AND [UsedAt] IS NULL
      AND [RevokedAt] IS NULL;

    UPDATE [identity].[UserAccount]
    SET
        [PasswordHash] = @PasswordHash,
        [UpdatedAt] = @NowUtc,
        [Version] = [Version] + 1
    WHERE [UserId] = @UserId;

    UPDATE [identity].[RefreshToken]
    SET
        [RevokedAt] = @NowUtc,
        [RevokedReason] = N'PasswordReset'
    WHERE [UserId] = @UserId
      AND [RevokedAt] IS NULL
      AND [ExpiresAt] > @NowUtc;

    COMMIT TRANSACTION;
END;
GO
