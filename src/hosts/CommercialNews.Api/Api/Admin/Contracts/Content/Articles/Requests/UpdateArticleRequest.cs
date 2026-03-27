namespace CommercialNews.Api.Api.Admin.Contracts.Content.Articles.Requests
{
    public sealed class UpdateArticleRequest
    {
        public int ExpectedVersion { get; init; }

        public string Title { get; init; } = string.Empty;
        public string? Summary { get; init; }
        public string Body { get; init; } = string.Empty;

        public long? CategoryId { get; init; }
        public long? CoverMediaId { get; init; }

        public string? ChangeSummary { get; init; }
    }
}