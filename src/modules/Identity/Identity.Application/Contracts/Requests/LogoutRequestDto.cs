namespace Identity.Application.Contracts.Requests
{
    public sealed class LogoutRequestDto
    {
        public string RefreshToken { get; init; } = string.Empty;
    }
}

