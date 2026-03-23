namespace Identity.Application.Contracts.Responses
{
    public sealed class LogoutResponseDto
    {
        public long UserId { get; init; }
        public bool LoggedOut { get; init; }
    }
}

