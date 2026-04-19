using Authorization.Domain.Exceptions;

namespace Authorization.Domain.Entities;

public sealed class RolePermission
{
    public long RoleId { get; private set; }
    public long PermissionId { get; private set; }

    public DateTime GrantedAt { get; private set; }
    public long? GrantedByUserId { get; private set; }

    private RolePermission()
    {
    }

    public static RolePermission CreateNew(
        long roleId,
        long permissionId,
        DateTime grantedAt,
        long? grantedByUserId)
    {
        ValidateRoleId(roleId);
        ValidatePermissionId(permissionId);
        ValidateGrantedAt(grantedAt);

        return new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId,
            GrantedAt = grantedAt,
            GrantedByUserId = grantedByUserId
        };
    }

    public static RolePermission Rehydrate(
        long roleId,
        long permissionId,
        DateTime grantedAt,
        long? grantedByUserId)
    {
        ValidateRoleId(roleId);
        ValidatePermissionId(permissionId);
        ValidateGrantedAt(grantedAt);

        return new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId,
            GrantedAt = grantedAt,
            GrantedByUserId = grantedByUserId
        };
    }

    private static void ValidateRoleId(long roleId)
    {
        if (roleId <= 0)
        {
            throw new AuthorizationDomainException(
                "AUTHORIZATION.ROLE_PERMISSION_INVALID_ROLE_ID",
                "Role id must be greater than zero.");
        }
    }

    private static void ValidatePermissionId(long permissionId)
    {
        if (permissionId <= 0)
        {
            throw new AuthorizationDomainException(
                "AUTHORIZATION.ROLE_PERMISSION_INVALID_PERMISSION_ID",
                "Permission id must be greater than zero.");
        }
    }

    private static void ValidateGrantedAt(DateTime grantedAt)
    {
        if (grantedAt == default)
        {
            throw new AuthorizationDomainException(
                "AUTHORIZATION.ROLE_PERMISSION_INVALID_GRANT_TIME",
                "GrantedAt is required.");
        }
    }
}