namespace CommercialNews.Api.Api.Admin.Contracts.Identity.User.Requests;

public sealed class DisableUserRequest
{
    public string? Reason { get; init; }

    public bool RevokeSessions { get; init; } = true;
}
