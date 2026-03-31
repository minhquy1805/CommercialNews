namespace CommercialNews.Api.Api.Public.Identity.Contracts.User.Requests
{
    public sealed class UpdateMyProfileRequest
    {
        public string? FullName { get; init; }
        public string? AvatarUrl { get; init; }
    }
}