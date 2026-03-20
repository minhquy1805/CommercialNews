namespace Identity.Application.Contracts.Dtos
{
    public sealed class LogoutResponseDto
    {
        public long UserId { get; init; }
        public bool LoggedOut { get; init; }
    }
}

