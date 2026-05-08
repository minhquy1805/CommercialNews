namespace CommercialNews.Api.Api.Admin.Contracts.Identity.User.Requests;

public sealed class LockUserRequest
{
    public DateTime LockedUntilUtc { get; init; }

    public string? Reason { get; init; }

    public bool RevokeSessions { get; init; } = true;
}
