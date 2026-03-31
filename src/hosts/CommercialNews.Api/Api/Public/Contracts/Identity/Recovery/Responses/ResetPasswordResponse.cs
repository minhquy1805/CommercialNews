namespace CommercialNews.Api.Api.Public.Identity.Contracts.Recovery.Responses
{
    public sealed class ResetPasswordResponse
    {
        public long UserId { get; init; }
        public bool PasswordReset { get; init; }
    }
}