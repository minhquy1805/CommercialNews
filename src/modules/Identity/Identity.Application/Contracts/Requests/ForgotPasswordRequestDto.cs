namespace Identity.Application.Contracts.Requests
{
    public sealed class ForgotPasswordRequestDto
    {
        public string Email { get; init; } = string.Empty;
    }
}
