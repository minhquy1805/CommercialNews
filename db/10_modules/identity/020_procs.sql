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

USE [CommercialNews];
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 51201, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
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
    @Status              VARCHAR(20) = 'Active',
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

CREATE OR ALTER PROCEDURE [identity].[UserAccount_UpdateProfile]
    @UserId      BIGINT,
    @FullName    NVARCHAR(200) = NULL,
    @AvatarUrl   NVARCHAR(800) = NULL
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
END;
GO

CREATE OR ALTER PROCEDURE [identity].[UserAccount_UpdatePassword]
    @UserId         BIGINT,
    @PasswordHash   NVARCHAR(500)
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
END;
GO

CREATE OR ALTER PROCEDURE [identity].[UserAccount_UpdateLastLogin]
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [identity].[UserAccount]
    SET
        [LastLoginAt] = SYSUTCDATETIME(),
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [UserId] = @UserId;
END;
GO

CREATE OR ALTER PROCEDURE [identity].[UserAccount_SetEmailVerified]
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [identity].[UserAccount]
    SET
        [IsEmailVerified] = 1,
        [EmailVerifiedAt] = SYSUTCDATETIME(),
        [UpdatedAt] = SYSUTCDATETIME(),
        [Version] = [Version] + 1
    WHERE [UserId] = @UserId
      AND [IsEmailVerified] = 0;
END;
GO

CREATE OR ALTER PROCEDURE [identity].[UserAccount_UpdateStatus]
    @UserId       BIGINT,
    @Status       VARCHAR(20),
    @LockedUntil  DATETIME2(3) = NULL
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
END;
GO

/* =========================================================
   EmailVerificationToken
   ========================================================= */

CREATE OR ALTER PROCEDURE [identity].[EmailVerificationToken_Insert]
    @UserId         BIGINT,
    @TokenHash      VARBINARY(32),
    @ExpiresAt      DATETIME2(3),
    @CreatedIp      NVARCHAR(45) = NULL,
    @CorrelationId  NVARCHAR(100) = NULL,
    @VerificationTokenId BIGINT OUTPUT
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
    @VerificationTokenId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [identity].[EmailVerificationToken]
    SET [UsedAt] = SYSUTCDATETIME()
    WHERE [VerificationTokenId] = @VerificationTokenId
      AND [UsedAt] IS NULL;
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
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [identity].[PasswordResetToken]
    SET [RevokedAt] = SYSUTCDATETIME()
    WHERE [UserId] = @UserId
      AND [UsedAt] IS NULL
      AND [RevokedAt] IS NULL
      AND [ExpiresAt] > SYSUTCDATETIME();
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
    @ResetTokenId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [identity].[PasswordResetToken]
    SET [UsedAt] = SYSUTCDATETIME()
    WHERE [ResetTokenId] = @ResetTokenId
      AND [UsedAt] IS NULL
      AND [RevokedAt] IS NULL;
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
    @RevokedReason         NVARCHAR(200) = NULL,
    @ReplacedByTokenHash   VARBINARY(32) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [identity].[RefreshToken]
    SET
        [RevokedAt] = SYSUTCDATETIME(),
        [RevokedReason] = @RevokedReason,
        [ReplacedByTokenHash] = @ReplacedByTokenHash
    WHERE [RefreshTokenId] = @RefreshTokenId
      AND [RevokedAt] IS NULL;
END;
GO

CREATE OR ALTER PROCEDURE [identity].[RefreshToken_RevokeAllActiveByUserId]
    @UserId         BIGINT,
    @RevokedReason  NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [identity].[RefreshToken]
    SET
        [RevokedAt] = SYSUTCDATETIME(),
        [RevokedReason] = @RevokedReason
    WHERE [UserId] = @UserId
      AND [RevokedAt] IS NULL
      AND [ExpiresAt] > SYSUTCDATETIME();
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
    @NewTokenHash          VARBINARY(32),
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
      AND [ExpiresAt] > SYSUTCDATETIME();

    IF @CurrentRefreshTokenId IS NULL
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51210, 'Active refresh token not found or no longer valid.', 1;
    END

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
        @NewTokenHash,
        @NewExpiresAt,
        @CreatedIp,
        @UserAgent,
        @CorrelationId
    );

    SET @NewRefreshTokenId = SCOPE_IDENTITY();

    UPDATE [identity].[RefreshToken]
    SET
        [RevokedAt] = SYSUTCDATETIME(),
        [RevokedReason] = N'Rotated',
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

    BEGIN TRANSACTION;

    SELECT TOP (1)
        @VerificationTokenId = [VerificationTokenId],
        @UserId = [UserId]
    FROM [identity].[EmailVerificationToken] WITH (UPDLOCK, ROWLOCK)
    WHERE [TokenHash] = @TokenHash
      AND [UsedAt] IS NULL
      AND [ExpiresAt] > SYSUTCDATETIME();

    IF @VerificationTokenId IS NULL
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51211, 'Active verification token not found or no longer valid.', 1;
    END

    UPDATE [identity].[EmailVerificationToken]
    SET [UsedAt] = SYSUTCDATETIME()
    WHERE [VerificationTokenId] = @VerificationTokenId
      AND [UsedAt] IS NULL;

    UPDATE [identity].[UserAccount]
    SET
        [IsEmailVerified] = 1,
        [EmailVerifiedAt] = SYSUTCDATETIME(),
        [UpdatedAt] = SYSUTCDATETIME(),
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

    BEGIN TRANSACTION;

    SELECT TOP (1)
        @ResetTokenId = [ResetTokenId],
        @UserId = [UserId]
    FROM [identity].[PasswordResetToken] WITH (UPDLOCK, ROWLOCK)
    WHERE [TokenHash] = @TokenHash
      AND [UsedAt] IS NULL
      AND [RevokedAt] IS NULL
      AND [ExpiresAt] > SYSUTCDATETIME();

    IF @ResetTokenId IS NULL
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51212, 'Active password reset token not found or no longer valid.', 1;
    END

    UPDATE [identity].[PasswordResetToken]
    SET [UsedAt] = SYSUTCDATETIME()
    WHERE [ResetTokenId] = @ResetTokenId
      AND [UsedAt] IS NULL
      AND [RevokedAt] IS NULL;

    UPDATE [identity].[UserAccount]
    SET
        [PasswordHash] = @PasswordHash,
        [UpdatedAt] = SYSUTCDATETIME(),
        [Version] = [Version] + 1
    WHERE [UserId] = @UserId;

    COMMIT TRANSACTION;
END;
GO