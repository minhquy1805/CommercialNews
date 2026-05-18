using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.MediaAsset.Requests;
using Media.Application.Contracts.MediaAsset.Responses;

namespace Media.Application.UseCases.MediaAssets.UpdateMediaAsset;

public interface IUpdateMediaAssetUseCase
{
    Task<Result<UpdateMediaAssetResponse>> ExecuteAsync(
        UpdateMediaAssetRequest request,
        CancellationToken cancellationToken = default);
}