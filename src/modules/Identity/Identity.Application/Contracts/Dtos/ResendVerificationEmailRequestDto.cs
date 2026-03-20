namespace Identity.Application.Contracts.Dtos
{
    public sealed class ResendVerificationEmailRequestDto
    {
        public string Email { get; init; } = string.Empty;
    }
}

