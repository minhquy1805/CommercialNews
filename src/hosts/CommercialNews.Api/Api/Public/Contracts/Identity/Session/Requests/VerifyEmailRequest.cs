namespace CommercialNews.Api.Api.Public.Identity.Contracts.Session.Requests
{
    public sealed class VerifyEmailRequest
    {
        public string Token { get; init; } = string.Empty;
    }
}