namespace CommercialNews.Api.Api.Public.Identity.Contracts.Session.Responses
{
    public sealed class LogoutResponse
    {
        public long UserId { get; init; }
        public bool LoggedOut { get; init; }
    }
}