/*
  File: db/10_modules/authorization/010_indexes.sql
  Module: Authorization
  Purpose:
  - Create non-PK/non-constraint indexes for Authorization tables in CommercialNews V1.
  - Includes:
      * [authorization].[Role]
      * [authorization].[Permission]
      * [authorization].[UserRole]
      * [authorization].[RolePermission]

  Notes:
  - Idempotent: safe to re-run.
  - Unique constraints on names/public IDs are already defined in 001_tables.sql.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 53101, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

USE [CommercialNews];
GO

IF SCHEMA_ID(N'authorization') IS NULL
BEGIN
    THROW 53102, 'Schema [authorization] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

IF OBJECT_ID(N'[authorization].[Role]', N'U') IS NULL
BEGIN
    THROW 53103, 'Table [authorization].[Role] does not exist. Run authorization/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[authorization].[Permission]', N'U') IS NULL
BEGIN
    THROW 53104, 'Table [authorization].[Permission] does not exist. Run authorization/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[authorization].[UserRole]', N'U') IS NULL
BEGIN
    THROW 53105, 'Table [authorization].[UserRole] does not exist. Run authorization/001_tables.sql first.', 1;
END
GO

IF OBJECT_ID(N'[authorization].[RolePermission]', N'U') IS NULL
BEGIN
    THROW 53106, 'Table [authorization].[RolePermission] does not exist. Run authorization/001_tables.sql first.', 1;
END
GO

/* =========================================================
   Role indexes
   ========================================================= */

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_Role_IsActive_Name'
      AND [object_id] = OBJECT_ID(N'[authorization].[Role]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Role_IsActive_Name]
    ON [authorization].[Role]
    (
        [IsActive] ASC,
        [Name] ASC
    )
    INCLUDE
    (
        [PublicId],
        [Description],
        [IsSystem],
        [UpdatedAt]
    );

    PRINT N'Created index: [authorization].[Role].[IX_Role_IsActive_Name]';
END
ELSE
BEGIN
    PRINT N'Index exists: [authorization].[Role].[IX_Role_IsActive_Name]';
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_Role_CreatedByUserId'
      AND [object_id] = OBJECT_ID(N'[authorization].[Role]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Role_CreatedByUserId]
    ON [authorization].[Role] ([CreatedByUserId] ASC);

    PRINT N'Created index: [authorization].[Role].[IX_Role_CreatedByUserId]';
END
ELSE
BEGIN
    PRINT N'Index exists: [authorization].[Role].[IX_Role_CreatedByUserId]';
END
GO

/* =========================================================
   Permission indexes
   ========================================================= */

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_Permission_IsActive_Module_Name'
      AND [object_id] = OBJECT_ID(N'[authorization].[Permission]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Permission_IsActive_Module_Name]
    ON [authorization].[Permission]
    (
        [IsActive] ASC,
        [Module] ASC,
        [Name] ASC
    )
    INCLUDE
    (
        [PublicId],
        [Description],
        [IsSystem],
        [UpdatedAt]
    );

    PRINT N'Created index: [authorization].[Permission].[IX_Permission_IsActive_Module_Name]';
END
ELSE
BEGIN
    PRINT N'Index exists: [authorization].[Permission].[IX_Permission_IsActive_Module_Name]';
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_Permission_CreatedByUserId'
      AND [object_id] = OBJECT_ID(N'[authorization].[Permission]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Permission_CreatedByUserId]
    ON [authorization].[Permission] ([CreatedByUserId] ASC);

    PRINT N'Created index: [authorization].[Permission].[IX_Permission_CreatedByUserId]';
END
ELSE
BEGIN
    PRINT N'Index exists: [authorization].[Permission].[IX_Permission_CreatedByUserId]';
END
GO

/* =========================================================
   UserRole indexes
   ========================================================= */

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'UX_UserRole_UserId_RoleId_Active'
      AND [object_id] = OBJECT_ID(N'[authorization].[UserRole]')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UX_UserRole_UserId_RoleId_Active]
    ON [authorization].[UserRole]
    (
        [UserId] ASC,
        [RoleId] ASC
    )
    WHERE [RevokedAt] IS NULL;

    PRINT N'Created index: [authorization].[UserRole].[UX_UserRole_UserId_RoleId_Active]';
END
ELSE
BEGIN
    PRINT N'Index exists: [authorization].[UserRole].[UX_UserRole_UserId_RoleId_Active]';
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_UserRole_UserId_RevokedAt_AssignedAt'
      AND [object_id] = OBJECT_ID(N'[authorization].[UserRole]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_UserRole_UserId_RevokedAt_AssignedAt]
    ON [authorization].[UserRole]
    (
        [UserId] ASC,
        [RevokedAt] ASC,
        [AssignedAt] DESC
    )
    INCLUDE
    (
        [RoleId],
        [AssignedByUserId],
        [RevokedByUserId]
    );

    PRINT N'Created index: [authorization].[UserRole].[IX_UserRole_UserId_RevokedAt_AssignedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [authorization].[UserRole].[IX_UserRole_UserId_RevokedAt_AssignedAt]';
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_UserRole_RoleId_RevokedAt_AssignedAt'
      AND [object_id] = OBJECT_ID(N'[authorization].[UserRole]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_UserRole_RoleId_RevokedAt_AssignedAt]
    ON [authorization].[UserRole]
    (
        [RoleId] ASC,
        [RevokedAt] ASC,
        [AssignedAt] DESC
    )
    INCLUDE
    (
        [UserId],
        [AssignedByUserId],
        [RevokedByUserId]
    );

    PRINT N'Created index: [authorization].[UserRole].[IX_UserRole_RoleId_RevokedAt_AssignedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [authorization].[UserRole].[IX_UserRole_RoleId_RevokedAt_AssignedAt]';
END
GO

/* =========================================================
   RolePermission indexes
   ========================================================= */

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'UX_RolePermission_RoleId_PermissionId_Active'
      AND [object_id] = OBJECT_ID(N'[authorization].[RolePermission]')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UX_RolePermission_RoleId_PermissionId_Active]
    ON [authorization].[RolePermission]
    (
        [RoleId] ASC,
        [PermissionId] ASC
    )
    WHERE [RevokedAt] IS NULL;

    PRINT N'Created index: [authorization].[RolePermission].[UX_RolePermission_RoleId_PermissionId_Active]';
END
ELSE
BEGIN
    PRINT N'Index exists: [authorization].[RolePermission].[UX_RolePermission_RoleId_PermissionId_Active]';
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_RolePermission_RoleId_RevokedAt_GrantedAt'
      AND [object_id] = OBJECT_ID(N'[authorization].[RolePermission]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_RolePermission_RoleId_RevokedAt_GrantedAt]
    ON [authorization].[RolePermission]
    (
        [RoleId] ASC,
        [RevokedAt] ASC,
        [GrantedAt] DESC
    )
    INCLUDE
    (
        [PermissionId],
        [GrantedByUserId],
        [RevokedByUserId]
    );

    PRINT N'Created index: [authorization].[RolePermission].[IX_RolePermission_RoleId_RevokedAt_GrantedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [authorization].[RolePermission].[IX_RolePermission_RoleId_RevokedAt_GrantedAt]';
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_RolePermission_PermissionId_RevokedAt_GrantedAt'
      AND [object_id] = OBJECT_ID(N'[authorization].[RolePermission]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_RolePermission_PermissionId_RevokedAt_GrantedAt]
    ON [authorization].[RolePermission]
    (
        [PermissionId] ASC,
        [RevokedAt] ASC,
        [GrantedAt] DESC
    )
    INCLUDE
    (
        [RoleId],
        [GrantedByUserId],
        [RevokedByUserId]
    );

    PRINT N'Created index: [authorization].[RolePermission].[IX_RolePermission_PermissionId_RevokedAt_GrantedAt]';
END
ELSE
BEGIN
    PRINT N'Index exists: [authorization].[RolePermission].[IX_RolePermission_PermissionId_RevokedAt_GrantedAt]';
END
GO