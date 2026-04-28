/*
  010_create_schemas.sql

  Purpose:
  - Create SQL module schemas (idempotent) for CommercialNews V1.
  - This script belongs to 00_bootstrap and should be run after 001_create_database.sql.

  Notes:
  - This script is SQL-only (SSMS friendly). No SQLCMD mode required.
  - Schema names should match physical SQL-backed modules.
  - Not every logical module must have a dedicated schema in V1.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 50001, 'Database [CommercialNews] does not exist. Run 001_create_database.sql first.', 1;
END
GO

USE [CommercialNews];
GO

-- =========================================================
-- Core SQL-backed module schemas (V1)
-- Keep these in alphabetical order.
-- Add new schema blocks here only when a module has real SQL-backed objects.
-- =========================================================

IF SCHEMA_ID(N'audit') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA [audit]');
    PRINT N'Created schema: [audit]';
END
ELSE
    PRINT N'Schema exists: [audit]';

IF SCHEMA_ID(N'authorization') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA [authorization]');
    PRINT N'Created schema: [authorization]';
END
ELSE
    PRINT N'Schema exists: [authorization]';

IF SCHEMA_ID(N'content') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA [content]');
    PRINT N'Created schema: [content]';
END
ELSE
    PRINT N'Schema exists: [content]';

IF SCHEMA_ID(N'identity') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA [identity]');
    PRINT N'Created schema: [identity]';
END
ELSE
    PRINT N'Schema exists: [identity]';

IF SCHEMA_ID(N'interaction') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA [interaction]');
    PRINT N'Created schema: [interaction]';
END
ELSE
    PRINT N'Schema exists: [interaction]';

IF SCHEMA_ID(N'media') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA [media]');
    PRINT N'Created schema: [media]';
END
ELSE
    PRINT N'Schema exists: [media]';

IF SCHEMA_ID(N'notifications') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA [notifications]');
    PRINT N'Created schema: [notifications]';
END
ELSE
    PRINT N'Schema exists: [notifications]';

IF SCHEMA_ID(N'outbox') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA [outbox]');
    PRINT N'Created schema: [outbox]';
END
ELSE
    PRINT N'Schema exists: [outbox]';

IF SCHEMA_ID(N'reading') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA [reading]');
    PRINT N'Created schema: [reading]';
END
ELSE
    PRINT N'Schema exists: [reading]';

IF SCHEMA_ID(N'seo') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA [seo]');
    PRINT N'Created schema: [seo]';
END
ELSE
    PRINT N'Schema exists: [seo]';

GO