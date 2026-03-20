namespace Identity.Application.Contracts.Dtos
{
    public sealed class LogoutRequestDto
    {
        public string RefreshToken { get; init; } = string.Empty;
    }
}

