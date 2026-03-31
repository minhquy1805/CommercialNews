namespace CommercialNews.Api.Api.Public.Identity.Contracts.Recovery.Requests
{
    public sealed class ResetPasswordRequest
    {
        public string Token { get; init; } = string.Empty;
        public string NewPassword { get; init; } = string.Empty;
    }
}