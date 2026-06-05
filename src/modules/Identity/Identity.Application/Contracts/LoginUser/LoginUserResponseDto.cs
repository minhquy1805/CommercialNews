namespace Identity.Application.Contracts.LoginUser;

public sealed class LoginUserResponseDto
{
    public long UserId { get; init; }
    public string PublicId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;

    public string AccessToken { get; init; } = string.Empty;
    public string? RefreshToken { get; init; }

    public DateTime AccessTokenExpiresAtUtc { get; init; }
    public DateTime? RefreshTokenExpiresAtUtc { get; init; }
}
