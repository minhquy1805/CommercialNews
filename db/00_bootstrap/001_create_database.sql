/*
  001_create_database.sql

  Purpose:
  - Create the CommercialNews database if it does not already exist.
  - Apply safe baseline database settings for CommercialNews V1.
  - Keep this script focused on database creation only.

  Notes:
  - This script does NOT create schemas, logins, users, roles, or grants.
  - Collation is intentionally left as server default unless an environment requires otherwise.
  - Recovery model is environment-specific and should be handled separately if needed.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

DECLARE @DbName sysname = N'CommercialNews';

IF DB_ID(@DbName) IS NOT NULL
BEGIN
    PRINT N'Database already exists: [' + @DbName + N']';
    RETURN;
END
GO

DECLARE @DbName sysname = N'CommercialNews';
DECLARE @Sql nvarchar(max);

SET @Sql = N'CREATE DATABASE [' + @DbName + N'];';
EXEC sys.sp_executesql @Sql;

PRINT N'Created database: [' + @DbName + N']';
GO

DECLARE @DbName sysname = N'CommercialNews';
DECLARE @Sql nvarchar(max);

SET @Sql = N'
ALTER DATABASE [' + @DbName + N'] SET AUTO_CLOSE OFF;
ALTER DATABASE [' + @DbName + N'] SET AUTO_SHRINK OFF;
ALTER DATABASE [' + @DbName + N'] SET READ_COMMITTED_SNAPSHOT ON;
ALTER DATABASE [' + @DbName + N'] SET ALLOW_SNAPSHOT_ISOLATION ON;
ALTER DATABASE [' + @DbName + N'] SET PAGE_VERIFY CHECKSUM;
';

EXEC sys.sp_executesql @Sql;

PRINT N'Applied baseline settings for [' + @DbName + N']';
PRINT N' - AUTO_CLOSE OFF';
PRINT N' - AUTO_SHRINK OFF';
PRINT N' - READ_COMMITTED_SNAPSHOT ON';
PRINT N' - ALLOW_SNAPSHOT_ISOLATION ON';
PRINT N' - PAGE_VERIFY CHECKSUM';
GO