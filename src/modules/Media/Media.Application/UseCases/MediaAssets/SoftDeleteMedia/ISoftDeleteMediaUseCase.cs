using CommercialNews.BuildingBlocks.Results;
using Media.Application.Contracts.MediaAsset.Requests;
using Media.Application.Contracts.MediaAsset.Responses;

namespace Media.Application.UseCases.MediaAssets.SoftDeleteMedia;

public interface ISoftDeleteMediaUseCase
{
    Task<Result<SoftDeleteMediaResponse>> ExecuteAsync(
        SoftDeleteMediaRequest request,
        CancellationToken cancellationToken = default);
}