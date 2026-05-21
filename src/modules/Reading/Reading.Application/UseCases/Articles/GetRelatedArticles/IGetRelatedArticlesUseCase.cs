using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Contracts.Articles.Requests;
using Reading.Application.Contracts.Articles.Responses;

namespace Reading.Application.UseCases.Articles.GetRelatedArticles;

public interface IGetRelatedArticlesUseCase
{
    Task<Result<IReadOnlyList<ArticleListItemResponse>>> ExecuteAsync(
        GetRelatedArticlesRequest request,
        CancellationToken cancellationToken = default);
}