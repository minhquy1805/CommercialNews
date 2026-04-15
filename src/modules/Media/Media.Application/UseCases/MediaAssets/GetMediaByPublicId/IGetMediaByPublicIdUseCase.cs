using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.MediaAsset.Requests;
using Media.Application.Contracts.MediaAsset.Responses;

namespace Media.Application.UseCases.MediaAssets.GetMediaByPublicId;

public interface IGetMediaByPublicIdUseCase
{
    Task<Result<GetMediaByPublicIdResponse>> ExecuteAsync(
        GetMediaByPublicIdRequest request,
        CancellationToken cancellationToken = default);
}