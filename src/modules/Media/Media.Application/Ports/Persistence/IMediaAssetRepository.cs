using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Media.Application.Models.QueryModels;
using Media.Domain.Entities;

namespace Media.Application.Ports.Persistence;

public interface IMediaAssetRepository
{
    Task<long> InsertAsync(
        MediaAsset mediaAsset,
        CancellationToken cancellationToken = default);

    Task<MediaAsset?> GetByIdAsync(
        long mediaId,
        CancellationToken cancellationToken = default);

    Task<MediaAsset?> GetByPublicIdAsync(
        string publicId,
        CancellationToken cancellationToken = default);

    Task<int> SoftDeleteAsync(
        long mediaId,
        long? deletedByUserId,
        DateTime? restoreUntil,
        CancellationToken cancellationToken = default);

    Task<int> RestoreAsync(
        long mediaId,
        long? restoredByUserId,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<MediaAssetListResultItem>> SelectSkipAndTakeAsync(
        MediaAssetListQuery query,
        CancellationToken cancellationToken = default);
}