/*
  File: db/10_modules/identity/010_indexes.sql
  Module: Identity
  Purpose:
  - Create non-PK/non-constraint indexes for Identity tables.
  - Optimize core security-critical flows and operational jobs.

  Notes:
  - Idempotent: safe to re-run.
  - Unique constraints are already defined in 001_tables.sql where appropriate.
  - This file focuses on read-path, validation-path, cleanup-path, and abuse-review indexes.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 51101, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

USE [CommercialNews];
GO

IF SCHEMA_ID(N'identity') IS NULL
BEGIN
    THROW 51102, 'Schema [identity] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

/* =========================================================
   Guard clauses for required tables
   ========================================================= */
IF OBJECT_ID(N'[identity].[UserAccount]', N'U') IS NULL
    THROW 51103, 'Table [identity].[UserAccount] does not exist. Run 001_tables.sql first.', 1;

IF OBJECT_ID(N'[identity].[EmailVerificationToken]', N'U') IS NULL
    THROW 51104, 'Table [identity].[EmailVerificationToken] does not exist. Run 001_tables.sql first.', 1;

IF OBJECT_ID(N'[identity].[PasswordResetToken]', N'U') IS NULL
    THROW 51105, 'Table [identity].[PasswordResetToken] does not exist. Run 001_tables.sql first.', 1;

IF OBJECT_ID(N'[identity].[RefreshToken]', N'U') IS NULL
    THROW 51106, 'Table [identity].[RefreshToken] does not exist. Run 001_tables.sql first.', 1;

IF OBJECT_ID(N'[identity].[LoginHistory]', N'U') IS NULL
    THROW 51107, 'Table [identity].[LoginHistory] does not exist. Run 001_tables.sql first.', 1;
GO

/* =========================================================
   1) UserAccount indexes
   ========================================================= */

-- Operational queries: locked accounts / status filtering
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_UserAccount_Status_LockedUntil'
      AND [object_id] = OBJECT_ID(N'[identity].[UserAccount]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_UserAccount_Status_LockedUntil]
    ON [identity].[UserAccount] ([Status] ASC, [LockedUntil] ASC);
    PRINT N'Created index: [identity].[UserAccount].[IX_UserAccount_Status_LockedUntil]';
END
ELSE
BEGIN
    PRINT N'Index exists: [identity].[UserAccount].[IX_UserAccount_Status_LockedUntil]';
END
GO

-- Useful for verification-state reviews / onboarding ops / filtering
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_UserAccount_IsEmailVerified_CreatedAt'
      AND [object_id] = OBJECT_ID(N'[identity].[UserAccount]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_UserAccount_IsEmailVerified_CreatedAt]
    ON [identity].[UserAccount] ([IsEmailVerified] ASC, [CreatedAt] ASC);
    PRINT N'Created index: [identity].[UserAccount].[IX_UserAccount_IsEmailVerified_CreatedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [identity].[UserAccount].[IX_UserAccount_IsEmailVerified_CreatedAt]';
END
GO

/* =========================================================
   2) EmailVerificationToken indexes
   ========================================================= */

-- Per-user token review / resend / cleanup support
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_EmailVerificationToken_UserId_ExpiresAt'
      AND [object_id] = OBJECT_ID(N'[identity].[EmailVerificationToken]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmailVerificationToken_UserId_ExpiresAt]
    ON [identity].[EmailVerificationToken] ([UserId] ASC, [ExpiresAt] ASC);
    PRINT N'Created index: [identity].[EmailVerificationToken].[IX_EmailVerificationToken_UserId_ExpiresAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [identity].[EmailVerificationToken].[IX_EmailVerificationToken_UserId_ExpiresAt]';
END
GO

-- Cleanup job support for expired tokens
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_EmailVerificationToken_ExpiresAt'
      AND [object_id] = OBJECT_ID(N'[identity].[EmailVerificationToken]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmailVerificationToken_ExpiresAt]
    ON [identity].[EmailVerificationToken] ([ExpiresAt] ASC);
    PRINT N'Created index: [identity].[EmailVerificationToken].[IX_EmailVerificationToken_ExpiresAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [identity].[EmailVerificationToken].[IX_EmailVerificationToken_ExpiresAt]';
END
GO

/* =========================================================
   3) PasswordResetToken indexes
   ========================================================= */

-- Per-user token review / invalidation / cleanup support
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_PasswordResetToken_UserId_ExpiresAt'
      AND [object_id] = OBJECT_ID(N'[identity].[PasswordResetToken]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PasswordResetToken_UserId_ExpiresAt]
    ON [identity].[PasswordResetToken] ([UserId] ASC, [ExpiresAt] ASC);
    PRINT N'Created index: [identity].[PasswordResetToken].[IX_PasswordResetToken_UserId_ExpiresAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [identity].[PasswordResetToken].[IX_PasswordResetToken_UserId_ExpiresAt]';
END
GO

-- Cleanup job support for expired reset tokens
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_PasswordResetToken_ExpiresAt'
      AND [object_id] = OBJECT_ID(N'[identity].[PasswordResetToken]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PasswordResetToken_ExpiresAt]
    ON [identity].[PasswordResetToken] ([ExpiresAt] ASC);
    PRINT N'Created index: [identity].[PasswordResetToken].[IX_PasswordResetToken_ExpiresAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [identity].[PasswordResetToken].[IX_PasswordResetToken_ExpiresAt]';
END
GO

/* =========================================================
   4) RefreshToken indexes
   ========================================================= */

-- Per-user session review / logout-all / cleanup support
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_RefreshToken_UserId_RevokedAt_ExpiresAt'
      AND [object_id] = OBJECT_ID(N'[identity].[RefreshToken]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_RefreshToken_UserId_RevokedAt_ExpiresAt]
    ON [identity].[RefreshToken] ([UserId] ASC, [RevokedAt] ASC, [ExpiresAt] ASC);
    PRINT N'Created index: [identity].[RefreshToken].[IX_RefreshToken_UserId_RevokedAt_ExpiresAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [identity].[RefreshToken].[IX_RefreshToken_UserId_RevokedAt_ExpiresAt]';
END
GO

-- Cleanup job support for expired refresh tokens
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_RefreshToken_ExpiresAt'
      AND [object_id] = OBJECT_ID(N'[identity].[RefreshToken]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_RefreshToken_ExpiresAt]
    ON [identity].[RefreshToken] ([ExpiresAt] ASC);
    PRINT N'Created index: [identity].[RefreshToken].[IX_RefreshToken_ExpiresAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [identity].[RefreshToken].[IX_RefreshToken_ExpiresAt]';
END
GO

-- Rotation chain investigation / token family traversal
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_RefreshToken_ReplacedByTokenHash'
      AND [object_id] = OBJECT_ID(N'[identity].[RefreshToken]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_RefreshToken_ReplacedByTokenHash]
    ON [identity].[RefreshToken] ([ReplacedByTokenHash] ASC);
    PRINT N'Created index: [identity].[RefreshToken].[IX_RefreshToken_ReplacedByTokenHash]';
END
ELSE
BEGIN
    PRINT N'Index exists: [identity].[RefreshToken].[IX_RefreshToken_ReplacedByTokenHash]';
END
GO

/* =========================================================
   5) LoginHistory indexes
   ========================================================= */

-- Time-based investigation / retention scanning
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_LoginHistory_AttemptedAt'
      AND [object_id] = OBJECT_ID(N'[identity].[LoginHistory]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_LoginHistory_AttemptedAt]
    ON [identity].[LoginHistory] ([AttemptedAt] ASC);
    PRINT N'Created index: [identity].[LoginHistory].[IX_LoginHistory_AttemptedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [identity].[LoginHistory].[IX_LoginHistory_AttemptedAt]';
END
GO

-- Abuse review by IP
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_LoginHistory_IpAddress_AttemptedAt'
      AND [object_id] = OBJECT_ID(N'[identity].[LoginHistory]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_LoginHistory_IpAddress_AttemptedAt]
    ON [identity].[LoginHistory] ([IpAddress] ASC, [AttemptedAt] ASC);
    PRINT N'Created index: [identity].[LoginHistory].[IX_LoginHistory_IpAddress_AttemptedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [identity].[LoginHistory].[IX_LoginHistory_IpAddress_AttemptedAt]';
END
GO

-- Per-user login audit trail
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_LoginHistory_UserId_AttemptedAt'
      AND [object_id] = OBJECT_ID(N'[identity].[LoginHistory]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_LoginHistory_UserId_AttemptedAt]
    ON [identity].[LoginHistory] ([UserId] ASC, [AttemptedAt] ASC);
    PRINT N'Created index: [identity].[LoginHistory].[IX_LoginHistory_UserId_AttemptedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [identity].[LoginHistory].[IX_LoginHistory_UserId_AttemptedAt]';
END
GO

-- Success/failure trend review
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_LoginHistory_Succeeded_AttemptedAt'
      AND [object_id] = OBJECT_ID(N'[identity].[LoginHistory]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_LoginHistory_Succeeded_AttemptedAt]
    ON [identity].[LoginHistory] ([Succeeded] ASC, [AttemptedAt] ASC);
    PRINT N'Created index: [identity].[LoginHistory].[IX_LoginHistory_Succeeded_AttemptedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [identity].[LoginHistory].[IX_LoginHistory_Succeeded_AttemptedAt]';
END
GO