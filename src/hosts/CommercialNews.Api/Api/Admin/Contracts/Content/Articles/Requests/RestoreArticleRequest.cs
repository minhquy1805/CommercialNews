namespace CommercialNews.Api.Api.Admin.Contracts.Content.Articles.Requests
{
    public sealed class RestoreArticleRequest
    {
        public int ExpectedVersion { get; init; }
    }
}

