namespace Reading.Application.Contracts.Responses;

public sealed class GetRelatedArticlesResponse
{
    public IReadOnlyList<ArticleListItemResponse> Items { get; set; } = Array.Empty<ArticleListItemResponse>();
}