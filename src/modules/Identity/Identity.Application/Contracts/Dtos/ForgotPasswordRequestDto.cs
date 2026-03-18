namespace Identity.Application.Contracts.Dtos
{
    public sealed class ForgotPasswordRequestDto
    {
        public string Email { get; init; } = string.Empty;
    }
}
