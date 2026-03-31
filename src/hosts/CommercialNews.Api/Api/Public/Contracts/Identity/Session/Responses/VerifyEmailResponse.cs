namespace CommercialNews.Api.Api.Public.Identity.Contracts.Session.Responses
{
    public sealed class VerifyEmailResponse
    {
        public long UserId { get; init; }
        public bool Verified { get; init; }
    }
}