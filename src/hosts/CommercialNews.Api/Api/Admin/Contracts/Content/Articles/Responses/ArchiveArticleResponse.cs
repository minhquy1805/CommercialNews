namespace CommercialNews.Api.Api.Admin.Contracts.Content.Articles.Responses
{
    public sealed class ArchiveArticleResponse
    {
        public long ArticleId { get; init; }
        public string PublicId { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateTime? ArchivedAt { get; init; }
        public int Version { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}

