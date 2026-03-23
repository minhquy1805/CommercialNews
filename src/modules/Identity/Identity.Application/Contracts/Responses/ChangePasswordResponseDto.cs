namespace Identity.Application.Contracts.Responses
{
    public sealed class ChangePasswordResponseDto
    {
        public long UserId { get; init; }
        public bool PasswordChanged { get; init; }
    }
}