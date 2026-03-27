namespace Content.Application.Models.QueryModels
{
    public sealed class ArticleListResultItem
    {
        public long ArticleId { get; init; }
        public string PublicId { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;
        public string? Summary { get; init; }

        public string Status { get; init; } = string.Empty;

        public long AuthorUserId { get; init; }
        public long? CategoryId { get; init; }
        public long? CoverMediaId { get; init; }

        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public DateTime? PublishedAt { get; init; }

        public int Version { get; init; }
    }
}

