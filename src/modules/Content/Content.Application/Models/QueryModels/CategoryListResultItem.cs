namespace Content.Application.Models.QueryModels
{
    public sealed class CategoryListResultItem
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

        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }

        public int Version { get; init; }
    }
}