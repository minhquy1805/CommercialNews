namespace Content.Application.Contracts.Responses
{
    public sealed class RestoreArticleResponseDto
    {
        public long ArticleId { get; init; }

        public string PublicId { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        public int Version { get; init; }

        public DateTime UpdatedAt { get; init; }
    }
}

