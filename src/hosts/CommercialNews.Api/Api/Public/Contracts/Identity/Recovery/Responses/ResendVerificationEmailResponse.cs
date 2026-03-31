namespace CommercialNews.Api.Api.Public.Identity.Contracts.Recovery.Responses
{
    public sealed class ResendVerificationEmailResponse
    {
        public bool Requested { get; init; }
        public string Message { get; init; } = string.Empty;
    }
}