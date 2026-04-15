using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.MediaAsset.Requests;
using Media.Application.Contracts.MediaAsset.Responses;

namespace Media.Application.UseCases.MediaAssets.RegisterMedia;

public interface IRegisterMediaUseCase
{
    Task<Result<RegisterMediaResponse>> ExecuteAsync(
        RegisterMediaRequest request,
        CancellationToken cancellationToken = default);
}