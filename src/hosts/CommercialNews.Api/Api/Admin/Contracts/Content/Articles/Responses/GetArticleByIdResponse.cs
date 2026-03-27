namespace CommercialNews.Api.Api.Admin.Contracts.Content.Articles.Responses
{
    public sealed class GetArticleByIdResponse
    {
        public long ArticleId { get; init; }
        public string PublicId { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;
        public string? Summary { get; init; }
        public string Body { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        public long AuthorUserId { get; init; }
        public long? CategoryId { get; init; }
        public long? CoverMediaId { get; init; }

        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }

        public DateTime? PublishedAt { get; init; }
        public DateTime? UnpublishedAt { get; init; }
        public DateTime? ArchivedAt { get; init; }

        public long? CreatedByUserId { get; init; }
        public long? UpdatedByUserId { get; init; }

        public bool IsDeleted { get; init; }
        public DateTime? DeletedAt { get; init; }
        public long? DeletedByUserId { get; init; }

        public int Version { get; init; }
    }
}

