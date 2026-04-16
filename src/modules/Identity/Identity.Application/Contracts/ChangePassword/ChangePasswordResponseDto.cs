namespace Identity.Application.Contracts.ChangePassword;

public sealed class ChangePasswordResponseDto
{
    public long UserId { get; init; }
    public bool PasswordChanged { get; init; }
}