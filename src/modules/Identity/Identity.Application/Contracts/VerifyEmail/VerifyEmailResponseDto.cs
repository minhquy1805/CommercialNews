namespace Identity.Application.Contracts.VerifyEmail;

public sealed class VerifyEmailResponseDto
{
    public long UserId { get; init; }
    public bool Verified { get; init; }
}