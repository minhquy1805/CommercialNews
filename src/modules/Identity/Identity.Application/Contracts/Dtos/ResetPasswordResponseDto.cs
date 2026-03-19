namespace Identity.Application.Contracts.Dtos
{
    public sealed class ResetPasswordResponseDto
    {
        public long UserId { get; init; }
        public bool PasswordReset { get; init; }
    }
}