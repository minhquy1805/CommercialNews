namespace CommercialNews.Api.Api.Admin.Contracts.Content.Articles.Requests;

public sealed class SoftDeleteArticleRequest
{
    public long ExpectedVersion { get; init; }
}