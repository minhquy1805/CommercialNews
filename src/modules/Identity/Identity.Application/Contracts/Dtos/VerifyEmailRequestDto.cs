namespace Identity.Application.Contracts.Dtos
{
    public sealed class VerifyEmailRequestDto
    {
        public string Token { get; init; } = string.Empty;
    }
}
