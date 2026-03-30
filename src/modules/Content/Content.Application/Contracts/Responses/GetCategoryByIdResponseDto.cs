namespace Content.Application.Contracts.Responses
{
    public sealed class GetCategoryByIdResponseDto
    {
        public long CategoryId { get; init; }
        public string PublicId { get; init; } = string.Empty;
        public long? ParentCategoryId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string NameNormalized { get; init; } = string.Empty;
        public string? Description { get; init; }
        public bool IsActive { get; init; }
        public int DisplayOrder { get; init; }
        public bool IsDeleted { get; init; }
        public int Version { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public DateTime? DeletedAt { get; init; }
    }
}