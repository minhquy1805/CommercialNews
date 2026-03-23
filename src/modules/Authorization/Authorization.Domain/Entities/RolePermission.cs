namespace Authorization.Domain.Entities;

public sealed class RolePermission
{
    public long RolePermissionId { get; private set; }
    public long RoleId { get; private set; }
    public long PermissionId { get; private set; }

    public DateTime GrantedAt { get; private set; }
    public long? GrantedByUserId { get; private set; }

    public DateTime? RevokedAt { get; private set; }
    public long? RevokedByUserId { get; private set; }

    public RolePermission(
        long rolePermissionId,
        long roleId,
        long permissionId,
        DateTime grantedAt,
        long? grantedByUserId,
        DateTime? revokedAt,
        long? revokedByUserId)
    {
        if (rolePermissionId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rolePermissionId), "RolePermissionId cannot be negative.");
        }

        if (roleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(roleId), "RoleId must be greater than zero.");
        }

        if (permissionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(permissionId), "PermissionId must be greater than zero.");
        }

        if (revokedAt.HasValue && revokedAt.Value < grantedAt)
        {
            throw new ArgumentException("RevokedAt cannot be earlier than GrantedAt.");
        }

        if (!revokedAt.HasValue && revokedByUserId.HasValue)
        {
            throw new ArgumentException("RevokedByUserId cannot be set when RevokedAt is null.");
        }

        RolePermissionId = rolePermissionId;
        RoleId = roleId;
        PermissionId = permissionId;

        GrantedAt = grantedAt;
        GrantedByUserId = grantedByUserId;

        RevokedAt = revokedAt;
        RevokedByUserId = revokedByUserId;
    }

    public bool IsActiveAt(DateTime pointInTimeUtc)
    {
        return !RevokedAt.HasValue || RevokedAt.Value > pointInTimeUtc;
    }

    public bool IsCurrentlyActive()
    {
        return RevokedAt is null;
    }

    public void Revoke(
        DateTime revokedAt,
        long? revokedByUserId)
    {
        if (RevokedAt.HasValue)
        {
            throw new InvalidOperationException("Role permission grant is already revoked.");
        }

        if (revokedAt < GrantedAt)
        {
            throw new ArgumentException("RevokedAt cannot be earlier than GrantedAt.", nameof(revokedAt));
        }

        RevokedAt = revokedAt;
        RevokedByUserId = revokedByUserId;
    }
}