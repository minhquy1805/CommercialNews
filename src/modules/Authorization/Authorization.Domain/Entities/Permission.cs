using Authorization.Domain.Exceptions;

namespace Authorization.Domain.Entities;

public sealed class Permission
{
    public long PermissionId { get; private set; }
    public string PublicId { get; private set; } = string.Empty;

    public string Key { get; private set; } = string.Empty;
    public string KeyNormalized { get; private set; } = string.Empty;
    public string? Module { get; private set; }
    public string? Action { get; private set; }
    public string? Description { get; private set; }

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
        string key,
        string keyNormalized,
        string? module,
        string? action,
        string? description,
        bool isSystem,
        DateTime nowUtc,
        long? actorUserId)
    {
        ValidatePublicId(publicId);
        ValidateKey(key);
        ValidateKeyNormalized(keyNormalized);
        ValidateModule(module);
        ValidateAction(action);
        EnsureValidCreateTime(nowUtc);

        return new Permission
        {
            PublicId = publicId.Trim(),
            Key = key.Trim(),
            KeyNormalized = keyNormalized.Trim(),
            Module = NormalizeOptional(module),
            Action = NormalizeOptional(action),
            Description = NormalizeOptional(description),
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
        string key,
        string keyNormalized,
        string? module,
        string? action,
        string? description,
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
        ValidateKey(key);
        ValidateKeyNormalized(keyNormalized);
        ValidateModule(module);
        ValidateAction(action);

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
            Key = key.Trim(),
            KeyNormalized = keyNormalized.Trim(),
            Module = NormalizeOptional(module),
            Action = NormalizeOptional(action),
            Description = NormalizeOptional(description),
            IsSystem = isSystem,
            IsActive = isActive,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            CreatedByUserId = createdByUserId,
            UpdatedByUserId = updatedByUserId
        };
    }

    public void UpdateMetadata(
        string key,
        string keyNormalized,
        string? module,
        string? action,
        string? description,
        DateTime nowUtc,
        long? actorUserId)
    {
        ValidateKey(key);
        ValidateKeyNormalized(keyNormalized);
        ValidateModule(module);
        ValidateAction(action);
        EnsureValidUpdateTime(nowUtc);

        var trimmedKey = key.Trim();
        var trimmedKeyNormalized = keyNormalized.Trim();

        if (IsSystem &&
            (!string.Equals(Key, trimmedKey, StringComparison.Ordinal) ||
             !string.Equals(KeyNormalized, trimmedKeyNormalized, StringComparison.Ordinal)))
        {
            throw new AuthorizationDomainException(
                "AUTHORIZATION.SYSTEM_PERMISSION_PROTECTED",
                "System permission key cannot be changed.");
        }

        Key = trimmedKey;
        KeyNormalized = trimmedKeyNormalized;
        Module = NormalizeOptional(module);
        Action = NormalizeOptional(action);
        Description = NormalizeOptional(description);
        UpdatedAt = nowUtc;
        UpdatedByUserId = actorUserId;
    }

    public void Activate(DateTime nowUtc, long? actorUserId)
    {
        if (IsActive)
        {
            return;
        }

        EnsureValidUpdateTime(nowUtc);

        IsActive = true;
        UpdatedAt = nowUtc;
        UpdatedByUserId = actorUserId;
    }

    public void Deactivate(DateTime nowUtc, long? actorUserId)
    {
        if (!IsActive)
        {
            return;
        }

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

    private static void EnsureValidCreateTime(DateTime nowUtc)
    {
        if (nowUtc == default)
        {
            throw new AuthorizationDomainException(
                "AUTHORIZATION.PERMISSION_INVALID_TIMESTAMP",
                "Creation time is required.");
        }
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

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new AuthorizationDomainException(
                "AUTHORIZATION.PERMISSION_NAME_REQUIRED",
                "Permission key is required.");
        }

        if (key.Trim().Length > 120)
        {
            throw new AuthorizationDomainException(
                "AUTHORIZATION.PERMISSION_NAME_TOO_LONG",
                "Permission key must not exceed 120 characters.");
        }
    }

    private static void ValidateKeyNormalized(string keyNormalized)
    {
        if (string.IsNullOrWhiteSpace(keyNormalized))
        {
            throw new AuthorizationDomainException(
                "AUTHORIZATION.PERMISSION_NAME_NORMALIZED_REQUIRED",
                "Normalized permission key is required.");
        }

        if (keyNormalized.Trim().Length > 120)
        {
            throw new AuthorizationDomainException(
                "AUTHORIZATION.PERMISSION_NAME_NORMALIZED_TOO_LONG",
                "Normalized permission key must not exceed 120 characters.");
        }
    }

    private static void ValidateModule(string? module)
    {
        if (module is null)
        {
            return;
        }

        if (module.Trim().Length > 50)
        {
            throw new AuthorizationDomainException(
                "AUTHORIZATION.PERMISSION_MODULE_TOO_LONG",
                "Permission module must not exceed 50 characters.");
        }
    }

    private static void ValidateAction(string? action)
    {
        if (action is null)
        {
            return;
        }

        if (action.Trim().Length > 50)
        {
            throw new AuthorizationDomainException(
                "AUTHORIZATION.PERMISSION_ACTION_TOO_LONG",
                "Permission action must not exceed 50 characters.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}