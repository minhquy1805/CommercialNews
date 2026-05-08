namespace Identity.Application.Contracts.Users.GetUserSessions;

public sealed class UserSessionItemDto
{
    public long RefreshTokenId { get; init; }

    public long UserId { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime ExpiresAt { get; init; }

    public DateTime? RevokedAt { get; init; }

    public string? RevokedReason { get; init; }

    public string? CreatedIp { get; init; }

    public string? UserAgent { get; init; }

    public string? CorrelationId { get; init; }

    public bool IsRevoked { get; init; }

    public bool IsExpired { get; init; }

    public bool IsActive { get; init; }
}