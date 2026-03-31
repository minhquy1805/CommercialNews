namespace CommercialNews.Api.Api.Public.Identity.Contracts.User.Requests
{
    public sealed class ChangePasswordRequest
    {
        public string CurrentPassword { get; init; } = string.Empty;
        public string NewPassword { get; init; } = string.Empty;
    }
}