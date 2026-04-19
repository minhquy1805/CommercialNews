/*
  File: db/10_modules/authorization/001_tables.sql
  Module: Authorization
  Purpose:
  - Create Authorization truth tables for CommercialNews V1.
  - RBAC baseline:
      * Role
      * Permission
      * UserRole
      * RolePermission
  - Sync-base truth-first model.
  - Idempotent assignment/grant via composite primary keys.

  Notes:
  - ABAC is intentionally NOT modeled here in V1 phase 1.
  - Audit of grant/revoke is handled by Audit module (no local audit tables).
  - Index-heavy tuning belongs in 010_indexes.sql.
  - Stored procedures belong in 020_procs.sql.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 53001, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

USE [CommercialNews];
GO

IF SCHEMA_ID(N'authorization') IS NULL
BEGIN
    THROW 53002, 'Schema [authorization] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

IF SCHEMA_ID(N'identity') IS NULL
BEGIN
    THROW 53003, 'Schema [identity] does not exist. Run 010_create_schemas.sql first.', 1;
END
GO

IF OBJECT_ID(N'[identity].[UserAccount]', N'U') IS NULL
BEGIN
    THROW 53004, 'Table [identity].[UserAccount] does not exist. Run identity/001_tables.sql first.', 1;
END
GO

/* =========================================================
   1) [authorization].[Role]
   ========================================================= */
IF OBJECT_ID(N'[authorization].[Role]', N'U') IS NULL
BEGIN
    CREATE TABLE [authorization].[Role]
    (
        [RoleId]               BIGINT IDENTITY(1,1) NOT NULL,
        [PublicId]             CHAR(26)             NOT NULL, -- ULID

        [Name]                 NVARCHAR(80)         NOT NULL,
        [NameNormalized]       NVARCHAR(80)         NOT NULL,
        [DisplayName]          NVARCHAR(120)        NULL,
        [Description]          NVARCHAR(300)        NULL,

        [IsSystem]             BIT                  NOT NULL
            CONSTRAINT [DF_Role_IsSystem] DEFAULT (0),
        [IsActive]             BIT                  NOT NULL
            CONSTRAINT [DF_Role_IsActive] DEFAULT (1),

        [CreatedAt]            DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_Role_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt]            DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_Role_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

        [CreatedByUserId]      BIGINT               NULL,
        [UpdatedByUserId]      BIGINT               NULL,

        CONSTRAINT [PK_Role]
            PRIMARY KEY CLUSTERED ([RoleId] ASC),

        CONSTRAINT [UQ_Role_PublicId]
            UNIQUE ([PublicId]),

        CONSTRAINT [UQ_Role_NameNormalized]
            UNIQUE ([NameNormalized]),

        CONSTRAINT [FK_Role_CreatedByUser]
            FOREIGN KEY ([CreatedByUserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [FK_Role_UpdatedByUser]
            FOREIGN KEY ([UpdatedByUserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [CK_Role_Name_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),

        CONSTRAINT [CK_Role_NameNormalized_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([NameNormalized]))) > 0),

        CONSTRAINT [CK_Role_UpdatedAt]
            CHECK ([UpdatedAt] >= [CreatedAt])
    );

    PRINT N'Created table: [authorization].[Role]';
END
ELSE
BEGIN
    PRINT N'Table exists: [authorization].[Role]';
END
GO

/* =========================================================
   2) [authorization].[Permission]
   ========================================================= */
IF OBJECT_ID(N'[authorization].[Permission]', N'U') IS NULL
BEGIN
    CREATE TABLE [authorization].[Permission]
    (
        [PermissionId]         BIGINT IDENTITY(1,1) NOT NULL,
        [PublicId]             CHAR(26)             NOT NULL, -- ULID

        [Key]                  NVARCHAR(120)        NOT NULL,
        [KeyNormalized]        NVARCHAR(120)        NOT NULL,
        [Module]               NVARCHAR(50)         NULL,
        [Action]               NVARCHAR(50)         NULL,
        [Description]          NVARCHAR(300)        NULL,

        [IsSystem]             BIT                  NOT NULL
            CONSTRAINT [DF_Permission_IsSystem] DEFAULT (0),
        [IsActive]             BIT                  NOT NULL
            CONSTRAINT [DF_Permission_IsActive] DEFAULT (1),

        [CreatedAt]            DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_Permission_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt]            DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_Permission_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

        [CreatedByUserId]      BIGINT               NULL,
        [UpdatedByUserId]      BIGINT               NULL,

        CONSTRAINT [PK_Permission]
            PRIMARY KEY CLUSTERED ([PermissionId] ASC),

        CONSTRAINT [UQ_Permission_PublicId]
            UNIQUE ([PublicId]),

        CONSTRAINT [UQ_Permission_KeyNormalized]
            UNIQUE ([KeyNormalized]),

        CONSTRAINT [FK_Permission_CreatedByUser]
            FOREIGN KEY ([CreatedByUserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [FK_Permission_UpdatedByUser]
            FOREIGN KEY ([UpdatedByUserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [CK_Permission_Key_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([Key]))) > 0),

        CONSTRAINT [CK_Permission_KeyNormalized_NotBlank]
            CHECK (LEN(LTRIM(RTRIM([KeyNormalized]))) > 0),

        CONSTRAINT [CK_Permission_UpdatedAt]
            CHECK ([UpdatedAt] >= [CreatedAt])
    );

    PRINT N'Created table: [authorization].[Permission]';
END
ELSE
BEGIN
    PRINT N'Table exists: [authorization].[Permission]';
END
GO

/* =========================================================
   3) [authorization].[UserRole]
   ========================================================= */
IF OBJECT_ID(N'[authorization].[UserRole]', N'U') IS NULL
BEGIN
    CREATE TABLE [authorization].[UserRole]
    (
        [UserId]               BIGINT               NOT NULL,
        [RoleId]               BIGINT               NOT NULL,

        [AssignedAt]           DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_UserRole_AssignedAt] DEFAULT (SYSUTCDATETIME()),
        [AssignedByUserId]     BIGINT               NULL,

        CONSTRAINT [PK_UserRole]
            PRIMARY KEY CLUSTERED ([UserId] ASC, [RoleId] ASC),

        CONSTRAINT [FK_UserRole_User]
            FOREIGN KEY ([UserId])
            REFERENCES [identity].[UserAccount]([UserId]),

        CONSTRAINT [FK_UserRole_Role]
            FOREIGN KEY ([RoleId])
            REFERENCES [authorization].[Role]([RoleId]),

        CONSTRAINT [FK_UserRole_AssignedByUser]
            FOREIGN KEY ([AssignedByUserId])
            REFERENCES [identity].[UserAccount]([UserId])
    );

    PRINT N'Created table: [authorization].[UserRole]';
END
ELSE
BEGIN
    PRINT N'Table exists: [authorization].[UserRole]';
END
GO

/* =========================================================
   4) [authorization].[RolePermission]
   ========================================================= */
IF OBJECT_ID(N'[authorization].[RolePermission]', N'U') IS NULL
BEGIN
    CREATE TABLE [authorization].[RolePermission]
    (
        [RoleId]               BIGINT               NOT NULL,
        [PermissionId]         BIGINT               NOT NULL,

        [GrantedAt]            DATETIME2(3)         NOT NULL
            CONSTRAINT [DF_RolePermission_GrantedAt] DEFAULT (SYSUTCDATETIME()),
        [GrantedByUserId]      BIGINT               NULL,

        CONSTRAINT [PK_RolePermission]
            PRIMARY KEY CLUSTERED ([RoleId] ASC, [PermissionId] ASC),

        CONSTRAINT [FK_RolePermission_Role]
            FOREIGN KEY ([RoleId])
            REFERENCES [authorization].[Role]([RoleId]),

        CONSTRAINT [FK_RolePermission_Permission]
            FOREIGN KEY ([PermissionId])
            REFERENCES [authorization].[Permission]([PermissionId]),

        CONSTRAINT [FK_RolePermission_GrantedByUser]
            FOREIGN KEY ([GrantedByUserId])
            REFERENCES [identity].[UserAccount]([UserId])
    );

    PRINT N'Created table: [authorization].[RolePermission]';
END
ELSE
BEGIN
    PRINT N'Table exists: [authorization].[RolePermission]';
END
GO