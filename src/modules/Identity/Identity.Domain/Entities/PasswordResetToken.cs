using Identity.Domain.Exceptions;

namespace Identity.Domain.Entities;

public sealed class PasswordResetToken
{
    private const int TokenHashLength = 32;
    private const int CreatedIpMaxLength = 45;
    private const int CorrelationIdMaxLength = 100;

    private byte[] _tokenHash = Array.Empty<byte>();

    private PasswordResetToken(
        long resetTokenId,
        long userId,
        byte[] tokenHash,
        DateTime createdAt,
        DateTime expiresAt,
        DateTime? usedAt,
        DateTime? revokedAt,
        string? createdIp,
        string? correlationId)
    {
        ResetTokenId = resetTokenId;
        UserId = userId;
        _tokenHash = tokenHash.ToArray();
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        UsedAt = usedAt;
        RevokedAt = revokedAt;
        CreatedIp = createdIp;
        CorrelationId = correlationId;
    }

    public long ResetTokenId { get; private set; }
    public long UserId { get; private set; }
    public byte[] TokenHash => _tokenHash.ToArray();
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? CreatedIp { get; private set; }
    public string? CorrelationId { get; private set; }

    public bool IsUsed => UsedAt.HasValue;
    public bool IsRevoked => RevokedAt.HasValue;

    public static PasswordResetToken Create(
        long userId,
        byte[] tokenHash,
        DateTime createdAt,
        DateTime expiresAt,
        string? createdIp,
        string? correlationId)
    {
        if (userId <= 0)
        {
            throw new IdentityDomainException(
                "IDENTITY.PASSWORD_RESET_INVALID_USER_ID",
                "User id must be greater than zero.");
        }

        ValidateTokenHash(tokenHash);
        ValidateCreatedIp(createdIp);
        ValidateCorrelationId(correlationId);

        if (expiresAt <= createdAt)
        {
            throw new IdentityDomainException(
                "IDENTITY.PASSWORD_RESET_INVALID_EXPIRES_AT",
                "ExpiresAt must be greater than CreatedAt.");
        }

        return new PasswordResetToken(
            resetTokenId: 0,
            userId: userId,
            tokenHash: tokenHash,
            createdAt: createdAt,
            expiresAt: expiresAt,
            usedAt: null,
            revokedAt: null,
            createdIp: NormalizeOptional(createdIp),
            correlationId: NormalizeOptional(correlationId));
    }

    public static PasswordResetToken Rehydrate(
        long resetTokenId,
        long userId,
        byte[] tokenHash,
        DateTime createdAt,
        DateTime expiresAt,
        DateTime? usedAt,
        DateTime? revokedAt,
        string? createdIp,
        string? correlationId)
    {
        if (resetTokenId <= 0)
        {
            throw new IdentityDomainException(
                "IDENTITY.PASSWORD_RESET_INVALID_TOKEN_ID",
                "Reset token id must be greater than zero.");
        }

        if (userId <= 0)
        {
            throw new IdentityDomainException(
                "IDENTITY.PASSWORD_RESET_INVALID_USER_ID",
                "User id must be greater than zero.");
        }

        ValidateTokenHash(tokenHash);
        ValidateCreatedIp(createdIp);
        ValidateCorrelationId(correlationId);

        if (expiresAt <= createdAt)
        {
            throw new IdentityDomainException(
                "IDENTITY.PASSWORD_RESET_INVALID_EXPIRES_AT",
                "ExpiresAt must be greater than CreatedAt.");
        }

        if (usedAt.HasValue && usedAt.Value < createdAt)
        {
            throw new IdentityDomainException(
                "IDENTITY.PASSWORD_RESET_INVALID_USED_AT",
                "UsedAt cannot be earlier than CreatedAt.");
        }

        if (usedAt.HasValue && usedAt.Value >= expiresAt)
        {
            throw new IdentityDomainException(
                "IDENTITY.PASSWORD_RESET_INVALID_USED_AT",
                "UsedAt must be earlier than ExpiresAt.");
        }

        if (revokedAt.HasValue && revokedAt.Value < createdAt)
        {
            throw new IdentityDomainException(
                "IDENTITY.PASSWORD_RESET_INVALID_REVOKED_AT",
                "RevokedAt cannot be earlier than CreatedAt.");
        }

        return new PasswordResetToken(
            resetTokenId: resetTokenId,
            userId: userId,
            tokenHash: tokenHash,
            createdAt: createdAt,
            expiresAt: expiresAt,
            usedAt: usedAt,
            revokedAt: revokedAt,
            createdIp: NormalizeOptional(createdIp),
            correlationId: NormalizeOptional(correlationId));
    }

    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAt;

    public bool CanBeUsed(DateTime nowUtc) => !IsUsed && !IsRevoked && !IsExpired(nowUtc);

    public void MarkUsed(DateTime usedAtUtc)
    {
        if (IsUsed)
        {
            throw new IdentityDomainException(
                "IDENTITY.PASSWORD_RESET_TOKEN_ALREADY_USED",
                "Password reset token has already been used.");
        }

        if (IsRevoked)
        {
            throw new IdentityDomainException(
                "IDENTITY.PASSWORD_RESET_TOKEN_REVOKED",
                "Password reset token has been revoked.");
        }

        if (usedAtUtc < CreatedAt)
        {
            throw new IdentityDomainException(
                "IDENTITY.PASSWORD_RESET_INVALID_USED_AT",
                "UsedAt cannot be earlier than CreatedAt.");
        }

        if (usedAtUtc >= ExpiresAt)
        {
            throw new IdentityDomainException(
                "IDENTITY.PASSWORD_RESET_TOKEN_EXPIRED",
                "Password reset token has expired.");
        }

        UsedAt = usedAtUtc;
    }

    public void Revoke(DateTime revokedAtUtc)
    {
        if (IsRevoked)
        {
            return;
        }

        if (revokedAtUtc < CreatedAt)
        {
            throw new IdentityDomainException(
                "IDENTITY.PASSWORD_RESET_INVALID_REVOKED_AT",
                "RevokedAt cannot be earlier than CreatedAt.");
        }

        RevokedAt = revokedAtUtc;
    }

    private static void ValidateTokenHash(byte[] tokenHash)
    {
        if (tokenHash is null || tokenHash.Length == 0)
        {
            throw new IdentityDomainException(
                "IDENTITY.PASSWORD_RESET_TOKEN_HASH_REQUIRED",
                "Password reset token hash is required.");
        }

        if (tokenHash.Length != TokenHashLength)
        {
            throw new IdentityDomainException(
                "IDENTITY.PASSWORD_RESET_TOKEN_HASH_INVALID",
                $"Password reset token hash must be exactly {TokenHashLength} bytes.");
        }
    }

    private static void ValidateCreatedIp(string? createdIp)
    {
        if (createdIp is not null && createdIp.Trim().Length > CreatedIpMaxLength)
        {
            throw new IdentityDomainException(
                "IDENTITY.PASSWORD_RESET_CREATED_IP_TOO_LONG",
                $"CreatedIp must not exceed {CreatedIpMaxLength} characters.");
        }
    }

    private static void ValidateCorrelationId(string? correlationId)
    {
        if (correlationId is not null && correlationId.Trim().Length > CorrelationIdMaxLength)
        {
            throw new IdentityDomainException(
                "IDENTITY.PASSWORD_RESET_CORRELATION_ID_TOO_LONG",
                $"CorrelationId must not exceed {CorrelationIdMaxLength} characters.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}