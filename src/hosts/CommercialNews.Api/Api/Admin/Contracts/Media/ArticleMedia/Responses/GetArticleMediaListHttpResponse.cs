namespace CommercialNews.Api.Api.Admin.Contracts.Media.ArticleMedia.Responses;

public sealed class GetArticleMediaListHttpResponse
{
    public IReadOnlyCollection<GetArticleMediaListItemHttpResponse> Items { get; init; }
        = Array.Empty<GetArticleMediaListItemHttpResponse>();

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }
}