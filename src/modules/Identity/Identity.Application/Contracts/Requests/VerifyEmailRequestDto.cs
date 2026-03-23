namespace Identity.Application.Contracts.Requests
{
    public sealed class VerifyEmailRequestDto
    {
        public string Token { get; init; } = string.Empty;
    }
}
