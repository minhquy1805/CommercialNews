namespace Content.Application.Contracts.Responses
{
    public sealed class DeleteCategoryResponseDto
    {
        public long CategoryId { get; init; }
        public bool IsDeleted { get; init; }
        public int Version { get; init; }
        public DateTime? DeletedAt { get; init; }
    }
}