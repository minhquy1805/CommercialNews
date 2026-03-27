namespace Content.Application.Contracts.Responses
{
    public sealed class UpdateArticleResponseDto
    {
        public long ArticleId { get; init; }
        public string PublicId { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;
        public string? Summary { get; init; }
        public string Body { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        public long? CategoryId { get; init; }
        public long? CoverMediaId { get; init; }

        public int Version { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}

