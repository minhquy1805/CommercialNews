using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.ArticleMedia.Requests;
using Media.Application.Contracts.ArticleMedia.Responses;

namespace Media.Application.UseCases.ArticleMedia.GetMediaUsage;

public interface IGetMediaUsageUseCase
{
    Task<Result<GetMediaUsageResponse>> ExecuteAsync(
        GetMediaUsageRequest request,
        CancellationToken cancellationToken = default);
}