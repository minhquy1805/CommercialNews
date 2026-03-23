namespace Identity.Application.Contracts.Requests
{
    public sealed class ResetPasswordRequestDto
    {
        public string Token { get; init; } = string.Empty;
        public string NewPassword { get; init; } = string.Empty;
    }
}