namespace Identity.Application.Contracts.Dtos
{
    public sealed class ChangePasswordResponseDto
    {
        public long UserId { get; init; }
        public bool PasswordChanged { get; init; }
    }
}