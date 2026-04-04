namespace Media.Application.Contracts.ArticleMedia.Responses;

public sealed class GetArticleMediaListResponse
{
    public IReadOnlyCollection<GetArticleMediaListItemResponse> Items { get; init; }
        = Array.Empty<GetArticleMediaListItemResponse>();

    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
}