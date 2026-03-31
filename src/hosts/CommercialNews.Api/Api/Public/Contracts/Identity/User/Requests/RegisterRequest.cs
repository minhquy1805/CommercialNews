namespace CommercialNews.Api.Api.Public.Identity.Contracts.User.Requests
{
    public sealed class RegisterRequest
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string? FullName { get; init; }
    }
}