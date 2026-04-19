/*
  File: db/10_modules/authorization/010_indexes.sql
  Module: Authorization
  Purpose:
  - Create non-PK/non-constraint indexes for Authorization tables in CommercialNews V1.
  - Sync-base truth-first authorization model.
  - Supports:
      * authoritative admin reads
      * role / permission listing
      * user -> roles lookup
      * role -> users lookup
      * role -> permissions lookup
      * permission -> roles reverse lookup

  Notes:
  - Idempotent: safe to re-run.
  - PKs / UNIQUE constraints are already defined in 001_tables.sql.
  - Do not duplicate indexes already covered by PK/UQ constraints.
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
   [authorization].[Role]
   ========================================================= */

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_Role_IsActive_NameNormalized'
      AND [object_id] = OBJECT_ID(N'[authorization].[Role]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Role_IsActive_NameNormalized]
    ON [authorization].[Role]
    (
        [IsActive] ASC,
        [NameNormalized] ASC
    )
    INCLUDE
    (
        [PublicId],
        [Name],
        [DisplayName],
        [Description],
        [IsSystem],
        [UpdatedAt]
    );

    PRINT N'Created index: [authorization].[Role].[IX_Role_IsActive_NameNormalized]';
END
ELSE
BEGIN
    PRINT N'Index exists: [authorization].[Role].[IX_Role_IsActive_NameNormalized]';
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

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_Role_UpdatedByUserId'
      AND [object_id] = OBJECT_ID(N'[authorization].[Role]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Role_UpdatedByUserId]
    ON [authorization].[Role] ([UpdatedByUserId] ASC);

    PRINT N'Created index: [authorization].[Role].[IX_Role_UpdatedByUserId]';
END
ELSE
BEGIN
    PRINT N'Index exists: [authorization].[Role].[IX_Role_UpdatedByUserId]';
END
GO

/* =========================================================
   [authorization].[Permission]
   ========================================================= */

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_Permission_IsActive_Module_Action'
      AND [object_id] = OBJECT_ID(N'[authorization].[Permission]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Permission_IsActive_Module_Action]
    ON [authorization].[Permission]
    (
        [IsActive] ASC,
        [Module] ASC,
        [Action] ASC
    )
    INCLUDE
    (
        [PublicId],
        [Key],
        [Description],
        [IsSystem],
        [UpdatedAt]
    );

    PRINT N'Created index: [authorization].[Permission].[IX_Permission_IsActive_Module_Action]';
END
ELSE
BEGIN
    PRINT N'Index exists: [authorization].[Permission].[IX_Permission_IsActive_Module_Action]';
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_Permission_IsActive_KeyNormalized'
      AND [object_id] = OBJECT_ID(N'[authorization].[Permission]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Permission_IsActive_KeyNormalized]
    ON [authorization].[Permission]
    (
        [IsActive] ASC,
        [KeyNormalized] ASC
    )
    INCLUDE
    (
        [PublicId],
        [Key],
        [Module],
        [Action],
        [Description],
        [IsSystem],
        [UpdatedAt]
    );

    PRINT N'Created index: [authorization].[Permission].[IX_Permission_IsActive_KeyNormalized]';
END
ELSE
BEGIN
    PRINT N'Index exists: [authorization].[Permission].[IX_Permission_IsActive_KeyNormalized]';
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

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_Permission_UpdatedByUserId'
      AND [object_id] = OBJECT_ID(N'[authorization].[Permission]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Permission_UpdatedByUserId]
    ON [authorization].[Permission] ([UpdatedByUserId] ASC);

    PRINT N'Created index: [authorization].[Permission].[IX_Permission_UpdatedByUserId]';
END
ELSE
BEGIN
    PRINT N'Index exists: [authorization].[Permission].[IX_Permission_UpdatedByUserId]';
END
GO

/* =========================================================
   [authorization].[UserRole]
   ========================================================= */

-- PK already covers (UserId, RoleId) for direct user -> roles lookup.
-- Add reverse lookup for role -> users and admin tooling.

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_UserRole_RoleId_UserId'
      AND [object_id] = OBJECT_ID(N'[authorization].[UserRole]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_UserRole_RoleId_UserId]
    ON [authorization].[UserRole]
    (
        [RoleId] ASC,
        [UserId] ASC
    )
    INCLUDE
    (
        [AssignedAt],
        [AssignedByUserId]
    );

    PRINT N'Created index: [authorization].[UserRole].[IX_UserRole_RoleId_UserId]';
END
ELSE
BEGIN
    PRINT N'Index exists: [authorization].[UserRole].[IX_UserRole_RoleId_UserId]';
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_UserRole_AssignedByUserId'
      AND [object_id] = OBJECT_ID(N'[authorization].[UserRole]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_UserRole_AssignedByUserId]
    ON [authorization].[UserRole] ([AssignedByUserId] ASC);

    PRINT N'Created index: [authorization].[UserRole].[IX_UserRole_AssignedByUserId]';
END
ELSE
BEGIN
    PRINT N'Index exists: [authorization].[UserRole].[IX_UserRole_AssignedByUserId]';
END
GO

/* =========================================================
   [authorization].[RolePermission]
   ========================================================= */

-- PK already covers (RoleId, PermissionId) for direct role -> permissions lookup.
-- Add reverse lookup for permission -> roles and admin tooling.

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_RolePermission_PermissionId_RoleId'
      AND [object_id] = OBJECT_ID(N'[authorization].[RolePermission]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_RolePermission_PermissionId_RoleId]
    ON [authorization].[RolePermission]
    (
        [PermissionId] ASC,
        [RoleId] ASC
    )
    INCLUDE
    (
        [GrantedAt],
        [GrantedByUserId]
    );

    PRINT N'Created index: [authorization].[RolePermission].[IX_RolePermission_PermissionId_RoleId]';
END
ELSE
BEGIN
    PRINT N'Index exists: [authorization].[RolePermission].[IX_RolePermission_PermissionId_RoleId]';
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_RolePermission_GrantedByUserId'
      AND [object_id] = OBJECT_ID(N'[authorization].[RolePermission]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_RolePermission_GrantedByUserId]
    ON [authorization].[RolePermission] ([GrantedByUserId] ASC);

    PRINT N'Created index: [authorization].[RolePermission].[IX_RolePermission_GrantedByUserId]';
END
ELSE
BEGIN
    PRINT N'Index exists: [authorization].[RolePermission].[IX_RolePermission_GrantedByUserId]';
END
GO