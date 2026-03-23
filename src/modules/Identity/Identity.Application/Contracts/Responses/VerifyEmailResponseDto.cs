namespace Identity.Application.Contracts.Responses
{
    public sealed class VerifyEmailResponseDto
    {
        public long UserId { get; init; }
        public bool Verified { get; init; }
    }
}
