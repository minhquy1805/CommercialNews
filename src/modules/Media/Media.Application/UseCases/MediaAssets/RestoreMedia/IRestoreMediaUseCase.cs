using CommercialNews.BuildingBlocks.Results;
using Media.Application.Contracts.MediaAsset.Requests;
using Media.Application.Contracts.MediaAsset.Responses;

namespace Media.Application.UseCases.MediaAssets.RestoreMedia;

public interface IRestoreMediaUseCase
{
    Task<Result<RestoreMediaResponse>> ExecuteAsync(
        RestoreMediaRequest request,
        CancellationToken cancellationToken = default);
}