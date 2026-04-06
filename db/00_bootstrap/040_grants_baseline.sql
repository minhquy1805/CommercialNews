/*
  040_grants_baseline.sql

  Purpose:
  - Add database users to database roles.
  - Apply baseline schema-scoped permissions for CommercialNews V1.

  Notes:
  - This script assumes 020_create_logins_users.sql and 030_create_roles.sql were run first.
  - Runtime roles are intentionally separated:
      - cn_api_rw
      - cn_worker_rw
      - cn_migration_ddl
      - cn_readonly
  - Reading may own stored procedures in V1 even if it does not own physical tables.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 50007, 'Database [CommercialNews] does not exist. Run bootstrap scripts in order.', 1;
END
GO

USE [CommercialNews];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

DECLARE @ApiUser sysname = N'cn_api_user';
DECLARE @WorkerUser sysname = N'cn_worker_user';
DECLARE @MigrationUser sysname = N'cn_migration_user';

DECLARE @ApiRole sysname = N'cn_api_rw';
DECLARE @WorkerRole sysname = N'cn_worker_rw';
DECLARE @MigrationRole sysname = N'cn_migration_ddl';
DECLARE @ReadOnlyRole sysname = N'cn_readonly';

-- =========================================================
-- 1) Add users to roles (idempotent)
-- =========================================================

IF EXISTS (SELECT 1 FROM sys.database_principals WHERE [name] = @ApiUser)
AND EXISTS (SELECT 1 FROM sys.database_principals WHERE [name] = @ApiRole)
AND NOT EXISTS
(
    SELECT 1
    FROM sys.database_role_members rm
    JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id
    JOIN sys.database_principals u ON rm.member_principal_id = u.principal_id
    WHERE r.[name] = @ApiRole AND u.[name] = @ApiUser
)
BEGIN
    EXEC (N'ALTER ROLE [' + @ApiRole + N'] ADD MEMBER [' + @ApiUser + N'];');
    PRINT N'Added [' + @ApiUser + N'] to role [' + @ApiRole + N']';
END
ELSE
    PRINT N'API user-role membership already exists or principal missing';

IF EXISTS (SELECT 1 FROM sys.database_principals WHERE [name] = @WorkerUser)
AND EXISTS (SELECT 1 FROM sys.database_principals WHERE [name] = @WorkerRole)
AND NOT EXISTS
(
    SELECT 1
    FROM sys.database_role_members rm
    JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id
    JOIN sys.database_principals u ON rm.member_principal_id = u.principal_id
    WHERE r.[name] = @WorkerRole AND u.[name] = @WorkerUser
)
BEGIN
    EXEC (N'ALTER ROLE [' + @WorkerRole + N'] ADD MEMBER [' + @WorkerUser + N'];');
    PRINT N'Added [' + @WorkerUser + N'] to role [' + @WorkerRole + N']';
END
ELSE
    PRINT N'Worker user-role membership already exists or principal missing';

IF EXISTS (SELECT 1 FROM sys.database_principals WHERE [name] = @MigrationUser)
AND EXISTS (SELECT 1 FROM sys.database_principals WHERE [name] = @MigrationRole)
AND NOT EXISTS
(
    SELECT 1
    FROM sys.database_role_members rm
    JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id
    JOIN sys.database_principals u ON rm.member_principal_id = u.principal_id
    WHERE r.[name] = @MigrationRole AND u.[name] = @MigrationUser
)
BEGIN
    EXEC (N'ALTER ROLE [' + @MigrationRole + N'] ADD MEMBER [' + @MigrationUser + N'];');
    PRINT N'Added [' + @MigrationUser + N'] to role [' + @MigrationRole + N']';
END
ELSE
    PRINT N'Migration user-role membership already exists or principal missing';

-- =========================================================
-- 2) Migration role permissions
-- =========================================================
-- Pragmatic local/dev baseline:
-- give migration role db_owner to simplify bootstrap and schema evolution.
-- For stricter production deployments, replace this with granular DDL permissions.

IF IS_ROLEMEMBER(N'db_owner', @MigrationRole) <> 1
BEGIN
    EXEC (N'ALTER ROLE [db_owner] ADD MEMBER [' + @MigrationRole + N'];');
    PRINT N'Added role [' + @MigrationRole + N'] to [db_owner]';
END
ELSE
    PRINT N'Role [' + @MigrationRole + N'] is already a member of [db_owner]';

-- =========================================================
-- 3) CONNECT permissions
-- =========================================================
GRANT CONNECT TO [cn_api_rw];
GRANT CONNECT TO [cn_worker_rw];
GRANT CONNECT TO [cn_readonly];

 /* =========================================================
    4) API role baseline grants
    ========================================================= */
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[identity] TO [cn_api_rw];
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[authorization] TO [cn_api_rw];
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[content] TO [cn_api_rw];
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[interaction] TO [cn_api_rw];
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[media] TO [cn_api_rw];
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[notifications] TO [cn_api_rw];
GRANT SELECT ON SCHEMA::[reading] TO [cn_api_rw];
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[seo] TO [cn_api_rw];

GRANT EXECUTE ON SCHEMA::[identity] TO [cn_api_rw];
GRANT EXECUTE ON SCHEMA::[authorization] TO [cn_api_rw];
GRANT EXECUTE ON SCHEMA::[content] TO [cn_api_rw];
GRANT EXECUTE ON SCHEMA::[interaction] TO [cn_api_rw];
GRANT EXECUTE ON SCHEMA::[media] TO [cn_api_rw];
GRANT EXECUTE ON SCHEMA::[notifications] TO [cn_api_rw];
GRANT EXECUTE ON SCHEMA::[reading] TO [cn_api_rw];
GRANT EXECUTE ON SCHEMA::[seo] TO [cn_api_rw];

-- =========================================================
-- 5) Worker role baseline grants
-- =========================================================
GRANT SELECT ON SCHEMA::[identity] TO [cn_worker_rw];
GRANT SELECT ON SCHEMA::[authorization] TO [cn_worker_rw];
GRANT SELECT ON SCHEMA::[content] TO [cn_worker_rw];
GRANT SELECT ON SCHEMA::[seo] TO [cn_worker_rw];
GRANT SELECT ON SCHEMA::[media] TO [cn_worker_rw];

GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[interaction] TO [cn_worker_rw];
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[audit] TO [cn_worker_rw];
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[notifications] TO [cn_worker_rw];

GRANT EXECUTE ON SCHEMA::[identity] TO [cn_worker_rw];
GRANT EXECUTE ON SCHEMA::[authorization] TO [cn_worker_rw];
GRANT EXECUTE ON SCHEMA::[content] TO [cn_worker_rw];
GRANT EXECUTE ON SCHEMA::[seo] TO [cn_worker_rw];
GRANT EXECUTE ON SCHEMA::[media] TO [cn_worker_rw];
GRANT EXECUTE ON SCHEMA::[interaction] TO [cn_worker_rw];
GRANT EXECUTE ON SCHEMA::[audit] TO [cn_worker_rw];
GRANT EXECUTE ON SCHEMA::[notifications] TO [cn_worker_rw];

-- =========================================================
-- 6) Readonly role baseline grants
-- =========================================================
GRANT SELECT ON SCHEMA::[identity] TO [cn_readonly];
GRANT SELECT ON SCHEMA::[authorization] TO [cn_readonly];
GRANT SELECT ON SCHEMA::[content] TO [cn_readonly];
GRANT SELECT ON SCHEMA::[seo] TO [cn_readonly];
GRANT SELECT ON SCHEMA::[media] TO [cn_readonly];
GRANT SELECT ON SCHEMA::[interaction] TO [cn_readonly];
GRANT SELECT ON SCHEMA::[audit] TO [cn_readonly];
GRANT SELECT ON SCHEMA::[notifications] TO [cn_readonly];

PRINT N'Baseline grants applied successfully.';
GO