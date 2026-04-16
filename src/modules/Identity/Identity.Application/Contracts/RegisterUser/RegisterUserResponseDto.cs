namespace Identity.Application.Contracts.RegisterUser;

public sealed class RegisterUserResponseDto
{
    public long UserId { get; init; }
    public string PublicId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool RequiresEmailVerification { get; init; }
}