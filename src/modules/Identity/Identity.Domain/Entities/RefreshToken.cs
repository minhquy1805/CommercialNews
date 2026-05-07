using Identity.Domain.Exceptions;

namespace Identity.Domain.Entities;

public sealed class RefreshToken
{
    private const int TokenHashLength = 32;
    private const int RevokedReasonMaxLength = 200;
    private const int CreatedIpMaxLength = 45;
    private const int UserAgentMaxLength = 300;
    private const int CorrelationIdMaxLength = 100;

    private byte[] _tokenHash = Array.Empty<byte>();
    private byte[]? _replacedByTokenHash;

    private RefreshToken(
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
        RefreshTokenId = refreshTokenId;
        UserId = userId;
        _tokenHash = tokenHash.ToArray();
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        RevokedAt = revokedAt;
        RevokedReason = revokedReason;
        _replacedByTokenHash = replacedByTokenHash?.ToArray();
        CreatedIp = createdIp;
        UserAgent = userAgent;
        CorrelationId = correlationId;
    }

    public long RefreshTokenId { get; private set; }
    public long UserId { get; private set; }
    public byte[] TokenHash => _tokenHash.ToArray();
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }
    public byte[]? ReplacedByTokenHash => _replacedByTokenHash?.ToArray();
    public string? CreatedIp { get; private set; }
    public string? UserAgent { get; private set; }
    public string? CorrelationId { get; private set; }

    public bool IsRevoked => RevokedAt.HasValue;
    public bool HasBeenReplaced => _replacedByTokenHash is not null;

    public static RefreshToken Create(
        long userId,
        byte[] tokenHash,
        DateTime createdAt,
        DateTime expiresAt,
        string? createdIp,
        string? userAgent,
        string? correlationId)
    {
        if (userId <= 0)
        {
            throw new IdentityDomainException(
                "IDENTITY.REFRESH_INVALID_USER_ID",
                "User id must be greater than zero.");
        }

        ValidateTokenHash(tokenHash);
        ValidateCreatedIp(createdIp);
        ValidateUserAgent(userAgent);
        ValidateCorrelationId(correlationId);

        if (expiresAt <= createdAt)
        {
            throw new IdentityDomainException(
                "IDENTITY.REFRESH_INVALID_EXPIRES_AT",
                "ExpiresAt must be greater than CreatedAt.");
        }

        return new RefreshToken(
            refreshTokenId: 0,
            userId: userId,
            tokenHash: tokenHash,
            createdAt: createdAt,
            expiresAt: expiresAt,
            revokedAt: null,
            revokedReason: null,
            replacedByTokenHash: null,
            createdIp: NormalizeOptional(createdIp),
            userAgent: NormalizeOptional(userAgent),
            correlationId: NormalizeOptional(correlationId));
    }

    public static RefreshToken Rehydrate(
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
        if (refreshTokenId <= 0)
        {
            throw new IdentityDomainException(
                "IDENTITY.REFRESH_INVALID_TOKEN_ID",
                "Refresh token id must be greater than zero.");
        }

        if (userId <= 0)
        {
            throw new IdentityDomainException(
                "IDENTITY.REFRESH_INVALID_USER_ID",
                "User id must be greater than zero.");
        }

        ValidateTokenHash(tokenHash);
        ValidateCreatedIp(createdIp);
        ValidateUserAgent(userAgent);
        ValidateCorrelationId(correlationId);
        ValidateRevokedReason(revokedReason);

        if (expiresAt <= createdAt)
        {
            throw new IdentityDomainException(
                "IDENTITY.REFRESH_INVALID_EXPIRES_AT",
                "ExpiresAt must be greater than CreatedAt.");
        }

        if (revokedAt.HasValue && revokedAt.Value < createdAt)
        {
            throw new IdentityDomainException(
                "IDENTITY.REFRESH_INVALID_REVOKED_AT",
                "RevokedAt cannot be earlier than CreatedAt.");
        }

        if (replacedByTokenHash is not null && replacedByTokenHash.Length != TokenHashLength)
        {
            throw new IdentityDomainException(
                "IDENTITY.REFRESH_REPLACED_BY_TOKEN_HASH_INVALID",
                $"ReplacedByTokenHash must be exactly {TokenHashLength} bytes when provided.");
        }

        if (replacedByTokenHash is not null && !revokedAt.HasValue)
        {
            throw new IdentityDomainException(
                "IDENTITY.REFRESH_INVALID_REPLACEMENT_STATE",
                "A replaced refresh token must also be revoked.");
        }

        return new RefreshToken(
            refreshTokenId: refreshTokenId,
            userId: userId,
            tokenHash: tokenHash,
            createdAt: createdAt,
            expiresAt: expiresAt,
            revokedAt: revokedAt,
            revokedReason: NormalizeOptional(revokedReason),
            replacedByTokenHash: replacedByTokenHash,
            createdIp: NormalizeOptional(createdIp),
            userAgent: NormalizeOptional(userAgent),
            correlationId: NormalizeOptional(correlationId));
    }

    public bool IsExpired(DateTime nowUtc)
    {
        EnsureValidTimestamp(nowUtc, "IDENTITY.REFRESH_INVALID_NOW");
        return nowUtc >= ExpiresAt;
    }

    public bool IsActiveAt(DateTime nowUtc)
    {
        EnsureValidTimestamp(nowUtc, "IDENTITY.REFRESH_INVALID_NOW");

        return !IsRevoked
            && !HasBeenReplaced
            && nowUtc < ExpiresAt;
    }

    public bool CanBeUsed(DateTime nowUtc) => IsActiveAt(nowUtc);

    public void Revoke(
        DateTime revokedAtUtc,
        string? revokedReason = null,
        byte[]? replacedByTokenHash = null)
    {
        if (IsRevoked)
        {
            return;
        }

        if (revokedAtUtc < CreatedAt)
        {
            throw new IdentityDomainException(
                "IDENTITY.REFRESH_INVALID_REVOKED_AT",
                "RevokedAt cannot be earlier than CreatedAt.");
        }

        ValidateRevokedReason(revokedReason);

        if (replacedByTokenHash is not null && replacedByTokenHash.Length != TokenHashLength)
        {
            throw new IdentityDomainException(
                "IDENTITY.REFRESH_REPLACED_BY_TOKEN_HASH_INVALID",
                $"ReplacedByTokenHash must be exactly {TokenHashLength} bytes when provided.");
        }

        if (replacedByTokenHash is not null && replacedByTokenHash.SequenceEqual(_tokenHash))
        {
            throw new IdentityDomainException(
                "IDENTITY.REFRESH_REPLACEMENT_CANNOT_BE_SELF",
                "A refresh token cannot be replaced by itself.");
        }

        RevokedAt = revokedAtUtc;
        RevokedReason = NormalizeOptional(revokedReason);
        _replacedByTokenHash = replacedByTokenHash?.ToArray();
    }

    public void ReplaceBy(
        DateTime revokedAtUtc,
        byte[] replacementTokenHash,
        string? revokedReason = null)
    {
        ValidateTokenHash(replacementTokenHash);

        if (replacementTokenHash.SequenceEqual(_tokenHash))
        {
            throw new IdentityDomainException(
                "IDENTITY.REFRESH_REPLACEMENT_CANNOT_BE_SELF",
                "A refresh token cannot be replaced by itself.");
        }

        Revoke(revokedAtUtc, revokedReason, replacementTokenHash);
    }

    private static void ValidateTokenHash(byte[] tokenHash)
    {
        if (tokenHash is null || tokenHash.Length == 0)
        {
            throw new IdentityDomainException(
                "IDENTITY.REFRESH_TOKEN_HASH_REQUIRED",
                "Refresh token hash is required.");
        }

        if (tokenHash.Length != TokenHashLength)
        {
            throw new IdentityDomainException(
                "IDENTITY.REFRESH_TOKEN_HASH_INVALID",
                $"Refresh token hash must be exactly {TokenHashLength} bytes.");
        }
    }

    private static void ValidateRevokedReason(string? revokedReason)
    {
        if (revokedReason is not null && revokedReason.Trim().Length > RevokedReasonMaxLength)
        {
            throw new IdentityDomainException(
                "IDENTITY.REFRESH_REVOKED_REASON_TOO_LONG",
                $"RevokedReason must not exceed {RevokedReasonMaxLength} characters.");
        }
    }

    private static void ValidateCreatedIp(string? createdIp)
    {
        if (createdIp is not null && createdIp.Trim().Length > CreatedIpMaxLength)
        {
            throw new IdentityDomainException(
                "IDENTITY.REFRESH_CREATED_IP_TOO_LONG",
                $"CreatedIp must not exceed {CreatedIpMaxLength} characters.");
        }
    }

    private static void ValidateUserAgent(string? userAgent)
    {
        if (userAgent is not null && userAgent.Trim().Length > UserAgentMaxLength)
        {
            throw new IdentityDomainException(
                "IDENTITY.REFRESH_USER_AGENT_TOO_LONG",
                $"UserAgent must not exceed {UserAgentMaxLength} characters.");
        }
    }

    private static void ValidateCorrelationId(string? correlationId)
    {
        if (correlationId is not null && correlationId.Trim().Length > CorrelationIdMaxLength)
        {
            throw new IdentityDomainException(
                "IDENTITY.REFRESH_CORRELATION_ID_TOO_LONG",
                $"CorrelationId must not exceed {CorrelationIdMaxLength} characters.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static void EnsureValidTimestamp(DateTime value, string code)
    {
        if (value == default)
        {
            throw new IdentityDomainException(code, "Timestamp is required.");
        }
    }
}