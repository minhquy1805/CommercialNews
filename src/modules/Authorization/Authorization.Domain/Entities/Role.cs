using Authorization.Domain.Exceptions;

namespace Authorization.Domain.Entities;

public sealed class Role
{
    public long RoleId { get; private set; }
    public string PublicId { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;
    public string NameNormalized { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }
    public string? Description { get; private set; }

    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public long? CreatedByUserId { get; private set; }
    public long? UpdatedByUserId { get; private set; }

    private Role()
    {
    }

    public static Role CreateNew(
        string publicId,
        string name,
        string nameNormalized,
        string? displayName,
        string? description,
        bool isSystem,
        DateTime nowUtc,
        long? actorUserId)
    {
        ValidatePublicId(publicId);
        ValidateName(name);
        ValidateNameNormalized(nameNormalized);
        EnsureValidCreateTime(nowUtc);

        return new Role
        {
            PublicId = publicId.Trim(),
            Name = name.Trim(),
            NameNormalized = nameNormalized.Trim(),
            DisplayName = NormalizeOptional(displayName),
            Description = NormalizeOptional(description),
            IsSystem = isSystem,
            IsActive = true,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc,
            CreatedByUserId = actorUserId,
            UpdatedByUserId = actorUserId
        };
    }

    public static Role Rehydrate(
        long roleId,
        string publicId,
        string name,
        string nameNormalized,
        string? displayName,
        string? description,
        bool isSystem,
        bool isActive,
        DateTime createdAt,
        DateTime updatedAt,
        long? createdByUserId,
        long? updatedByUserId)
    {
        if (roleId <= 0)
        {
            throw new AuthorizationDomainException(
                "AUTHORIZATION.ROLE_INVALID_ROLE_ID",
                "Role id must be greater than zero.");
        }

        ValidatePublicId(publicId);
        ValidateName(name);
        ValidateNameNormalized(nameNormalized);

        if (updatedAt < createdAt)
        {
            throw new AuthorizationDomainException(
                "AUTHORIZATION.ROLE_INVALID_TIMESTAMP",
                "UpdatedAt cannot be earlier than CreatedAt.");
        }

        return new Role
        {
            RoleId = roleId,
            PublicId = publicId.Trim(),
            Name = name.Trim(),
            NameNormalized = nameNormalized.Trim(),
            DisplayName = NormalizeOptional(displayName),
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
        string name,
        string nameNormalized,
        string? displayName,
        string? description,
        DateTime nowUtc,
        long? actorUserId)
    {
        ValidateName(name);
        ValidateNameNormalized(nameNormalized);
        EnsureValidUpdateTime(nowUtc);

        var trimmedName = name.Trim();
        var trimmedNormalized = nameNormalized.Trim();

        if (IsSystem &&
            (!string.Equals(Name, trimmedName, StringComparison.Ordinal) ||
             !string.Equals(NameNormalized, trimmedNormalized, StringComparison.Ordinal)))
        {
            throw new AuthorizationDomainException(
                "AUTHORIZATION.SYSTEM_ROLE_PROTECTED",
                "System role name cannot be changed.");
        }

        Name = trimmedName;
        NameNormalized = trimmedNormalized;
        DisplayName = NormalizeOptional(displayName);
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
                "AUTHORIZATION.SYSTEM_ROLE_PROTECTED",
                "System role cannot be deactivated.");
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
                "AUTHORIZATION.ROLE_INVALID_TIMESTAMP",
                "Creation time is required.");
        }
    }

    private void EnsureValidUpdateTime(DateTime nowUtc)
    {
        if (nowUtc < CreatedAt)
        {
            throw new AuthorizationDomainException(
                "AUTHORIZATION.ROLE_INVALID_TIMESTAMP",
                "UpdatedAt cannot be earlier than CreatedAt.");
        }

        if (nowUtc < UpdatedAt)
        {
            throw new AuthorizationDomainException(
                "AUTHORIZATION.ROLE_STALE_UPDATE_TIME",
                "UpdatedAt cannot be earlier than the current UpdatedAt.");
        }
    }

    private static void ValidatePublicId(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            throw new AuthorizationDomainException(
                "AUTHORIZATION.ROLE_PUBLIC_ID_REQUIRED",
                "Role public id is required.");
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new AuthorizationDomainException(
                "AUTHORIZATION.ROLE_NAME_REQUIRED",
                "Role name is required.");
        }

        if (name.Trim().Length > 80)
        {
            throw new AuthorizationDomainException(
                "AUTHORIZATION.ROLE_NAME_TOO_LONG",
                "Role name must not exceed 80 characters.");
        }
    }

    private static void ValidateNameNormalized(string nameNormalized)
    {
        if (string.IsNullOrWhiteSpace(nameNormalized))
        {
            throw new AuthorizationDomainException(
                "AUTHORIZATION.ROLE_NAME_NORMALIZED_REQUIRED",
                "Normalized role name is required.");
        }

        if (nameNormalized.Trim().Length > 80)
        {
            throw new AuthorizationDomainException(
                "AUTHORIZATION.ROLE_NAME_NORMALIZED_TOO_LONG",
                "Normalized role name must not exceed 80 characters.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}