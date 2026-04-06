using CommercialNews.BuildingBlocks.Contracts.Common;

namespace Reading.Application.Contracts.Responses;

public sealed class SearchArticlesResponse
{
    public IReadOnlyList<ArticleListItemResponse> Items { get; set; } = Array.Empty<ArticleListItemResponse>();

    public PageInfo PageInfo { get; set; } = new();
}