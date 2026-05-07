using Identity.Domain.Enums;
using Identity.Domain.Exceptions;

namespace Identity.Domain.Entities;

public sealed class UserAccount
{
    private const int PublicIdLength = 26;
    private const int EmailMaxLength = 320;
    private const int PasswordHashMaxLength = 500;
    private const int FullNameMaxLength = 200;
    private const int AvatarUrlMaxLength = 800;

    private UserAccount(
        long userId,
        string publicId,
        string email,
        string emailNormalized,
        string passwordHash,
        string? fullName,
        string? avatarUrl,
        bool isEmailVerified,
        DateTime? emailVerifiedAt,
        string status,
        DateTime? lockedUntil,
        DateTime createdAt,
        DateTime updatedAt,
        DateTime? lastLoginAt,
        int version)
    {
        UserId = userId;
        PublicId = publicId;
        Email = email;
        EmailNormalized = emailNormalized;
        PasswordHash = passwordHash;
        FullName = fullName;
        AvatarUrl = avatarUrl;
        IsEmailVerified = isEmailVerified;
        EmailVerifiedAt = emailVerifiedAt;
        Status = status;
        LockedUntil = lockedUntil;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        LastLoginAt = lastLoginAt;
        Version = version;
    }

    public long UserId { get; private set; }
    public string PublicId { get; private set; }
    public string Email { get; private set; }
    public string EmailNormalized { get; private set; }
    public string PasswordHash { get; private set; }

    public string? FullName { get; private set; }
    public string? AvatarUrl { get; private set; }

    public bool IsEmailVerified { get; private set; }
    public DateTime? EmailVerifiedAt { get; private set; }

    public string Status { get; private set; }
    public DateTime? LockedUntil { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    public int Version { get; private set; }

    public bool IsUnverified => string.Equals(Status, UserAccountStatuses.Unverified, StringComparison.OrdinalIgnoreCase);
    public bool IsActive => string.Equals(Status, UserAccountStatuses.Active, StringComparison.OrdinalIgnoreCase);
    public bool IsLocked => string.Equals(Status, UserAccountStatuses.Locked, StringComparison.OrdinalIgnoreCase);
    public bool IsDisabled => string.Equals(Status, UserAccountStatuses.Disabled, StringComparison.OrdinalIgnoreCase);

    public static UserAccount Create(
        string publicId,
        string email,
        string emailNormalized,
        string passwordHash,
        string? fullName,
        string? avatarUrl,
        DateTime nowUtc)
    {
        ValidatePublicId(publicId);
        ValidateRequiredString(email, nameof(email), EmailMaxLength, "IDENTITY.USER_EMAIL_INVALID");
        ValidateRequiredString(emailNormalized, nameof(emailNormalized), EmailMaxLength, "IDENTITY.USER_EMAIL_NORMALIZED_INVALID");
        ValidateRequiredString(passwordHash, nameof(passwordHash), PasswordHashMaxLength, "IDENTITY.USER_PASSWORD_HASH_INVALID");
        ValidateOptionalString(fullName, nameof(fullName), FullNameMaxLength, "IDENTITY.USER_FULL_NAME_INVALID");
        ValidateOptionalString(avatarUrl, nameof(avatarUrl), AvatarUrlMaxLength, "IDENTITY.USER_AVATAR_URL_INVALID");

        return new UserAccount(
            userId: 0,
            publicId: publicId.Trim(),
            email: email.Trim(),
            emailNormalized: emailNormalized.Trim(),
            passwordHash: passwordHash.Trim(),
            fullName: NormalizeOptional(fullName),
            avatarUrl: NormalizeOptional(avatarUrl),
            isEmailVerified: false,
            emailVerifiedAt: null,
            status: UserAccountStatuses.Unverified,
            lockedUntil: null,
            createdAt: nowUtc,
            updatedAt: nowUtc,
            lastLoginAt: null,
            version: 1);
    }

    public static UserAccount Rehydrate(
        long userId,
        string publicId,
        string email,
        string emailNormalized,
        string passwordHash,
        string? fullName,
        string? avatarUrl,
        bool isEmailVerified,
        DateTime? emailVerifiedAt,
        string status,
        DateTime? lockedUntil,
        DateTime createdAt,
        DateTime updatedAt,
        DateTime? lastLoginAt,
        int version)
    {
        if (userId <= 0)
        {
            throw new IdentityDomainException("IDENTITY.USER_INVALID_USER_ID", "User id must be greater than zero.");
        }

        ValidatePublicId(publicId);
        ValidateRequiredString(email, nameof(email), EmailMaxLength, "IDENTITY.USER_EMAIL_INVALID");
        ValidateRequiredString(emailNormalized, nameof(emailNormalized), EmailMaxLength, "IDENTITY.USER_EMAIL_NORMALIZED_INVALID");
        ValidateRequiredString(passwordHash, nameof(passwordHash), PasswordHashMaxLength, "IDENTITY.USER_PASSWORD_HASH_INVALID");
        ValidateOptionalString(fullName, nameof(fullName), FullNameMaxLength, "IDENTITY.USER_FULL_NAME_INVALID");
        ValidateOptionalString(avatarUrl, nameof(avatarUrl), AvatarUrlMaxLength, "IDENTITY.USER_AVATAR_URL_INVALID");
        ValidateStatus(status);

        if (version < 1)
        {
            throw new IdentityDomainException("IDENTITY.USER_INVALID_VERSION", "Version must be greater than or equal to 1.");
        }

        if (updatedAt < createdAt)
        {
            throw new IdentityDomainException("IDENTITY.USER_INVALID_UPDATED_AT", "UpdatedAt cannot be earlier than CreatedAt.");
        }

        if (emailVerifiedAt.HasValue && emailVerifiedAt.Value < createdAt)
        {
            throw new IdentityDomainException("IDENTITY.USER_INVALID_EMAIL_VERIFIED_AT", "EmailVerifiedAt cannot be earlier than CreatedAt.");
        }

        if (lockedUntil.HasValue && lockedUntil.Value < createdAt)
        {
            throw new IdentityDomainException("IDENTITY.USER_INVALID_LOCKED_UNTIL", "LockedUntil cannot be earlier than CreatedAt.");
        }

        if (lastLoginAt.HasValue && lastLoginAt.Value < createdAt)
        {
            throw new IdentityDomainException("IDENTITY.USER_INVALID_LAST_LOGIN_AT", "LastLoginAt cannot be earlier than CreatedAt.");
        }

        if (isEmailVerified && !string.Equals(status, UserAccountStatuses.Active, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(status, UserAccountStatuses.Locked, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(status, UserAccountStatuses.Disabled, StringComparison.OrdinalIgnoreCase))
        {
            throw new IdentityDomainException("IDENTITY.USER_INVALID_STATUS", "Verified user cannot remain in unverified status.");
        }

        return new UserAccount(
            userId,
            publicId.Trim(),
            email.Trim(),
            emailNormalized.Trim(),
            passwordHash.Trim(),
            NormalizeOptional(fullName),
            NormalizeOptional(avatarUrl),
            isEmailVerified,
            emailVerifiedAt,
            status.Trim(),
            lockedUntil,
            createdAt,
            updatedAt,
            lastLoginAt,
            version);
    }

    public void VerifyEmail(DateTime verifiedAtUtc)
    {
        if (IsEmailVerified)
        {
            return;
        }

        EnsureValidTimestamp(verifiedAtUtc, "IDENTITY.USER_INVALID_EMAIL_VERIFIED_AT");

        IsEmailVerified = true;
        EmailVerifiedAt = verifiedAtUtc;

        if (IsUnverified)
        {
            Status = UserAccountStatuses.Active;
        }

        Touch(verifiedAtUtc);
    }

    public void ChangePasswordHash(string newPasswordHash, DateTime updatedAtUtc)
    {
        ValidateRequiredString(newPasswordHash, nameof(newPasswordHash), PasswordHashMaxLength, "IDENTITY.USER_PASSWORD_HASH_INVALID");

        PasswordHash = newPasswordHash.Trim();
        Touch(updatedAtUtc);
    }

    public void UpdateProfile(string? fullName, string? avatarUrl, DateTime updatedAtUtc)
    {
        ValidateOptionalString(fullName, nameof(fullName), FullNameMaxLength, "IDENTITY.USER_FULL_NAME_INVALID");
        ValidateOptionalString(avatarUrl, nameof(avatarUrl), AvatarUrlMaxLength, "IDENTITY.USER_AVATAR_URL_INVALID");

        FullName = NormalizeOptional(fullName);
        AvatarUrl = NormalizeOptional(avatarUrl);
        Touch(updatedAtUtc);
    }

    public void RecordLoginSuccess(DateTime loginAtUtc)
    {
        EnsureValidTimestamp(loginAtUtc, "IDENTITY.USER_INVALID_LAST_LOGIN_AT");
        LastLoginAt = loginAtUtc;
        Touch(loginAtUtc);
    }

    public void LockUntil(DateTime lockedUntilUtc, DateTime updatedAtUtc)
    {
        EnsureNotDisabled();
        EnsureValidTimestamp(updatedAtUtc, "IDENTITY.USER_INVALID_UPDATED_AT");

        if (lockedUntilUtc <= updatedAtUtc)
        {
            throw new IdentityDomainException(
                "IDENTITY.USER_INVALID_LOCKED_UNTIL",
                "LockedUntil must be later than the update timestamp.");
        }

        if (IsLocked && LockedUntil == lockedUntilUtc)
        {
            return;
        }

        Status = UserAccountStatuses.Locked;
        LockedUntil = lockedUntilUtc;
        Touch(updatedAtUtc);
    }

    public void Unlock(DateTime updatedAtUtc)
    {
        EnsureValidTimestamp(updatedAtUtc, "IDENTITY.USER_INVALID_UPDATED_AT");

        if (!IsLocked && LockedUntil is null)
        {
            return;
        }

        LockedUntil = null;
        Status = IsEmailVerified
            ? UserAccountStatuses.Active
            : UserAccountStatuses.Unverified;

        Touch(updatedAtUtc);
    }

    public void Disable(DateTime updatedAtUtc)
    {
        EnsureValidTimestamp(updatedAtUtc, "IDENTITY.USER_INVALID_UPDATED_AT");

        if (IsDisabled && LockedUntil is null)
        {
            return;
        }

        LockedUntil = null;
        Status = UserAccountStatuses.Disabled;
        Touch(updatedAtUtc);
    }

    public void Activate(DateTime updatedAtUtc)
    {
        EnsureValidTimestamp(updatedAtUtc, "IDENTITY.USER_INVALID_UPDATED_AT");

        if (!IsEmailVerified)
        {
            throw new IdentityDomainException(
                "IDENTITY.USER_CANNOT_ACTIVATE_UNVERIFIED",
                "Unverified user cannot be activated.");
        }

        if (IsActive && LockedUntil is null)
        {
            return;
        }

        LockedUntil = null;
        Status = UserAccountStatuses.Active;
        Touch(updatedAtUtc);
    }

    public void MarkEmailVerified(DateTime verifiedAtUtc)
    {
        if (IsEmailVerified)
        {
            return;
        }

        EnsureValidTimestamp(verifiedAtUtc, "IDENTITY.USER_INVALID_EMAIL_VERIFIED_AT");

        IsEmailVerified = true;
        EmailVerifiedAt = verifiedAtUtc;

        if (IsUnverified)
        {
            Status = UserAccountStatuses.Active;
        }

        Touch(verifiedAtUtc);
    }

    public bool IsLockedAt(DateTime nowUtc)
    {
        return IsLocked && LockedUntil.HasValue && LockedUntil.Value > nowUtc;
    }

    public static UserAccount CreateBootstrapAdmin(
        string publicId,
        string email,
        string emailNormalized,
        string passwordHash,
        string? fullName,
        string? avatarUrl,
        DateTime nowUtc)
    {
        ValidatePublicId(publicId);
        ValidateRequiredString(email, nameof(email), EmailMaxLength, "IDENTITY.USER_EMAIL_INVALID");
        ValidateRequiredString(emailNormalized, nameof(emailNormalized), EmailMaxLength, "IDENTITY.USER_EMAIL_NORMALIZED_INVALID");
        ValidateRequiredString(passwordHash, nameof(passwordHash), PasswordHashMaxLength, "IDENTITY.USER_PASSWORD_HASH_INVALID");
        ValidateOptionalString(fullName, nameof(fullName), FullNameMaxLength, "IDENTITY.USER_FULL_NAME_INVALID");
        ValidateOptionalString(avatarUrl, nameof(avatarUrl), AvatarUrlMaxLength, "IDENTITY.USER_AVATAR_URL_INVALID");

        return new UserAccount(
            userId: 0,
            publicId: publicId.Trim(),
            email: email.Trim(),
            emailNormalized: emailNormalized.Trim(),
            passwordHash: passwordHash.Trim(),
            fullName: NormalizeOptional(fullName),
            avatarUrl: NormalizeOptional(avatarUrl),
            isEmailVerified: true,
            emailVerifiedAt: nowUtc,
            status: UserAccountStatuses.Active,
            lockedUntil: null,
            createdAt: nowUtc,
            updatedAt: nowUtc,
            lastLoginAt: null,
            version: 1);
    }

    private void Touch(DateTime updatedAtUtc)
    {
        EnsureValidTimestamp(updatedAtUtc, "IDENTITY.USER_INVALID_UPDATED_AT");
        UpdatedAt = updatedAtUtc;
        Version++;
    }

    private void EnsureValidTimestamp(DateTime value, string code)
    {
        if (value < CreatedAt)
        {
            throw new IdentityDomainException(code, "The provided timestamp cannot be earlier than CreatedAt.");
        }
    }

    private void EnsureNotDisabled()
    {
        if (IsDisabled)
        {
            throw new IdentityDomainException("IDENTITY.USER_DISABLED", "Disabled user cannot perform this state transition.");
        }
    }

    private static void ValidateStatus(string status)
    {
        if (!UserAccountStatuses.IsValid(status))
        {
            throw new IdentityDomainException("IDENTITY.USER_INVALID_STATUS", "User status is invalid.");
        }
    }

    private static void ValidatePublicId(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId) || publicId.Trim().Length != PublicIdLength)
        {
            throw new IdentityDomainException("IDENTITY.USER_PUBLIC_ID_INVALID", $"PublicId must be exactly {PublicIdLength} characters.");
        }
    }

    private static void ValidateRequiredString(string value, string name, int maxLength, string code)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
        {
            throw new IdentityDomainException(code, $"{name} is invalid.");
        }
    }

    private static void ValidateOptionalString(string? value, string name, int maxLength, string code)
    {
        if (value is not null && value.Trim().Length > maxLength)
        {
            throw new IdentityDomainException(code, $"{name} is invalid.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}