namespace CommercialNews.Api.Api.Public.Identity.Contracts.Session.Responses
{
    public sealed class RefreshTokenResponse
    {
        public long UserId { get; init; }
        public string AccessToken { get; init; } = string.Empty;
        public DateTime AccessTokenExpiresAtUtc { get; init; }
    }
}