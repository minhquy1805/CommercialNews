namespace CommercialNews.Api.Api.Admin.Contracts.Content.Articles.Requests
{
    public sealed class UnpublishArticleRequest
    {
        public int ExpectedVersion { get; init; }
        public string Reason { get; init; } = string.Empty;
    }
}

