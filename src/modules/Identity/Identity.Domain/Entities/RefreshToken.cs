namespace Identity.Domain.Entities
{
    public sealed class RefreshToken
    {
        public long RefreshTokenId { get; private set; }
        public long UserId { get; private set; }
        public byte[] TokenHash { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public DateTime? RevokedAt { get; private set; }
        public string? RevokedReason { get; private set; }
        public byte[]? ReplacedByTokenHash { get; private set; }
        public string? CreatedIp { get; private set; }
        public string? UserAgent { get; private set; }
        public string? CorrelationId { get; private set; }

        private RefreshToken()
        {
            TokenHash = Array.Empty<byte>();
        }

        public RefreshToken(
        long refreshTokenId,
        long userId,
        byte[] tokenHash,
        DateTime createdAt,
        DateTime expiresAt,
        DateTime? revokedAt,
        string? revokedReason,
        byte[]? replacedByTokenHash,
        string? createdIp,
        string? userAgent,
        string? correlationId)
        {
            if (tokenHash is null || tokenHash.Length == 0)
                throw new ArgumentException("TokenHash is required.", nameof(tokenHash));

            if (expiresAt <= createdAt)
                throw new ArgumentException("ExpiresAt must be greater than CreatedAt.", nameof(expiresAt));

            if (revokedAt.HasValue && revokedAt.Value < createdAt)
                throw new ArgumentException("RevokedAt cannot be earlier than CreatedAt.", nameof(revokedAt));

            RefreshTokenId = refreshTokenId;
            UserId = userId;
            TokenHash = tokenHash;
            CreatedAt = createdAt;
            ExpiresAt = expiresAt;
            RevokedAt = revokedAt;
            RevokedReason = revokedReason;
            ReplacedByTokenHash = replacedByTokenHash;
            CreatedIp = createdIp;
            UserAgent = userAgent;
            CorrelationId = correlationId;
        }

        public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAt;

        public bool IsRevoked() => RevokedAt.HasValue;

        public bool CanBeUsed(DateTime nowUtc) => !IsRevoked() && !IsExpired(nowUtc);

        public void Revoke(DateTime revokedAtUtc, string? revokedReason = null, byte[]? replacedByTokenHash = null)
        {
            if (RevokedAt.HasValue)
                return;

            if (revokedAtUtc < CreatedAt)
                throw new ArgumentException("RevokedAt cannot be earlier than CreatedAt.", nameof(revokedAtUtc));

            RevokedAt = revokedAtUtc;
            RevokedReason = string.IsNullOrWhiteSpace(revokedReason) ? null : revokedReason.Trim();
            ReplacedByTokenHash = replacedByTokenHash;
        }
    }
}
