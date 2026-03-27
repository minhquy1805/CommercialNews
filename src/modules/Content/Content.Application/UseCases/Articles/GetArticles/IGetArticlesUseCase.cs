using CommercialNews.BuildingBlocks.Contracts.Common;
using CommercialNews.BuildingBlocks.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.Articles.GetArticles;

public interface IGetArticlesUseCase
{
    Task<Result<PagedResponse<ArticleListItemDto>>> ExecuteAsync(
        GetArticlesRequestDto request,
        CancellationToken cancellationToken = default);
}