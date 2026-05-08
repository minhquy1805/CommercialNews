namespace Identity.Application.Contracts.Users.RevokeUserSessions;

public sealed class RevokeUserSessionsResponseDto
{
    public long UserId { get; init; }

    public string PublicId { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public int RevokedSessionCount { get; init; }

    public DateTime RevokedAtUtc { get; init; }
}