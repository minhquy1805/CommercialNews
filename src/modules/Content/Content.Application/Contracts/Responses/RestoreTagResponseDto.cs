namespace Content.Application.Contracts.Responses
{
    public sealed class RestoreTagResponseDto
    {
        public long TagId { get; init; }
        public bool IsDeleted { get; init; }

        public int Version { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}