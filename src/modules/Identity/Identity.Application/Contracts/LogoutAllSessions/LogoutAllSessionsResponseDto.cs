namespace Identity.Application.Contracts.LogoutAllSessions;

public sealed class LogoutAllSessionsResponseDto
{
    public long UserId { get; init; }
    public bool LoggedOutAllSessions { get; init; }
}