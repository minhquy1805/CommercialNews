/*
  030_create_roles.sql

  Purpose:
  - Create database roles for CommercialNews V1 (idempotent).
  - Separate API runtime, Worker runtime, and Migration permissions.

  Notes:
  - Keep roles stable.
  - Expand permissions in 040_grants_baseline.sql as modules evolve.
  - Not every new module needs a new role.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 50006, 'Database [CommercialNews] does not exist. Run bootstrap scripts in order.', 1;
END
GO

USE [CommercialNews];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

DECLARE @ApiRole sysname = N'cn_api_rw';
DECLARE @WorkerRole sysname = N'cn_worker_rw';
DECLARE @MigrationRole sysname = N'cn_migration_ddl';
DECLARE @ReadOnlyRole sysname = N'cn_readonly';

IF NOT EXISTS
(
    SELECT 1
    FROM sys.database_principals
    WHERE [type] = 'R' AND [name] = @ApiRole
)
BEGIN
    EXEC (N'CREATE ROLE [' + @ApiRole + N'];');
    PRINT N'Created role: [' + @ApiRole + N']';
END
ELSE
    PRINT N'Role exists: [' + @ApiRole + N']';

IF NOT EXISTS
(
    SELECT 1
    FROM sys.database_principals
    WHERE [type] = 'R' AND [name] = @WorkerRole
)
BEGIN
    EXEC (N'CREATE ROLE [' + @WorkerRole + N'];');
    PRINT N'Created role: [' + @WorkerRole + N']';
END
ELSE
    PRINT N'Role exists: [' + @WorkerRole + N']';

IF NOT EXISTS
(
    SELECT 1
    FROM sys.database_principals
    WHERE [type] = 'R' AND [name] = @MigrationRole
)
BEGIN
    EXEC (N'CREATE ROLE [' + @MigrationRole + N'];');
    PRINT N'Created role: [' + @MigrationRole + N']';
END
ELSE
    PRINT N'Role exists: [' + @MigrationRole + N']';

IF NOT EXISTS
(
    SELECT 1
    FROM sys.database_principals
    WHERE [type] = 'R' AND [name] = @ReadOnlyRole
)
BEGIN
    EXEC (N'CREATE ROLE [' + @ReadOnlyRole + N'];');
    PRINT N'Created role: [' + @ReadOnlyRole + N']';
END
ELSE
    PRINT N'Role exists: [' + @ReadOnlyRole + N']';
GO