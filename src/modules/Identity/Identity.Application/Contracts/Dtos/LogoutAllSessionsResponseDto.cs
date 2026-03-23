namespace Identity.Application.Contracts.Dtos
{
    public sealed class LogoutAllSessionsResponseDto
    {
        public long UserId { get; init; }
        public bool LoggedOutAllSessions { get; init; }
    }
}

