namespace CommercialNews.Api.Api.Public.Identity.Contracts.Recovery.Requests
{
    public sealed class ResendVerificationEmailRequest
    {
        public string Email { get; init; } = string.Empty;
    }
}