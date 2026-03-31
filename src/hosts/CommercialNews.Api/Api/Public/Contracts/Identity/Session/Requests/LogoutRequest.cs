namespace CommercialNews.Api.Api.Public.Identity.Contracts.Session.Requests
{
    public sealed class LogoutRequest
    {
        public string RefreshToken { get; init; } = string.Empty;
    }
}