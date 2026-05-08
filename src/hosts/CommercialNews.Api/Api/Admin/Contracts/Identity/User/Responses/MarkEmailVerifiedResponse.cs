namespace CommercialNews.Api.Api.Admin.Contracts.Identity.User.Responses;

public sealed class MarkEmailVerifiedResponse
{
    public long UserId { get; init; }

    public string PublicId { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public bool IsEmailVerified { get; init; }

    public bool WasAlreadyVerified { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime MarkedVerifiedAtUtc { get; init; }
}
