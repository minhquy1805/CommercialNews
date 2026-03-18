namespace Identity.Domain.Entities
{
    public sealed class EmailVerificationToken
    {
        public long VerificationTokenId { get; private set; }
        public long UserId { get; private set; }
        public byte[] TokenHash { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public DateTime? UsedAt { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public string? CreatedIp { get; private set; }
        public string? CorrelationId { get; private set; }

        private EmailVerificationToken()
        {
            TokenHash = Array.Empty<byte>();
        }

        public EmailVerificationToken(
            long verificationTokenId,
            long userId,
            byte[] tokenHash,
            DateTime expiresAt,
            DateTime? usedAt,
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

            VerificationTokenId = verificationTokenId;
            UserId = userId;
            TokenHash = tokenHash;
            ExpiresAt = expiresAt;
            UsedAt = usedAt;
            CreatedAt = createdAt;
            CreatedIp = createdIp;
            CorrelationId = correlationId;
        }

        public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAt;

        public bool IsUsed() => UsedAt.HasValue;

        public bool CanBeUsed(DateTime nowUtc) => !IsUsed() && !IsExpired(nowUtc);

        public void MarkUsed(DateTime usedAtUtc)
        {
            if (UsedAt.HasValue)
                throw new InvalidOperationException("Verification token has already been used.");

            if (usedAtUtc < CreatedAt)
                throw new ArgumentException("UsedAt cannot be earlier than CreatedAt.", nameof(usedAtUtc));

            UsedAt = usedAtUtc;
        }
    }
}
