namespace CommercialNews.Api.Api.Admin.Contracts.Content.Articles.Requests;

public sealed class ArchiveArticleRequest
{
    public long ExpectedVersion { get; init; }
}