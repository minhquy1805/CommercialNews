namespace Content.Application.Contracts.Responses
{
    public sealed class RestoreCategoryResponseDto
    {
        public long CategoryId { get; init; }
        public bool IsDeleted { get; init; }
        public int Version { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}