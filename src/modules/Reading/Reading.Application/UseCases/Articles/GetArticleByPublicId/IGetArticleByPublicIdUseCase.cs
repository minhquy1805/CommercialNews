using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Contracts.Articles.Requests;
using Reading.Application.Contracts.Articles.Responses;

namespace Reading.Application.UseCases.Articles.GetArticleByPublicId;

public interface IGetArticleByPublicIdUseCase
{
    Task<Result<ArticleDetailResponse>> ExecuteAsync(
        GetArticleByPublicIdRequest request,
        CancellationToken cancellationToken = default);
}