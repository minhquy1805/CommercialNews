namespace CommercialNews.Api.Api.Public.Identity.Contracts.User.Responses
{
    public sealed class ChangePasswordResponse
    {
        public long UserId { get; init; }
        public bool PasswordChanged { get; init; }
    }
}