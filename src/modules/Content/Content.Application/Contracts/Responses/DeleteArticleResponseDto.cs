namespace Content.Application.Contracts.Responses
{
    public sealed class DeleteArticleResponseDto
    {
        public long ArticleId { get; init; }

        public string PublicId { get; init; } = string.Empty;

        public bool IsDeleted { get; init; }

        public DateTime? DeletedAt { get; init; }

        public int Version { get; init; }

        public DateTime UpdatedAt { get; init; }
    }
}