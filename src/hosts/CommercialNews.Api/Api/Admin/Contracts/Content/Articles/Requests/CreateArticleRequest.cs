namespace CommercialNews.Api.Api.Admin.Contracts.Content.Articles.Requests
{
    public sealed class CreateArticleRequest
    {
        public string Title { get; init; } = string.Empty;
        public string? Summary { get; init; }
        public string Body { get; init; } = string.Empty;
        public long AuthorUserId { get; init; }
        public long? CategoryId { get; init; }
        public long? CoverMediaId { get; init; }
    }
}