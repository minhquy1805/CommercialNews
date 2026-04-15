using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.ArticleMedia.Requests;
using Media.Application.Contracts.ArticleMedia.Responses;

namespace Media.Application.UseCases.ArticleMedia.GetArticlePrimaryMedia;

public interface IGetArticlePrimaryMediaUseCase
{
    Task<Result<GetArticlePrimaryMediaResponse>> ExecuteAsync(
        GetArticlePrimaryMediaRequest request,
        CancellationToken cancellationToken = default);
}