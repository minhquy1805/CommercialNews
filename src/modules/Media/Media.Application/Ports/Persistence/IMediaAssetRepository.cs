using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Media.Application.Models.Commands;
using Media.Application.Models.Queries;
using Media.Application.Models.Results;
using Media.Domain.Entities;

namespace Media.Application.Ports.Persistence;

public interface IMediaAssetRepository
{
    Task<MediaAssetInsertResult> InsertAsync(
        CreateMediaAssetCommand command,
        CancellationToken cancellationToken = default);

    Task<MediaAssetMutationResult> UpdateMetadataAsync(
        UpdateMediaMetadataCommand command,
        CancellationToken cancellationToken = default);

    Task<MediaAsset?> GetByIdAsync(
        long mediaId,
        CancellationToken cancellationToken = default);

    Task<MediaAsset?> GetByPublicIdAsync(
        string publicId,
        CancellationToken cancellationToken = default);

    Task<MediaAssetMutationResult> SoftDeleteAsync(
        SoftDeleteMediaAssetCommand command,
        CancellationToken cancellationToken = default);

    Task<MediaAssetMutationResult> RestoreAsync(
        RestoreMediaAssetCommand command,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<MediaAssetListResultItem>> SelectSkipAndTakeAsync(
        MediaAssetListQuery query,
        CancellationToken cancellationToken = default);
}