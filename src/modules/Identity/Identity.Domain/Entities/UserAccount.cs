using Identity.Domain.Enums;

namespace Identity.Domain.Entities
{
    public sealed class UserAccount
    {
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

        private UserAccount() 
        {
            PublicId = string.Empty;
            Email = string.Empty;
            EmailNormalized = string.Empty;
            PasswordHash = string.Empty;
        }

        public UserAccount(
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
            if (string.IsNullOrWhiteSpace(publicId))
                throw new ArgumentException("PublicId is required.", nameof(publicId));

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.", nameof(email));

            if (string.IsNullOrWhiteSpace(emailNormalized))
                throw new ArgumentException("EmailNormalized is required.", nameof(emailNormalized));

            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("PasswordHash is required.", nameof(passwordHash));

            if (version < 1)
                throw new ArgumentOutOfRangeException(nameof(version), "Version must be greater than or equal to 1.");

            if (emailVerifiedAt.HasValue && emailVerifiedAt.Value < createdAt)
                throw new ArgumentException("EmailVerifiedAt cannot be earlier than CreatedAt.", nameof(emailVerifiedAt));

            if (lockedUntil.HasValue && lockedUntil.Value < createdAt)
                throw new ArgumentException("LockedUntil cannot be earlier than CreatedAt.", nameof(lockedUntil));

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

        public void VerifyEmail(DateTime verifiedAtUtc)
        {
            if (IsEmailVerified)
                return;

            if (verifiedAtUtc < CreatedAt)
                throw new ArgumentException("Verification time cannot be earlier than CreatedAt.", nameof(verifiedAtUtc));

            IsEmailVerified = true;
            EmailVerifiedAt = verifiedAtUtc;
            Touch(verifiedAtUtc);
        }

        public void ChangePassword(string newPasswordHash, DateTime updatedAtUtc)
        {
            if (string.IsNullOrWhiteSpace(newPasswordHash))
                throw new ArgumentException("New password hash is required.", nameof(newPasswordHash));

            PasswordHash = newPasswordHash;
            Touch(updatedAtUtc);
        }

        public void UpdateProfile(string? fullName, string? avatarUrl, DateTime updatedAtUtc)
        {
            FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim();
            AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
            Touch(updatedAtUtc);
        }

        public void RecordLoginSuccess(DateTime loginAtUtc)
        {
            LastLoginAt = loginAtUtc;

            if (Status == UserAccountStatus.Locked && LockedUntil.HasValue && LockedUntil.Value <= loginAtUtc)
            {
                Status = UserAccountStatus.Active;
                LockedUntil = null;
            }

            Touch(loginAtUtc);
        }

        public void LockUntil(DateTime lockedUntilUtc, DateTime updatedAtUtc)
        {
            if (lockedUntilUtc < CreatedAt)
                throw new ArgumentException("LockedUntil cannot be earlier than CreatedAt.", nameof(lockedUntilUtc));

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
            Status = UserAccountStatus.Inactive;
            LockedUntil = null;
            Touch(updatedAtUtc);
        }

        public void Activate(DateTime updatedAtUtc)
        {
            Status = UserAccountStatus.Active;
            LockedUntil = null;
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
            if (updatedAtUtc < CreatedAt)
                throw new ArgumentException("UpdatedAt cannot be earlier than CreatedAt.", nameof(updatedAtUtc));

            UpdatedAt = updatedAtUtc;
            Version++;
        }
    }
}
