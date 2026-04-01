using Authorization.Domain.Exceptions;

namespace Authorization.Domain.Entities
{
    public sealed class UserRole
    {
        public long UserRoleId { get; private set; }
        public long UserId { get; private set; }
        public long RoleId { get; private set; }

        public DateTime AssignedAt { get; private set; }
        public long? AssignedByUserId { get; private set; }

        public DateTime? RevokedAt { get; private set; }
        public long? RevokedByUserId { get; private set; }

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

            return new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedAt = assignedAt,
                AssignedByUserId = assignedByUserId,
                RevokedAt = null,
                RevokedByUserId = null
            };
        }

        public static UserRole Rehydrate(
            long userRoleId,
            long userId,
            long roleId,
            DateTime assignedAt,
            long? assignedByUserId,
            DateTime? revokedAt,
            long? revokedByUserId)
        {
            if (userRoleId <= 0)
            {
                throw new AuthorizationDomainException(
                    "AUTHORIZATION.USER_ROLE_INVALID_USER_ROLE_ID",
                    "User role id must be greater than zero.");
            }

            ValidateUserId(userId);
            ValidateRoleId(roleId);
            ValidateRevocationState(assignedAt, revokedAt, revokedByUserId);

            return new UserRole
            {
                UserRoleId = userRoleId,
                UserId = userId,
                RoleId = roleId,
                AssignedAt = assignedAt,
                AssignedByUserId = assignedByUserId,
                RevokedAt = revokedAt,
                RevokedByUserId = revokedByUserId
            };
        }

        public bool IsActiveAt(DateTime pointInTimeUtc)
        {
            return RevokedAt is null || RevokedAt > pointInTimeUtc;
        }

        public bool IsCurrentlyActive()
        {
            return RevokedAt is null;
        }

        public void Revoke(DateTime revokedAt, long? revokedByUserId)
        {
            if (RevokedAt.HasValue)
            {
                throw new AuthorizationDomainException(
                    "AUTHORIZATION.USER_ROLE_ALREADY_REVOKED",
                    "User role assignment is already revoked.");
            }

            if (revokedAt < AssignedAt)
            {
                throw new AuthorizationDomainException(
                    "AUTHORIZATION.USER_ROLE_INVALID_REVOKE_TIME",
                    "RevokedAt cannot be earlier than AssignedAt.");
            }

            RevokedAt = revokedAt;
            RevokedByUserId = revokedByUserId;
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

        private static void ValidateRevocationState(
            DateTime assignedAt,
            DateTime? revokedAt,
            long? revokedByUserId)
        {
            if (revokedAt.HasValue && revokedAt.Value < assignedAt)
            {
                throw new AuthorizationDomainException(
                    "AUTHORIZATION.USER_ROLE_INVALID_REVOKE_TIME",
                    "RevokedAt cannot be earlier than AssignedAt.");
            }

            if (!revokedAt.HasValue && revokedByUserId.HasValue)
            {
                throw new AuthorizationDomainException(
                    "AUTHORIZATION.USER_ROLE_INVALID_REVOKE_STATE",
                    "RevokedByUserId cannot be set when RevokedAt is null.");
            }
        }
    }
}