namespace CommercialNews.Api.Api.Admin.Contracts.Identity.User.Requests;

public sealed class RevokeUserSessionsRequest
{
    public string? Reason { get; init; }
}
