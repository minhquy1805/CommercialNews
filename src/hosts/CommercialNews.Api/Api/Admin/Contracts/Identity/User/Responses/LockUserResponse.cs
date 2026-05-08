namespace CommercialNews.Api.Api.Admin.Contracts.Identity.User.Responses;

public sealed class LockUserResponse
{
    public long UserId { get; init; }

    public string PublicId { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime LockedUntilUtc { get; init; }

    public bool Locked { get; init; }

    public bool SessionsRevoked { get; init; }

    public int RevokedSessionCount { get; init; }

    public DateTime LockedAtUtc { get; init; }
}
