/*
  File: db/10_modules/authorization/020_procs.sql
  Module: Authorization
  Purpose:
  - Create stored procedures for Authorization V1.
  - RBAC baseline operations for:
      * Role
      * Permission
      * UserRole assignment/revoke
      * RolePermission grant/revoke
  - Idempotent: uses CREATE OR ALTER PROCEDURE

  Notes:
  - UserRole and RolePermission use revoke semantics instead of hard delete.
  - Assignment/grant operations are idempotent.
*/

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF DB_ID(N'CommercialNews') IS NULL
BEGIN
    THROW 53201, 'Database [CommercialNews] does not exist. Run bootstrap scripts first.', 1;
END
GO

USE [CommercialNews];
GO

IF SCHEMA_ID(N'authorization') IS NULL
BEGIN
    THROW 53202, 'Schema [authorization] does not exist. Run bootstrap scripts first.', 1;
END
GO

/* =========================================================
   Role
   ========================================================= */

CREATE OR ALTER PROCEDURE [authorization].[Role_Insert]
    @PublicId          CHAR(26),
    @Name              NVARCHAR(100),
    @NameNormalized    NVARCHAR(100),
    @Description       NVARCHAR(500) = NULL,
    @IsSystem          BIT = 0,
    @IsActive          BIT = 1,
    @CreatedByUserId   BIGINT = NULL,
    @RoleId            BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NULLIF(LTRIM(RTRIM(@PublicId)), N'') IS NULL
        THROW 53310, 'Role public id is required.', 1;

    IF NULLIF(LTRIM(RTRIM(@Name)), N'') IS NULL
        THROW 53311, 'Role name is required.', 1;

    IF NULLIF(LTRIM(RTRIM(@NameNormalized)), N'') IS NULL
        THROW 53312, 'Role normalized name is required.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [authorization].[Role]
        WHERE [NameNormalized] = @NameNormalized
    )
        THROW 53313, 'Role name already exists.', 1;

    INSERT INTO [authorization].[Role]
    (
        [PublicId],
        [Name],
        [NameNormalized],
        [Description],
        [IsSystem],
        [IsActive],
        [CreatedByUserId],
        [UpdatedByUserId]
    )
    VALUES
    (
        @PublicId,
        @Name,
        @NameNormalized,
        @Description,
        @IsSystem,
        @IsActive,
        @CreatedByUserId,
        @CreatedByUserId
    );

    SET @RoleId = SCOPE_IDENTITY();
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[Role_Update]
    @RoleId            BIGINT,
    @Name              NVARCHAR(100),
    @NameNormalized    NVARCHAR(100),
    @Description       NVARCHAR(500) = NULL,
    @IsActive          BIT,
    @UpdatedByUserId   BIGINT = NULL,
    @AffectedRows      INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @RoleId <= 0
        THROW 53314, 'Role id must be greater than zero.', 1;

    IF NULLIF(LTRIM(RTRIM(@Name)), N'') IS NULL
        THROW 53311, 'Role name is required.', 1;

    IF NULLIF(LTRIM(RTRIM(@NameNormalized)), N'') IS NULL
        THROW 53312, 'Role normalized name is required.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [authorization].[Role]
        WHERE [RoleId] = @RoleId
    )
        THROW 53316, 'Role was not found.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [authorization].[Role]
        WHERE [NameNormalized] = @NameNormalized
          AND [RoleId] <> @RoleId
    )
        THROW 53313, 'Role name already exists.', 1;

    UPDATE [authorization].[Role]
    SET
        [Name] = @Name,
        [NameNormalized] = @NameNormalized,
        [Description] = @Description,
        [IsActive] = @IsActive,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedByUserId] = @UpdatedByUserId
    WHERE [RoleId] = @RoleId;

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[Role_Delete]
    @RoleId BIGINT,
    @AffectedRows INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @RoleId <= 0
        THROW 53314, 'Role id must be greater than zero.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [authorization].[Role]
        WHERE [RoleId] = @RoleId
    )
        THROW 53316, 'Role was not found.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [authorization].[Role]
        WHERE [RoleId] = @RoleId
          AND [IsSystem] = 1
    )
        THROW 53315, 'System role is protected.', 1;

    DELETE FROM [authorization].[Role]
    WHERE [RoleId] = @RoleId;

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[Role_SelectById]
    @RoleId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [RoleId],
        [PublicId],
        [Name],
        [NameNormalized],
        [Description],
        [IsSystem],
        [IsActive],
        [CreatedAt],
        [UpdatedAt],
        [CreatedByUserId],
        [UpdatedByUserId]
    FROM [authorization].[Role]
    WHERE [RoleId] = @RoleId;
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[Role_SelectByNameNormalized]
    @NameNormalized NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [RoleId],
        [PublicId],
        [Name],
        [NameNormalized],
        [Description],
        [IsSystem],
        [IsActive],
        [CreatedAt],
        [UpdatedAt],
        [CreatedByUserId],
        [UpdatedByUserId]
    FROM [authorization].[Role]
    WHERE [NameNormalized] = @NameNormalized;
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[Role_SelectAll]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [RoleId],
        [PublicId],
        [Name],
        [NameNormalized],
        [Description],
        [IsSystem],
        [IsActive],
        [CreatedAt],
        [UpdatedAt],
        [CreatedByUserId],
        [UpdatedByUserId]
    FROM [authorization].[Role]
    ORDER BY [Name] ASC;
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[Role_SelectSkipAndTake]
    @Skip INT,
    @Take INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;

    SELECT
        [RoleId],
        [PublicId],
        [Name],
        [NameNormalized],
        [Description],
        [IsSystem],
        [IsActive],
        [CreatedAt],
        [UpdatedAt],
        [CreatedByUserId],
        [UpdatedByUserId]
    FROM [authorization].[Role]
    ORDER BY [Name] ASC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[Role_SelectSkipAndTakeWhereDynamic]
    @NameContains NVARCHAR(100) = NULL,
    @IsActive BIT = NULL,
    @Skip INT = 0,
    @Take INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;

    SELECT
        [RoleId],
        [PublicId],
        [Name],
        [NameNormalized],
        [Description],
        [IsSystem],
        [IsActive],
        [CreatedAt],
        [UpdatedAt],
        [CreatedByUserId],
        [UpdatedByUserId]
    FROM [authorization].[Role]
    WHERE
        (@NameContains IS NULL OR [Name] LIKE N'%' + @NameContains + N'%')
        AND (@IsActive IS NULL OR [IsActive] = @IsActive)
    ORDER BY [Name] ASC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[Role_GetRecordCount]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT_BIG(1) AS [RecordCount]
    FROM [authorization].[Role];
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[Role_GetRecordCountWhereDynamic]
    @NameContains NVARCHAR(100) = NULL,
    @IsActive BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT_BIG(1) AS [RecordCount]
    FROM [authorization].[Role]
    WHERE
        (@NameContains IS NULL OR [Name] LIKE N'%' + @NameContains + N'%')
        AND (@IsActive IS NULL OR [IsActive] = @IsActive);
END;
GO

/* =========================================================
   Permission
   ========================================================= */

CREATE OR ALTER PROCEDURE [authorization].[Permission_Insert]
    @PublicId          CHAR(26),
    @Name              NVARCHAR(150),
    @NameNormalized    NVARCHAR(150),
    @Description       NVARCHAR(500) = NULL,
    @Module            NVARCHAR(100) = NULL,
    @IsSystem          BIT = 0,
    @IsActive          BIT = 1,
    @CreatedByUserId   BIGINT = NULL,
    @PermissionId      BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NULLIF(LTRIM(RTRIM(@PublicId)), N'') IS NULL
        THROW 53330, 'Permission public id is required.', 1;

    IF NULLIF(LTRIM(RTRIM(@Name)), N'') IS NULL
        THROW 53331, 'Permission name is required.', 1;

    IF NULLIF(LTRIM(RTRIM(@NameNormalized)), N'') IS NULL
        THROW 53332, 'Permission normalized name is required.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [authorization].[Permission]
        WHERE [NameNormalized] = @NameNormalized
    )
        THROW 53333, 'Permission name already exists.', 1;

    INSERT INTO [authorization].[Permission]
    (
        [PublicId],
        [Name],
        [NameNormalized],
        [Description],
        [Module],
        [IsSystem],
        [IsActive],
        [CreatedByUserId],
        [UpdatedByUserId]
    )
    VALUES
    (
        @PublicId,
        @Name,
        @NameNormalized,
        @Description,
        @Module,
        @IsSystem,
        @IsActive,
        @CreatedByUserId,
        @CreatedByUserId
    );

    SET @PermissionId = SCOPE_IDENTITY();
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[Permission_Update]
    @PermissionId      BIGINT,
    @Name              NVARCHAR(150),
    @NameNormalized    NVARCHAR(150),
    @Description       NVARCHAR(500) = NULL,
    @Module            NVARCHAR(100) = NULL,
    @IsActive          BIT,
    @UpdatedByUserId   BIGINT = NULL,
    @AffectedRows      INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @PermissionId <= 0
        THROW 53334, 'Permission id must be greater than zero.', 1;

    IF NULLIF(LTRIM(RTRIM(@Name)), N'') IS NULL
        THROW 53331, 'Permission name is required.', 1;

    IF NULLIF(LTRIM(RTRIM(@NameNormalized)), N'') IS NULL
        THROW 53332, 'Permission normalized name is required.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [authorization].[Permission]
        WHERE [PermissionId] = @PermissionId
    )
        THROW 53336, 'Permission was not found.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [authorization].[Permission]
        WHERE [NameNormalized] = @NameNormalized
          AND [PermissionId] <> @PermissionId
    )
        THROW 53333, 'Permission name already exists.', 1;

    UPDATE [authorization].[Permission]
    SET
        [Name] = @Name,
        [NameNormalized] = @NameNormalized,
        [Description] = @Description,
        [Module] = @Module,
        [IsActive] = @IsActive,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedByUserId] = @UpdatedByUserId
    WHERE [PermissionId] = @PermissionId;

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[Permission_Delete]
    @PermissionId BIGINT,
    @AffectedRows INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @PermissionId <= 0
        THROW 53334, 'Permission id must be greater than zero.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [authorization].[Permission]
        WHERE [PermissionId] = @PermissionId
    )
        THROW 53336, 'Permission was not found.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [authorization].[Permission]
        WHERE [PermissionId] = @PermissionId
          AND [IsSystem] = 1
    )
        THROW 53335, 'System permission is protected.', 1;

    DELETE FROM [authorization].[Permission]
    WHERE [PermissionId] = @PermissionId;

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[Permission_SelectById]
    @PermissionId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [PermissionId],
        [PublicId],
        [Name],
        [NameNormalized],
        [Description],
        [Module],
        [IsSystem],
        [IsActive],
        [CreatedAt],
        [UpdatedAt],
        [CreatedByUserId],
        [UpdatedByUserId]
    FROM [authorization].[Permission]
    WHERE [PermissionId] = @PermissionId;
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[Permission_SelectByNameNormalized]
    @NameNormalized NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [PermissionId],
        [PublicId],
        [Name],
        [NameNormalized],
        [Description],
        [Module],
        [IsSystem],
        [IsActive],
        [CreatedAt],
        [UpdatedAt],
        [CreatedByUserId],
        [UpdatedByUserId]
    FROM [authorization].[Permission]
    WHERE [NameNormalized] = @NameNormalized;
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[Permission_SelectAll]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [PermissionId],
        [PublicId],
        [Name],
        [NameNormalized],
        [Description],
        [Module],
        [IsSystem],
        [IsActive],
        [CreatedAt],
        [UpdatedAt],
        [CreatedByUserId],
        [UpdatedByUserId]
    FROM [authorization].[Permission]
    ORDER BY [Name] ASC;
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[Permission_SelectSkipAndTake]
    @Skip INT,
    @Take INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;

    SELECT
        [PermissionId],
        [PublicId],
        [Name],
        [NameNormalized],
        [Description],
        [Module],
        [IsSystem],
        [IsActive],
        [CreatedAt],
        [UpdatedAt],
        [CreatedByUserId],
        [UpdatedByUserId]
    FROM [authorization].[Permission]
    ORDER BY [Name] ASC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[Permission_SelectSkipAndTakeWhereDynamic]
    @NameContains NVARCHAR(150) = NULL,
    @Module NVARCHAR(100) = NULL,
    @IsActive BIT = NULL,
    @Skip INT = 0,
    @Take INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;

    SELECT
        [PermissionId],
        [PublicId],
        [Name],
        [NameNormalized],
        [Description],
        [Module],
        [IsSystem],
        [IsActive],
        [CreatedAt],
        [UpdatedAt],
        [CreatedByUserId],
        [UpdatedByUserId]
    FROM [authorization].[Permission]
    WHERE
        (@NameContains IS NULL OR [Name] LIKE N'%' + @NameContains + N'%')
        AND (@Module IS NULL OR [Module] = @Module)
        AND (@IsActive IS NULL OR [IsActive] = @IsActive)
    ORDER BY [Name] ASC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[Permission_GetRecordCount]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT_BIG(1) AS [RecordCount]
    FROM [authorization].[Permission];
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[Permission_GetRecordCountWhereDynamic]
    @NameContains NVARCHAR(150) = NULL,
    @Module NVARCHAR(100) = NULL,
    @IsActive BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT_BIG(1) AS [RecordCount]
    FROM [authorization].[Permission]
    WHERE
        (@NameContains IS NULL OR [Name] LIKE N'%' + @NameContains + N'%')
        AND (@Module IS NULL OR [Module] = @Module)
        AND (@IsActive IS NULL OR [IsActive] = @IsActive);
END;
GO

/* =========================================================
   UserRole
   ========================================================= */

CREATE OR ALTER PROCEDURE [authorization].[UserRole_Assign]
    @UserId             BIGINT,
    @RoleId             BIGINT,
    @AssignedByUserId   BIGINT = NULL,
    @UserRoleId         BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @UserId <= 0
        THROW 53352, 'User id must be greater than zero.', 1;

    IF @RoleId <= 0
        THROW 53353, 'Role id must be greater than zero.', 1;

    SET @UserRoleId = NULL;

    SELECT TOP (1)
        @UserRoleId = [UserRoleId]
    FROM [authorization].[UserRole]
    WHERE [UserId] = @UserId
      AND [RoleId] = @RoleId
      AND [RevokedAt] IS NULL;

    IF @UserRoleId IS NOT NULL
        RETURN;

    INSERT INTO [authorization].[UserRole]
    (
        [UserId],
        [RoleId],
        [AssignedByUserId]
    )
    VALUES
    (
        @UserId,
        @RoleId,
        @AssignedByUserId
    );

    SET @UserRoleId = SCOPE_IDENTITY();
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[UserRole_Revoke]
    @UserId             BIGINT,
    @RoleId             BIGINT,
    @RevokedByUserId    BIGINT = NULL,
    @AffectedRows       INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @UserId <= 0
        THROW 53352, 'User id must be greater than zero.', 1;

    IF @RoleId <= 0
        THROW 53353, 'Role id must be greater than zero.', 1;

    UPDATE [authorization].[UserRole]
    SET
        [RevokedAt] = SYSUTCDATETIME(),
        [RevokedByUserId] = @RevokedByUserId
    WHERE [UserId] = @UserId
      AND [RoleId] = @RoleId
      AND [RevokedAt] IS NULL;

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[UserRole_SelectRolesByUserId]
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ur.[UserRoleId],
        ur.[UserId],
        ur.[RoleId],
        ur.[AssignedAt],
        ur.[AssignedByUserId],
        ur.[RevokedAt],
        ur.[RevokedByUserId],
        r.[PublicId] AS [RolePublicId],
        r.[Name] AS [RoleName],
        r.[NameNormalized] AS [RoleNameNormalized],
        r.[Description] AS [RoleDescription],
        r.[IsSystem] AS [RoleIsSystem],
        r.[IsActive] AS [RoleIsActive]
    FROM [authorization].[UserRole] ur
    INNER JOIN [authorization].[Role] r
        ON ur.[RoleId] = r.[RoleId]
    WHERE ur.[UserId] = @UserId
      AND ur.[RevokedAt] IS NULL
    ORDER BY ur.[AssignedAt] DESC, ur.[UserRoleId] DESC;
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[UserRole_SelectUsersByRoleId]
    @RoleId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ur.[UserRoleId],
        ur.[UserId],
        ur.[RoleId],
        ur.[AssignedAt],
        ur.[AssignedByUserId],
        ur.[RevokedAt],
        ur.[RevokedByUserId],
        ua.[PublicId] AS [UserPublicId],
        ua.[Email],
        ua.[EmailNormalized],
        ua.[FullName],
        ua.[IsEmailVerified],
        ua.[Status]
    FROM [authorization].[UserRole] ur
    INNER JOIN [identity].[UserAccount] ua
        ON ur.[UserId] = ua.[UserId]
    WHERE ur.[RoleId] = @RoleId
      AND ur.[RevokedAt] IS NULL
    ORDER BY ur.[AssignedAt] DESC, ur.[UserRoleId] DESC;
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[UserRole_SelectActiveByUserIdAndRoleId]
    @UserId BIGINT,
    @RoleId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        [UserRoleId],
        [UserId],
        [RoleId],
        [AssignedAt],
        [AssignedByUserId],
        [RevokedAt],
        [RevokedByUserId]
    FROM [authorization].[UserRole]
    WHERE [UserId] = @UserId
      AND [RoleId] = @RoleId
      AND [RevokedAt] IS NULL;
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[UserRole_GetRecordCountByUserId]
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT_BIG(1) AS [RecordCount]
    FROM [authorization].[UserRole]
    WHERE [UserId] = @UserId;
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[UserRole_GetRecordCountByRoleId]
    @RoleId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT_BIG(1) AS [RecordCount]
    FROM [authorization].[UserRole]
    WHERE [RoleId] = @RoleId;
END;
GO

/* =========================================================
   RolePermission
   ========================================================= */

CREATE OR ALTER PROCEDURE [authorization].[RolePermission_Grant]
    @RoleId             BIGINT,
    @PermissionId       BIGINT,
    @GrantedByUserId    BIGINT = NULL,
    @RolePermissionId   BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @RoleId <= 0
        THROW 53372, 'Role id must be greater than zero.', 1;

    IF @PermissionId <= 0
        THROW 53373, 'Permission id must be greater than zero.', 1;

    SET @RolePermissionId = NULL;

    SELECT TOP (1)
        @RolePermissionId = [RolePermissionId]
    FROM [authorization].[RolePermission]
    WHERE [RoleId] = @RoleId
      AND [PermissionId] = @PermissionId
      AND [RevokedAt] IS NULL;

    IF @RolePermissionId IS NOT NULL
        RETURN;

    INSERT INTO [authorization].[RolePermission]
    (
        [RoleId],
        [PermissionId],
        [GrantedByUserId]
    )
    VALUES
    (
        @RoleId,
        @PermissionId,
        @GrantedByUserId
    );

    SET @RolePermissionId = SCOPE_IDENTITY();
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[RolePermission_Revoke]
    @RoleId             BIGINT,
    @PermissionId       BIGINT,
    @RevokedByUserId    BIGINT = NULL,
    @AffectedRows       INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @RoleId <= 0
        THROW 53372, 'Role id must be greater than zero.', 1;

    IF @PermissionId <= 0
        THROW 53373, 'Permission id must be greater than zero.', 1;

    UPDATE [authorization].[RolePermission]
    SET
        [RevokedAt] = SYSUTCDATETIME(),
        [RevokedByUserId] = @RevokedByUserId
    WHERE [RoleId] = @RoleId
      AND [PermissionId] = @PermissionId
      AND [RevokedAt] IS NULL;

    SET @AffectedRows = @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[RolePermission_SelectPermissionsByRoleId]
    @RoleId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        rp.[RolePermissionId],
        rp.[RoleId],
        rp.[PermissionId],
        rp.[GrantedAt],
        rp.[GrantedByUserId],
        rp.[RevokedAt],
        rp.[RevokedByUserId],
        p.[PublicId] AS [PermissionPublicId],
        p.[Name] AS [PermissionName],
        p.[NameNormalized] AS [PermissionNameNormalized],
        p.[Description] AS [PermissionDescription],
        p.[Module] AS [PermissionModule],
        p.[IsSystem] AS [PermissionIsSystem],
        p.[IsActive] AS [PermissionIsActive]
    FROM [authorization].[RolePermission] rp
    INNER JOIN [authorization].[Permission] p
        ON rp.[PermissionId] = p.[PermissionId]
    WHERE rp.[RoleId] = @RoleId
      AND rp.[RevokedAt] IS NULL
    ORDER BY rp.[GrantedAt] DESC, rp.[RolePermissionId] DESC;
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[RolePermission_SelectRolesByPermissionId]
    @PermissionId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        rp.[RolePermissionId],
        rp.[RoleId],
        rp.[PermissionId],
        rp.[GrantedAt],
        rp.[GrantedByUserId],
        rp.[RevokedAt],
        rp.[RevokedByUserId],
        r.[PublicId] AS [RolePublicId],
        r.[Name] AS [RoleName],
        r.[NameNormalized] AS [RoleNameNormalized],
        r.[Description] AS [RoleDescription],
        r.[IsSystem] AS [RoleIsSystem],
        r.[IsActive] AS [RoleIsActive]
    FROM [authorization].[RolePermission] rp
    INNER JOIN [authorization].[Role] r
        ON rp.[RoleId] = r.[RoleId]
    WHERE rp.[PermissionId] = @PermissionId
      AND rp.[RevokedAt] IS NULL
    ORDER BY rp.[GrantedAt] DESC, rp.[RolePermissionId] DESC;
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[RolePermission_SelectActiveByRoleIdAndPermissionId]
    @RoleId BIGINT,
    @PermissionId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        [RolePermissionId],
        [RoleId],
        [PermissionId],
        [GrantedAt],
        [GrantedByUserId],
        [RevokedAt],
        [RevokedByUserId]
    FROM [authorization].[RolePermission]
    WHERE [RoleId] = @RoleId
      AND [PermissionId] = @PermissionId
      AND [RevokedAt] IS NULL;
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[RolePermission_GetRecordCountByRoleId]
    @RoleId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT_BIG(1) AS [RecordCount]
    FROM [authorization].[RolePermission]
    WHERE [RoleId] = @RoleId;
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[RolePermission_GetRecordCountByPermissionId]
    @PermissionId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT_BIG(1) AS [RecordCount]
    FROM [authorization].[RolePermission]
    WHERE [PermissionId] = @PermissionId;
END;
GO

CREATE OR ALTER PROCEDURE [authorization].[Authorization_SelectEffectivePermissionsByUserId]
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT
        p.[PermissionId],
        p.[PublicId] AS [PermissionPublicId],
        p.[Name] AS [PermissionName],
        p.[NameNormalized] AS [PermissionNameNormalized],
        p.[Description] AS [PermissionDescription],
        p.[Module] AS [PermissionModule],
        p.[IsSystem] AS [PermissionIsSystem],
        p.[IsActive] AS [PermissionIsActive]
    FROM [authorization].[UserRole] ur
    INNER JOIN [authorization].[Role] r
        ON ur.[RoleId] = r.[RoleId]
    INNER JOIN [authorization].[RolePermission] rp
        ON r.[RoleId] = rp.[RoleId]
    INNER JOIN [authorization].[Permission] p
        ON rp.[PermissionId] = p.[PermissionId]
    WHERE ur.[UserId] = @UserId
      AND ur.[RevokedAt] IS NULL
      AND rp.[RevokedAt] IS NULL
      AND r.[IsActive] = 1
      AND p.[IsActive] = 1
    ORDER BY p.[NameNormalized] ASC;
END
GO