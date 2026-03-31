namespace CommercialNews.Api.Api.Public.Identity.Contracts.Session.Requests
{
    public sealed class RefreshTokenRequest
    {
        public string RefreshToken { get; init; } = string.Empty;
    }
}