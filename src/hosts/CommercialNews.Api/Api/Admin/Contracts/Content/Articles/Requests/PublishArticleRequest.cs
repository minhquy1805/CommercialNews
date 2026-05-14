namespace CommercialNews.Api.Api.Admin.Contracts.Content.Articles.Requests;

public sealed class PublishArticleRequest
{
    public long ExpectedVersion { get; init; }
}