using Identity.Domain.Enums;
using Identity.Domain.Exceptions;

namespace Identity.Domain.Entities
{
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
            UserAccountStatus status,
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

        public UserAccountStatus Status { get; private set; }
        public DateTime? LockedUntil { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        public DateTime? LastLoginAt { get; private set; }

        public int Version { get; private set; }

        public bool IsActive => Status == UserAccountStatus.Active;
        public bool IsInactive => Status == UserAccountStatus.Inactive;
        public bool IsLocked => Status == UserAccountStatus.Locked;

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
            ValidateEmail(email, nameof(email), "IDENTITY.USER_EMAIL_REQUIRED");
            ValidateEmail(emailNormalized, nameof(emailNormalized), "IDENTITY.USER_EMAIL_NORMALIZED_REQUIRED");
            ValidatePasswordHash(passwordHash);
            ValidateFullName(fullName);
            ValidateAvatarUrl(avatarUrl);

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
                status: UserAccountStatus.Active,
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
            UserAccountStatus status,
            DateTime? lockedUntil,
            DateTime createdAt,
            DateTime updatedAt,
            DateTime? lastLoginAt,
            int version)
        {
            if (userId <= 0)
            {
                throw new IdentityDomainException(
                    "IDENTITY.USER_INVALID_USER_ID",
                    "User id must be greater than zero.");
            }

            ValidatePublicId(publicId);
            ValidateEmail(email, nameof(email), "IDENTITY.USER_EMAIL_REQUIRED");
            ValidateEmail(emailNormalized, nameof(emailNormalized), "IDENTITY.USER_EMAIL_NORMALIZED_REQUIRED");
            ValidatePasswordHash(passwordHash);
            ValidateFullName(fullName);
            ValidateAvatarUrl(avatarUrl);

            if (version < 1)
            {
                throw new IdentityDomainException(
                    "IDENTITY.USER_INVALID_VERSION",
                    "Version must be greater than or equal to 1.");
            }

            if (updatedAt < createdAt)
            {
                throw new IdentityDomainException(
                    "IDENTITY.USER_INVALID_UPDATED_AT",
                    "UpdatedAt cannot be earlier than CreatedAt.");
            }

            if (emailVerifiedAt.HasValue && emailVerifiedAt.Value < createdAt)
            {
                throw new IdentityDomainException(
                    "IDENTITY.USER_INVALID_EMAIL_VERIFIED_AT",
                    "EmailVerifiedAt cannot be earlier than CreatedAt.");
            }

            if (lockedUntil.HasValue && lockedUntil.Value < createdAt)
            {
                throw new IdentityDomainException(
                    "IDENTITY.USER_INVALID_LOCKED_UNTIL",
                    "LockedUntil cannot be earlier than CreatedAt.");
            }

            if (lastLoginAt.HasValue && lastLoginAt.Value < createdAt)
            {
                throw new IdentityDomainException(
                    "IDENTITY.USER_INVALID_LAST_LOGIN_AT",
                    "LastLoginAt cannot be earlier than CreatedAt.");
            }

            return new UserAccount(
                userId: userId,
                publicId: publicId.Trim(),
                email: email.Trim(),
                emailNormalized: emailNormalized.Trim(),
                passwordHash: passwordHash.Trim(),
                fullName: NormalizeOptional(fullName),
                avatarUrl: NormalizeOptional(avatarUrl),
                isEmailVerified: isEmailVerified,
                emailVerifiedAt: emailVerifiedAt,
                status: status,
                lockedUntil: lockedUntil,
                createdAt: createdAt,
                updatedAt: updatedAt,
                lastLoginAt: lastLoginAt,
                version: version);
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
            Touch(verifiedAtUtc);
        }

        public void ChangePassword(string newPasswordHash, DateTime updatedAtUtc)
        {
            ValidatePasswordHash(newPasswordHash);

            PasswordHash = newPasswordHash.Trim();
            Touch(updatedAtUtc);
        }

        public void UpdateProfile(string? fullName, string? avatarUrl, DateTime updatedAtUtc)
        {
            ValidateFullName(fullName);
            ValidateAvatarUrl(avatarUrl);

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
            EnsureValidTimestamp(updatedAtUtc, "IDENTITY.USER_INVALID_UPDATED_AT");

            if (lockedUntilUtc <= updatedAtUtc)
            {
                throw new IdentityDomainException(
                    "IDENTITY.USER_INVALID_LOCKED_UNTIL",
                    "LockedUntil must be later than the update timestamp.");
            }

            Status = UserAccountStatus.Locked;
            LockedUntil = lockedUntilUtc;
            Touch(updatedAtUtc);
        }

        public void Unlock(DateTime updatedAtUtc)
        {
            LockedUntil = null;
            Status = UserAccountStatus.Active;
            Touch(updatedAtUtc);
        }

        public void Deactivate(DateTime updatedAtUtc)
        {
            LockedUntil = null;
            Status = UserAccountStatus.Inactive;
            Touch(updatedAtUtc);
        }

        public void Activate(DateTime updatedAtUtc)
        {
            LockedUntil = null;
            Status = UserAccountStatus.Active;
            Touch(updatedAtUtc);
        }

        public bool IsLockedAt(DateTime nowUtc)
        {
            return Status == UserAccountStatus.Locked
                   && LockedUntil.HasValue
                   && LockedUntil.Value > nowUtc;
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
                throw new IdentityDomainException(
                    code,
                    "The provided timestamp cannot be earlier than CreatedAt.");
            }
        }

        private static void ValidatePublicId(string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                throw new IdentityDomainException(
                    "IDENTITY.USER_PUBLIC_ID_REQUIRED",
                    "PublicId is required.");
            }

            if (publicId.Trim().Length != PublicIdLength)
            {
                throw new IdentityDomainException(
                    "IDENTITY.USER_PUBLIC_ID_INVALID",
                    $"PublicId must be exactly {PublicIdLength} characters.");
            }
        }

        private static void ValidateEmail(string value, string paramName, string code)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new IdentityDomainException(
                    code,
                    $"{paramName} is required.");
            }

            if (value.Trim().Length > EmailMaxLength)
            {
                throw new IdentityDomainException(
                    paramName == nameof(EmailNormalized)
                        ? "IDENTITY.USER_EMAIL_NORMALIZED_TOO_LONG"
                        : "IDENTITY.USER_EMAIL_TOO_LONG",
                    $"{paramName} must not exceed {EmailMaxLength} characters.");
            }
        }

        private static void ValidatePasswordHash(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                throw new IdentityDomainException(
                    "IDENTITY.USER_PASSWORD_HASH_REQUIRED",
                    "PasswordHash is required.");
            }

            if (passwordHash.Trim().Length > PasswordHashMaxLength)
            {
                throw new IdentityDomainException(
                    "IDENTITY.USER_PASSWORD_HASH_TOO_LONG",
                    $"PasswordHash must not exceed {PasswordHashMaxLength} characters.");
            }
        }

        private static void ValidateFullName(string? fullName)
        {
            if (fullName is not null && fullName.Trim().Length > FullNameMaxLength)
            {
                throw new IdentityDomainException(
                    "IDENTITY.USER_FULL_NAME_TOO_LONG",
                    $"FullName must not exceed {FullNameMaxLength} characters.");
            }
        }

        private static void ValidateAvatarUrl(string? avatarUrl)
        {
            if (avatarUrl is not null && avatarUrl.Trim().Length > AvatarUrlMaxLength)
            {
                throw new IdentityDomainException(
                    "IDENTITY.USER_AVATAR_URL_TOO_LONG",
                    $"AvatarUrl must not exceed {AvatarUrlMaxLength} characters.");
            }
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}