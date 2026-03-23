namespace Identity.Application.Contracts.Responses
{
    public sealed class ForgotPasswordResponseDto
    {
        public bool Requested { get; init; }
        public string Message { get; init; } = string.Empty;
    }
}
