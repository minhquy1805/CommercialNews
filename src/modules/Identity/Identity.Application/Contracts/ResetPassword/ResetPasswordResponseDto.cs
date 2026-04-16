namespace Identity.Application.Contracts.ResetPassword;

public sealed class ResetPasswordResponseDto
{
    public long UserId { get; init; }
    public bool PasswordReset { get; init; }
}