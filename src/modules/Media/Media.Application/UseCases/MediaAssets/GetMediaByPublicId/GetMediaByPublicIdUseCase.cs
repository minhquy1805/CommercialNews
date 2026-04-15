using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.MediaAsset.Requests;
using Media.Application.Contracts.MediaAsset.Responses;
using Media.Application.Errors;
using Media.Application.Ports.Persistence;
using Media.Domain.Exceptions;

namespace Media.Application.UseCases.MediaAssets.GetMediaByPublicId;

public sealed class GetMediaByPublicIdUseCase : IGetMediaByPublicIdUseCase
{
    private readonly IMediaAssetRepository _mediaAssetRepository;

    public GetMediaByPublicIdUseCase(IMediaAssetRepository mediaAssetRepository)
    {
        _mediaAssetRepository = mediaAssetRepository;
    }

    public async Task<Result<GetMediaByPublicIdResponse>> ExecuteAsync(
        GetMediaByPublicIdRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.PublicId))
            {
                return Result<GetMediaByPublicIdResponse>.Failure(MediaErrors.MediaAsset.PublicIdRequired);
            }

            if (request.PublicId.Trim().Length != 26)
            {
                return Result<GetMediaByPublicIdResponse>.Failure(MediaErrors.MediaAsset.PublicIdInvalid);
            }

            var mediaAsset = await _mediaAssetRepository.GetByPublicIdAsync(
                request.PublicId.Trim(),
                cancellationToken);

            if (mediaAsset is null)
            {
                return Result<GetMediaByPublicIdResponse>.Failure(MediaErrors.MediaAsset.NotFound);
            }

            return Result<GetMediaByPublicIdResponse>.Success(new GetMediaByPublicIdResponse
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
            return Result<GetMediaByPublicIdResponse>.Failure(MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<GetMediaByPublicIdResponse>.Failure(MapDomainException(exception));
        }
    }

    private static Error MapDomainException(MediaDomainException exception)
    {
        return exception.Code switch
        {
            "MEDIA.MEDIA_ASSET_PUBLIC_ID_REQUIRED" => MediaErrors.MediaAsset.PublicIdRequired,
            "MEDIA.MEDIA_ASSET_PUBLIC_ID_INVALID" => MediaErrors.MediaAsset.PublicIdInvalid,
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