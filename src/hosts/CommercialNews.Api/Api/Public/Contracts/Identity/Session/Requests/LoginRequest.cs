namespace CommercialNews.Api.Api.Public.Identity.Contracts.Session.Requests
{
    public sealed class LoginRequest
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }
}