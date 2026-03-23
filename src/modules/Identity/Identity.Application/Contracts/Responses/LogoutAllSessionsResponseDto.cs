namespace Identity.Application.Contracts.Responses
{
    public sealed class LogoutAllSessionsResponseDto
    {
        public long UserId { get; init; }
        public bool LoggedOutAllSessions { get; init; }
    }
}

