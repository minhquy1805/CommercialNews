namespace Identity.Application.Contracts.ResendVerificationEmail;

public sealed class ResendVerificationEmailResponseDto
{
    public bool Requested { get; init; }
    public string Message { get; init; } = string.Empty;
}