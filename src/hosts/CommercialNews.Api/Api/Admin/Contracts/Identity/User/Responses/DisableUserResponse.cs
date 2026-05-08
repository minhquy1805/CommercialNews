namespace CommercialNews.Api.Api.Admin.Contracts.Identity.User.Responses;

public sealed class DisableUserResponse
{
    public long UserId { get; init; }

    public string PublicId { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public bool Disabled { get; init; }

    public bool SessionsRevoked { get; init; }

    public int RevokedSessionCount { get; init; }

    public DateTime DisabledAtUtc { get; init; }
}
