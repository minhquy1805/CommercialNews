using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Contracts.Articles.Requests;
using Reading.Application.Contracts.Articles.Responses;

namespace Reading.Application.UseCases.Articles.GetArticles;

public interface IGetArticlesUseCase
{
    Task<Result<GetArticlesResponse>> ExecuteAsync(
        GetArticlesRequest request,
        CancellationToken cancellationToken = default);
}