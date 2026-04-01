using Authorization.Domain.Exceptions;

namespace Authorization.Domain.Entities
{
    public sealed class Permission
    {
        public long PermissionId { get; private set; }
        public string PublicId { get; private set; } = string.Empty;

        public string Name { get; private set; } = string.Empty;
        public string NameNormalized { get; private set; } = string.Empty;
        public string? Description { get; private set; }
        public string? Module { get; private set; }

        public bool IsSystem { get; private set; }
        public bool IsActive { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public long? CreatedByUserId { get; private set; }
        public long? UpdatedByUserId { get; private set; }

        private Permission()
        {
        }

        public static Permission CreateNew(
            string publicId,
            string name,
            string nameNormalized,
            string? description,
            string? module,
            bool isSystem,
            DateTime nowUtc,
            long? actorUserId)
        {
            ValidatePublicId(publicId);
            ValidateName(name);
            ValidateNameNormalized(nameNormalized);
            ValidateModule(module);

            return new Permission
            {
                PublicId = publicId.Trim(),
                Name = name.Trim(),
                NameNormalized = nameNormalized.Trim(),
                Description = NormalizeOptional(description),
                Module = NormalizeOptional(module),
                IsSystem = isSystem,
                IsActive = true,
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc,
                CreatedByUserId = actorUserId,
                UpdatedByUserId = actorUserId
            };
        }

        public static Permission Rehydrate(
            long permissionId,
            string publicId,
            string name,
            string nameNormalized,
            string? description,
            string? module,
            bool isSystem,
            bool isActive,
            DateTime createdAt,
            DateTime updatedAt,
            long? createdByUserId,
            long? updatedByUserId)
        {
            if (permissionId <= 0)
            {
                throw new AuthorizationDomainException(
                    "AUTHORIZATION.PERMISSION_INVALID_PERMISSION_ID",
                    "Permission id must be greater than zero.");
            }

            ValidatePublicId(publicId);
            ValidateName(name);
            ValidateNameNormalized(nameNormalized);
            ValidateModule(module);

            if (updatedAt < createdAt)
            {
                throw new AuthorizationDomainException(
                    "AUTHORIZATION.PERMISSION_INVALID_TIMESTAMP",
                    "UpdatedAt cannot be earlier than CreatedAt.");
            }

            return new Permission
            {
                PermissionId = permissionId,
                PublicId = publicId.Trim(),
                Name = name.Trim(),
                NameNormalized = nameNormalized.Trim(),
                Description = NormalizeOptional(description),
                Module = NormalizeOptional(module),
                IsSystem = isSystem,
                IsActive = isActive,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt,
                CreatedByUserId = createdByUserId,
                UpdatedByUserId = updatedByUserId
            };
        }

        public void UpdateMetadata(
            string name,
            string nameNormalized,
            string? description,
            string? module,
            DateTime nowUtc,
            long? actorUserId)
        {
            ValidateName(name);
            ValidateNameNormalized(nameNormalized);
            ValidateModule(module);
            EnsureValidUpdateTime(nowUtc);

            Name = name.Trim();
            NameNormalized = nameNormalized.Trim();
            Description = NormalizeOptional(description);
            Module = NormalizeOptional(module);
            UpdatedAt = nowUtc;
            UpdatedByUserId = actorUserId;
        }

        public void Activate(DateTime nowUtc, long? actorUserId)
        {
            EnsureValidUpdateTime(nowUtc);

            IsActive = true;
            UpdatedAt = nowUtc;
            UpdatedByUserId = actorUserId;
        }

        public void Deactivate(DateTime nowUtc, long? actorUserId)
        {
            if (IsSystem)
            {
                throw new AuthorizationDomainException(
                    "AUTHORIZATION.SYSTEM_PERMISSION_PROTECTED",
                    "System permission cannot be deactivated.");
            }

            EnsureValidUpdateTime(nowUtc);

            IsActive = false;
            UpdatedAt = nowUtc;
            UpdatedByUserId = actorUserId;
        }

        private void EnsureValidUpdateTime(DateTime nowUtc)
        {
            if (nowUtc < CreatedAt)
            {
                throw new AuthorizationDomainException(
                    "AUTHORIZATION.PERMISSION_INVALID_TIMESTAMP",
                    "UpdatedAt cannot be earlier than CreatedAt.");
            }

            if (nowUtc < UpdatedAt)
            {
                throw new AuthorizationDomainException(
                    "AUTHORIZATION.PERMISSION_STALE_UPDATE_TIME",
                    "UpdatedAt cannot be earlier than the current UpdatedAt.");
            }
        }

        private static void ValidatePublicId(string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                throw new AuthorizationDomainException(
                    "AUTHORIZATION.PERMISSION_PUBLIC_ID_REQUIRED",
                    "Permission public id is required.");
            }
        }

        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new AuthorizationDomainException(
                    "AUTHORIZATION.PERMISSION_NAME_REQUIRED",
                    "Permission name is required.");
            }

            if (name.Trim().Length > 150)
            {
                throw new AuthorizationDomainException(
                    "AUTHORIZATION.PERMISSION_NAME_TOO_LONG",
                    "Permission name must not exceed 150 characters.");
            }
        }

        private static void ValidateNameNormalized(string nameNormalized)
        {
            if (string.IsNullOrWhiteSpace(nameNormalized))
            {
                throw new AuthorizationDomainException(
                    "AUTHORIZATION.PERMISSION_NAME_NORMALIZED_REQUIRED",
                    "Normalized permission name is required.");
            }

            if (nameNormalized.Trim().Length > 150)
            {
                throw new AuthorizationDomainException(
                    "AUTHORIZATION.PERMISSION_NAME_NORMALIZED_TOO_LONG",
                    "Normalized permission name must not exceed 150 characters.");
            }
        }

        private static void ValidateModule(string? module)
        {
            if (module is null)
            {
                return;
            }

            if (module.Trim().Length > 100)
            {
                throw new AuthorizationDomainException(
                    "AUTHORIZATION.PERMISSION_MODULE_TOO_LONG",
                    "Permission module must not exceed 100 characters.");
            }
        }

        private static string? NormalizeOptional(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }
    }
}