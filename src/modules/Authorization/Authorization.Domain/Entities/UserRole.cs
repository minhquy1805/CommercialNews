namespace Authorization.Domain.Entities;

public sealed class UserRole
{
    public long UserRoleId { get; private set; }
    public long UserId { get; private set; }
    public long RoleId { get; private set; }

    public DateTime AssignedAt { get; private set; }
    public long? AssignedByUserId { get; private set; }

    public DateTime? RevokedAt { get; private set; }
    public long? RevokedByUserId { get; private set; }

    public UserRole(
        long userRoleId,
        long userId,
        long roleId,
        DateTime assignedAt,
        long? assignedByUserId,
        DateTime? revokedAt,
        long? revokedByUserId)
    {
        if (userRoleId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userRoleId), "UserRoleId cannot be negative.");
        }

        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "UserId must be greater than zero.");
        }

        if (roleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(roleId), "RoleId must be greater than zero.");
        }

        if (revokedAt.HasValue && revokedAt.Value < assignedAt)
        {
            throw new ArgumentException("RevokedAt cannot be earlier than AssignedAt.");
        }

        if (!revokedAt.HasValue && revokedByUserId.HasValue)
        {
            throw new ArgumentException("RevokedByUserId cannot be set when RevokedAt is null.");
        }

        UserRoleId = userRoleId;
        UserId = userId;
        RoleId = roleId;

        AssignedAt = assignedAt;
        AssignedByUserId = assignedByUserId;

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
            throw new InvalidOperationException("User role assignment is already revoked.");
        }

        if (revokedAt < AssignedAt)
        {
            throw new ArgumentException("RevokedAt cannot be earlier than AssignedAt.", nameof(revokedAt));
        }

        RevokedAt = revokedAt;
        RevokedByUserId = revokedByUserId;
    }
}