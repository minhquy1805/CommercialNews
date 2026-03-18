/*
  020_create_logins_users.sql

  Purpose:
  - Create server-level logins and database users for CommercialNews V1.
  - Runtime principals:
      1) API
      2) Worker
      3) Migration
  - Idempotent and safe to re-run.

  Security:
  - Never commit real passwords to git.
  - Commit placeholders only.
  - Use a local-only override script for real secrets.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 50002, 'Database [CommercialNews] does not exist. Run 001_create_database.sql first.', 1;
END
GO

-- =========================================================
-- 1) Create server-level LOGINS (run in master)
-- =========================================================
USE [master];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

DECLARE @ApiLogin sysname = N'cn_api_login';
DECLARE @WorkerLogin sysname = N'cn_worker_login';
DECLARE @MigrationLogin sysname = N'cn_migration_login';

DECLARE @ApiPwd nvarchar(256) = N'__REPLACE_WITH_API_LOGIN_PASSWORD__';
DECLARE @WorkerPwd nvarchar(256) = N'__REPLACE_WITH_WORKER_LOGIN_PASSWORD__';
DECLARE @MigrationPwd nvarchar(256) = N'__REPLACE_WITH_MIGRATION_LOGIN_PASSWORD__';

DECLARE @CheckPolicy bit = 1;
DECLARE @CheckExpiration bit = 0;

IF NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE [name] = @ApiLogin)
BEGIN
    IF @ApiPwd LIKE N'__REPLACE_WITH_%'
        THROW 50003, 'API login password placeholder detected. Use a local-only script with a real password.', 1;

    DECLARE @SqlApi nvarchar(max) =
        N'CREATE LOGIN [' + @ApiLogin + N'] WITH PASSWORD = N''' + REPLACE(@ApiPwd, '''', '''''') + N''', ' +
        N'CHECK_POLICY = ' + CASE WHEN @CheckPolicy = 1 THEN N'ON' ELSE N'OFF' END + N', ' +
        N'CHECK_EXPIRATION = ' + CASE WHEN @CheckExpiration = 1 THEN N'ON' ELSE N'OFF' END + N';';

    EXEC sys.sp_executesql @SqlApi;
    PRINT N'Created login: [' + @ApiLogin + N']';
END
ELSE
    PRINT N'Login exists: [' + @ApiLogin + N']';

IF NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE [name] = @WorkerLogin)
BEGIN
    IF @WorkerPwd LIKE N'__REPLACE_WITH_%'
        THROW 50004, 'Worker login password placeholder detected. Use a local-only script with a real password.', 1;

    DECLARE @SqlWorker nvarchar(max) =
        N'CREATE LOGIN [' + @WorkerLogin + N'] WITH PASSWORD = N''' + REPLACE(@WorkerPwd, '''', '''''') + N''', ' +
        N'CHECK_POLICY = ' + CASE WHEN @CheckPolicy = 1 THEN N'ON' ELSE N'OFF' END + N', ' +
        N'CHECK_EXPIRATION = ' + CASE WHEN @CheckExpiration = 1 THEN N'ON' ELSE N'OFF' END + N';';

    EXEC sys.sp_executesql @SqlWorker;
    PRINT N'Created login: [' + @WorkerLogin + N']';
END
ELSE
    PRINT N'Login exists: [' + @WorkerLogin + N']';

IF NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE [name] = @MigrationLogin)
BEGIN
    IF @MigrationPwd LIKE N'__REPLACE_WITH_%'
        THROW 50005, 'Migration login password placeholder detected. Use a local-only script with a real password.', 1;

    DECLARE @SqlMigration nvarchar(max) =
        N'CREATE LOGIN [' + @MigrationLogin + N'] WITH PASSWORD = N''' + REPLACE(@MigrationPwd, '''', '''''') + N''', ' +
        N'CHECK_POLICY = ' + CASE WHEN @CheckPolicy = 1 THEN N'ON' ELSE N'OFF' END + N', ' +
        N'CHECK_EXPIRATION = ' + CASE WHEN @CheckExpiration = 1 THEN N'ON' ELSE N'OFF' END + N';';

    EXEC sys.sp_executesql @SqlMigration;
    PRINT N'Created login: [' + @MigrationLogin + N']';
END
ELSE
    PRINT N'Login exists: [' + @MigrationLogin + N']';
GO

-- =========================================================
-- 2) Create database USERS (run in target database)
-- =========================================================
USE [CommercialNews];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

DECLARE @ApiLogin sysname = N'cn_api_login';
DECLARE @WorkerLogin sysname = N'cn_worker_login';
DECLARE @MigrationLogin sysname = N'cn_migration_login';

DECLARE @ApiUser sysname = N'cn_api_user';
DECLARE @WorkerUser sysname = N'cn_worker_user';
DECLARE @MigrationUser sysname = N'cn_migration_user';

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE [name] = @ApiUser)
BEGIN
    EXEC (N'CREATE USER [' + @ApiUser + N'] FOR LOGIN [' + @ApiLogin + N'];');
    PRINT N'Created user: [' + @ApiUser + N']';
END
ELSE
    PRINT N'User exists: [' + @ApiUser + N']';

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE [name] = @WorkerUser)
BEGIN
    EXEC (N'CREATE USER [' + @WorkerUser + N'] FOR LOGIN [' + @WorkerLogin + N'];');
    PRINT N'Created user: [' + @WorkerUser + N']';
END
ELSE
    PRINT N'User exists: [' + @WorkerUser + N']';

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE [name] = @MigrationUser)
BEGIN
    EXEC (N'CREATE USER [' + @MigrationUser + N'] FOR LOGIN [' + @MigrationLogin + N'];');
    PRINT N'Created user: [' + @MigrationUser + N']';
END
ELSE
    PRINT N'User exists: [' + @MigrationUser + N']';
GO