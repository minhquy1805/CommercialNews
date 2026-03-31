namespace CommercialNews.Api.Api.Public.Identity.Contracts.User.Responses
{
    public sealed class RegisterResponse
    {
        public long UserId { get; init; }
        public string PublicId { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public bool RequiresEmailVerification { get; init; }
    }
}