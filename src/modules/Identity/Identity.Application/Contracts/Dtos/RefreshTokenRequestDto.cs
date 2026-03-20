namespace Identity.Application.Contracts.Dtos
{
    public sealed class RefreshTokenRequestDto
    {
        public string RefreshToken { get; init; } = string.Empty;
    }
}