namespace Identity.Application.Contracts.Requests
{
    public sealed class RefreshTokenRequestDto
    {
        public string RefreshToken { get; init; } = string.Empty;
    }
}