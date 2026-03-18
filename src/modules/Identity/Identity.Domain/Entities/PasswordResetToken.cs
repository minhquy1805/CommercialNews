namespace Identity.Domain.Entities
{
    public sealed class PasswordResetToken
    {
        public long ResetTokenId { get; private set; }
        public long UserId { get; private set; }
        public byte[] TokenHash { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public DateTime? UsedAt { get; private set; }
        public DateTime? RevokedAt { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public string? CreatedIp { get; private set; }
        public string? CorrelationId { get; private set; }

        private PasswordResetToken()
        {
            TokenHash = Array.Empty<byte>();
        }

        public PasswordResetToken(
           long resetTokenId,
           long userId,
           byte[] tokenHash,
           DateTime expiresAt,
           DateTime? usedAt,
           DateTime? revokedAt,
           DateTime createdAt,
           string? createdIp,
           string? correlationId)
        {
            if (tokenHash is null || tokenHash.Length == 0)
                throw new ArgumentException("TokenHash is required.", nameof(tokenHash));

            if (expiresAt <= createdAt)
                throw new ArgumentException("ExpiresAt must be greater than CreatedAt.", nameof(expiresAt));

            if (usedAt.HasValue && usedAt.Value < createdAt)
                throw new ArgumentException("UsedAt cannot be earlier than CreatedAt.", nameof(usedAt));

            if (revokedAt.HasValue && revokedAt.Value < createdAt)
                throw new ArgumentException("RevokedAt cannot be earlier than CreatedAt.", nameof(revokedAt));

            ResetTokenId = resetTokenId;
            UserId = userId;
            TokenHash = tokenHash;
            ExpiresAt = expiresAt;
            UsedAt = usedAt;
            RevokedAt = revokedAt;
            CreatedAt = createdAt;
            CreatedIp = createdIp;
            CorrelationId = correlationId;
        }

        public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAt;

        public bool IsUsed() => UsedAt.HasValue;

        public bool IsRevoked() => RevokedAt.HasValue;

        public bool CanBeUsed(DateTime nowUtc) => !IsUsed() && !IsRevoked() && !IsExpired(nowUtc);

        public void MarkUsed(DateTime usedAtUtc)
        {
            if (UsedAt.HasValue)
                throw new InvalidOperationException("Password reset token has already been used.");

            if (RevokedAt.HasValue)
                throw new InvalidOperationException("Password reset token has been revoked.");

            if (usedAtUtc < CreatedAt)
                throw new ArgumentException("UsedAt cannot be earlier than CreatedAt.", nameof(usedAtUtc));

            UsedAt = usedAtUtc;
        }

        public void Revoke(DateTime revokedAtUtc)
        {
            if (RevokedAt.HasValue)
                return;

            if (revokedAtUtc < CreatedAt)
                throw new ArgumentException("RevokedAt cannot be earlier than CreatedAt.", nameof(revokedAtUtc));

            RevokedAt = revokedAtUtc;
        }
    } 
}
