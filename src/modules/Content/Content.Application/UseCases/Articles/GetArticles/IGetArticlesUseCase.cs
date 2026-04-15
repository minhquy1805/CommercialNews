using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.Articles.GetArticles;

public interface IGetArticlesUseCase
{
    Task<Result<PagedQueryResult<ArticleListItemDto>>> ExecuteAsync(
        GetArticlesRequestDto request,
        CancellationToken cancellationToken = default);
}