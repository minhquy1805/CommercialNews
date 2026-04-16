namespace Identity.Application.Contracts.Logout;

public sealed class LogoutResponseDto
{
    public long UserId { get; init; }
    public bool LoggedOut { get; init; }
}