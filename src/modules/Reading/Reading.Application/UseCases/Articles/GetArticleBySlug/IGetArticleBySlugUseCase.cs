using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Contracts.Articles.Requests;
using Reading.Application.Contracts.Articles.Responses;

namespace Reading.Application.UseCases.Articles.GetArticleBySlug;

public interface IGetArticleBySlugUseCase
{
    Task<Result<ArticleDetailResponse>> ExecuteAsync(
        GetArticleBySlugRequest request,
        CancellationToken cancellationToken = default);
}