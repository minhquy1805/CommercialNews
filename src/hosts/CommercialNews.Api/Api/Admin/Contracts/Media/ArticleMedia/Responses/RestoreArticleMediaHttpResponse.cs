namespace CommercialNews.Api.Api.Admin.Contracts.Media.ArticleMedia.Responses;

public sealed class RestoreArticleMediaHttpResponse
{
    public long ArticleId { get; init; }
    public long MediaId { get; init; }
    public bool Restored { get; init; }
    public int AffectedRows { get; init; }
}