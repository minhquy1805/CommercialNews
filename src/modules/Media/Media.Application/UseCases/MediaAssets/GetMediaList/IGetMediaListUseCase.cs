using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.MediaAsset.Requests;
using Media.Application.Contracts.MediaAsset.Responses;

namespace Media.Application.UseCases.MediaAssets.GetMediaList;

public interface IGetMediaListUseCase
{
    Task<Result<GetMediaListResponse>> ExecuteAsync(
        GetMediaListRequest request,
        CancellationToken cancellationToken = default);
}