namespace Identity.Application.Contracts.Responses
{
    public sealed class ResetPasswordResponseDto
    {
        public long UserId { get; init; }
        public bool PasswordReset { get; init; }
    }
}