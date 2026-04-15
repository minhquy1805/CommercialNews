

using CommercialNews.BuildingBlocks.SharedKernel.Paging;

namespace Reading.Application.Contracts.Responses;

public sealed class GetArticlesResponse
{
    public IReadOnlyList<ArticleListItemResponse> Items { get; set; } = Array.Empty<ArticleListItemResponse>();

    public PageInfo PageInfo { get; set; } = new();
}