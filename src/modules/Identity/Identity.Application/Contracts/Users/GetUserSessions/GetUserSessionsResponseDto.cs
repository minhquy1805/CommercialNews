namespace Identity.Application.Contracts.Users.GetUserSessions;

public sealed class GetUserSessionsResponseDto
{
    public long UserId { get; init; }

    public IReadOnlyList<UserSessionItemDto> Items { get; init; } =
        Array.Empty<UserSessionItemDto>();
}