namespace Identity.Application.Contracts.Requests
{
    public sealed class ResendVerificationEmailRequestDto
    {
        public string Email { get; init; } = string.Empty;
    }
}

