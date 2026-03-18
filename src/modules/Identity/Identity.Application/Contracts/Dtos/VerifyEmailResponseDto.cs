namespace Identity.Application.Contracts.Dtos
{
    public sealed class VerifyEmailResponseDto
    {
        public long UserId { get; init; }
        public bool Verified { get; init; }
    }
}
