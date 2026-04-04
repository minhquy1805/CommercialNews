namespace CommercialNews.Api.Api.Admin.Contracts.Media.ArticleMedia.Responses;

public sealed class ReorderArticleMediaHttpResponse
{
    public long ArticleId { get; init; }
    public bool Reordered { get; init; }
    public int AffectedRows { get; init; }
}