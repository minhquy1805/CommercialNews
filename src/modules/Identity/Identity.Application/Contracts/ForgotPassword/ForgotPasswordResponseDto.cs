namespace Identity.Application.Contracts.ForgotPassword;

public sealed class ForgotPasswordResponseDto
{
    public bool Requested { get; init; }
    public string Message { get; init; } = string.Empty;
}