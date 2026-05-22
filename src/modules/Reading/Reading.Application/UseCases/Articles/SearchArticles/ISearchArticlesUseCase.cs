using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Contracts.Articles.Requests;
using Reading.Application.Contracts.Articles.Responses;

namespace Reading.Application.UseCases.Articles.SearchArticles;

public interface ISearchArticlesUseCase
{
    Task<Result<GetArticlesResponse>> ExecuteAsync(
        SearchArticlesRequest request,
        CancellationToken cancellationToken = default);
}