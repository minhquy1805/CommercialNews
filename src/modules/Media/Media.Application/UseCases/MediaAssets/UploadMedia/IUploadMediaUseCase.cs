using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.MediaAsset.Requests;
using Media.Application.Contracts.MediaAsset.Responses;

namespace Media.Application.UseCases.MediaAssets.UploadMedia;

public interface IUploadMediaUseCase
{
    Task<Result<UploadMediaResponse>> ExecuteAsync(
        UploadMediaRequest request,
        CancellationToken cancellationToken = default);
}