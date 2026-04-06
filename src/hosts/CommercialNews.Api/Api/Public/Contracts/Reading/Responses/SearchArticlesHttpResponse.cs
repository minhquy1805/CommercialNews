using CommercialNews.BuildingBlocks.Contracts.Common;

namespace CommercialNews.Api.Api.Public.Contracts.Reading.Responses;

public sealed class SearchArticlesHttpResponse
{
    public IReadOnlyList<ArticleListItemHttpResponse> Items { get; init; } = Array.Empty<ArticleListItemHttpResponse>();

    public PageInfo PageInfo { get; init; } = new();
}