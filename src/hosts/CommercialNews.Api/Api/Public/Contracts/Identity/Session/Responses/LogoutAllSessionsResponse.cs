namespace CommercialNews.Api.Api.Public.Identity.Contracts.Session.Responses
{
    public sealed class LogoutAllSessionsResponse
    {
        public long UserId { get; init; }
        public bool LoggedOutAllSessions { get; init; }
    }
}