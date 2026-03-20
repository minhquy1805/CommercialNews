namespace Identity.Application.Contracts.Dtos
{
    public sealed class RefreshTokenResponseDto
    {
        public long UserId { get; init; }
        public string AccessToken { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
        public DateTime AccessTokenExpiresAtUtc { get; init; }
        public DateTime RefreshTokenExpiresAtUtc { get; init; }
    }
}