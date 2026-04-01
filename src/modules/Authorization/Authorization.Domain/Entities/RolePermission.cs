using Authorization.Domain.Exceptions;

namespace Authorization.Domain.Entities
{
    public sealed class RolePermission
    {
        public long RolePermissionId { get; private set; }
        public long RoleId { get; private set; }
        public long PermissionId { get; private set; }

        public DateTime GrantedAt { get; private set; }
        public long? GrantedByUserId { get; private set; }

        public DateTime? RevokedAt { get; private set; }
        public long? RevokedByUserId { get; private set; }

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

            return new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                GrantedAt = grantedAt,
                GrantedByUserId = grantedByUserId,
                RevokedAt = null,
                RevokedByUserId = null
            };
        }

        public static RolePermission Rehydrate(
            long rolePermissionId,
            long roleId,
            long permissionId,
            DateTime grantedAt,
            long? grantedByUserId,
            DateTime? revokedAt,
            long? revokedByUserId)
        {
            if (rolePermissionId <= 0)
            {
                throw new AuthorizationDomainException(
                    "AUTHORIZATION.ROLE_PERMISSION_INVALID_ROLE_PERMISSION_ID",
                    "Role permission id must be greater than zero.");
            }

            ValidateRoleId(roleId);
            ValidatePermissionId(permissionId);
            ValidateRevocationState(grantedAt, revokedAt, revokedByUserId);

            return new RolePermission
            {
                RolePermissionId = rolePermissionId,
                RoleId = roleId,
                PermissionId = permissionId,
                GrantedAt = grantedAt,
                GrantedByUserId = grantedByUserId,
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
                    "AUTHORIZATION.ROLE_PERMISSION_ALREADY_REVOKED",
                    "Role permission grant is already revoked.");
            }

            if (revokedAt < GrantedAt)
            {
                throw new AuthorizationDomainException(
                    "AUTHORIZATION.ROLE_PERMISSION_INVALID_REVOKE_TIME",
                    "RevokedAt cannot be earlier than GrantedAt.");
            }

            RevokedAt = revokedAt;
            RevokedByUserId = revokedByUserId;
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

        private static void ValidateRevocationState(
            DateTime grantedAt,
            DateTime? revokedAt,
            long? revokedByUserId)
        {
            if (revokedAt.HasValue && revokedAt.Value < grantedAt)
            {
                throw new AuthorizationDomainException(
                    "AUTHORIZATION.ROLE_PERMISSION_INVALID_REVOKE_TIME",
                    "RevokedAt cannot be earlier than GrantedAt.");
            }

            if (!revokedAt.HasValue && revokedByUserId.HasValue)
            {
                throw new AuthorizationDomainException(
                    "AUTHORIZATION.ROLE_PERMISSION_INVALID_REVOKE_STATE",
                    "RevokedByUserId cannot be set when RevokedAt is null.");
            }
        }
    }
}