using Authorization.Domain.Exceptions;

namespace Authorization.Domain.Entities;

public sealed class UserRole
{
    public long UserId { get; private set; }
    public long RoleId { get; private set; }

    public DateTime AssignedAt { get; private set; }
    public long? AssignedByUserId { get; private set; }

    private UserRole()
    {
    }

    public static UserRole CreateNew(
        long userId,
        long roleId,
        DateTime assignedAt,
        long? assignedByUserId)
    {
        ValidateUserId(userId);
        ValidateRoleId(roleId);
        ValidateAssignedAt(assignedAt);

        return new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            AssignedAt = assignedAt,
            AssignedByUserId = assignedByUserId
        };
    }

    public static UserRole Rehydrate(
        long userId,
        long roleId,
        DateTime assignedAt,
        long? assignedByUserId)
    {
        ValidateUserId(userId);
        ValidateRoleId(roleId);
        ValidateAssignedAt(assignedAt);

        return new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            AssignedAt = assignedAt,
            AssignedByUserId = assignedByUserId
        };
    }

    private static void ValidateUserId(long userId)
    {
        if (userId <= 0)
        {
            throw new AuthorizationDomainException(
                "AUTHORIZATION.USER_ROLE_INVALID_USER_ID",
                "User id must be greater than zero.");
        }
    }

    private static void ValidateRoleId(long roleId)
    {
        if (roleId <= 0)
        {
            throw new AuthorizationDomainException(
                "AUTHORIZATION.USER_ROLE_INVALID_ROLE_ID",
                "Role id must be greater than zero.");
        }
    }

    private static void ValidateAssignedAt(DateTime assignedAt)
    {
        if (assignedAt == default)
        {
            throw new AuthorizationDomainException(
                "AUTHORIZATION.USER_ROLE_INVALID_ASSIGN_TIME",
                "AssignedAt is required.");
        }
    }
}