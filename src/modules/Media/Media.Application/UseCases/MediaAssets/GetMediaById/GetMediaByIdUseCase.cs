using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.MediaAsset.Requests;
using Media.Application.Contracts.MediaAsset.Responses;
using Media.Application.Errors;
using Media.Application.Ports.Persistence;
using Media.Domain.Exceptions;

namespace Media.Application.UseCases.MediaAssets.GetMediaById;

public sealed class GetMediaByIdUseCase : IGetMediaByIdUseCase
{
    private readonly IMediaAssetRepository _mediaAssetRepository;

    public GetMediaByIdUseCase(IMediaAssetRepository mediaAssetRepository)
    {
        _mediaAssetRepository = mediaAssetRepository;
    }

    public async Task<Result<GetMediaByIdResponse>> ExecuteAsync(
        GetMediaByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.MediaId <= 0)
            {
                return Result<GetMediaByIdResponse>.Failure(MediaErrors.MediaAsset.InvalidMediaId);
            }

            var mediaAsset = await _mediaAssetRepository.GetByIdAsync(
                request.MediaId,
                cancellationToken);

            if (mediaAsset is null)
            {
                return Result<GetMediaByIdResponse>.Failure(MediaErrors.MediaAsset.NotFound);
            }

            return Result<GetMediaByIdResponse>.Success(new GetMediaByIdResponse
            {
                MediaId = mediaAsset.MediaId,
                PublicId = mediaAsset.PublicId,
                StorageProvider = mediaAsset.StorageProvider,
                Url = mediaAsset.Url,
                StoragePath = mediaAsset.StoragePath,
                FileName = mediaAsset.FileName,
                MediaType = mediaAsset.MediaType,
                MimeType = mediaAsset.MimeType,
                FileSizeBytes = mediaAsset.FileSizeBytes,
                Width = mediaAsset.Width,
                Height = mediaAsset.Height,
                DurationSeconds = mediaAsset.DurationSeconds,
                AltText = mediaAsset.AltText,
                MetadataJson = mediaAsset.MetadataJson,
                CreatedAt = mediaAsset.CreatedAt,
                CreatedByUserId = mediaAsset.CreatedByUserId,
                UpdatedAt = mediaAsset.UpdatedAt,
                UpdatedByUserId = mediaAsset.UpdatedByUserId,
                IsDeleted = mediaAsset.IsDeleted,
                DeletedAt = mediaAsset.DeletedAt,
                DeletedByUserId = mediaAsset.DeletedByUserId,
                RestoreUntil = mediaAsset.RestoreUntil,
                Version = mediaAsset.Version
            });
        }
        catch (PersistenceException exception)
        {
            return Result<GetMediaByIdResponse>.Failure(MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<GetMediaByIdResponse>.Failure(MapDomainException(exception));
        }
    }

    private static Error MapDomainException(MediaDomainException exception)
    {
        return exception.Code switch
        {
            "MEDIA.MEDIA_ASSET_INVALID_MEDIA_ID" => MediaErrors.MediaAsset.InvalidMediaId,
            "MEDIA.MEDIA_ASSET_INVALID_VERSION" => MediaErrors.MediaAsset.InvalidVersion,
            "MEDIA.MEDIA_ASSET_DELETED_AT_REQUIRED" => MediaErrors.MediaAsset.InvalidDeletedAt,
            "MEDIA.MEDIA_ASSET_DELETED_AT_INVALID" => MediaErrors.MediaAsset.InvalidDeletedAt,
            _ => MediaErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            _ => MediaErrors.ValidationFailed
        };
    }
}