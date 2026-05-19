namespace CommercialNews.Api.Api.Admin.Contracts.Media.ArticleMedia.Requests;

public sealed class ReorderArticleMediaHttpRequest
{
    public int? ExpectedVersion { get; init; }

    public IReadOnlyCollection<ReorderArticleMediaItemHttpRequest> Items { get; init; }
        = Array.Empty<ReorderArticleMediaItemHttpRequest>();
}

public sealed class ReorderArticleMediaItemHttpRequest
{
    public long MediaId { get; init; }

    public int SortOrder { get; init; }
}