using CommercialNews.BuildingBlocks.Results;
using Media.Application.Contracts.MediaAsset.Requests;
using Media.Application.Contracts.MediaAsset.Responses;

namespace Media.Application.UseCases.MediaAssets.GetMediaById;

public interface IGetMediaByIdUseCase
{
    Task<Result<GetMediaByIdResponse>> ExecuteAsync(
        GetMediaByIdRequest request,
        CancellationToken cancellationToken = default);
}