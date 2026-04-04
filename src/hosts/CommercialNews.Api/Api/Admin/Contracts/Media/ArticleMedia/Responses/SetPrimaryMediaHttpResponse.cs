namespace CommercialNews.Api.Api.Admin.Contracts.Media.ArticleMedia.Responses;

public sealed class SetPrimaryMediaHttpResponse
{
    public long ArticleId { get; init; }
    public long MediaId { get; init; }
    public bool PrimarySet { get; init; }
    public int AffectedRows { get; init; }
}