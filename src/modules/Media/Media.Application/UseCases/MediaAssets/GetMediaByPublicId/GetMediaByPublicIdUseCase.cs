using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.MediaAsset.Requests;
using Media.Application.Contracts.MediaAsset.Responses;
using Media.Application.Errors;
using Media.Application.Ports.Persistence;
using Media.Domain.Entities;
using Media.Domain.Exceptions;

namespace Media.Application.UseCases.MediaAssets.GetMediaByPublicId;

public sealed class GetMediaByPublicIdUseCase : IGetMediaByPublicIdUseCase
{
    private readonly IMediaAssetRepository _mediaAssetRepository;

    public GetMediaByPublicIdUseCase(IMediaAssetRepository mediaAssetRepository)
    {
        _mediaAssetRepository = mediaAssetRepository
            ?? throw new ArgumentNullException(nameof(mediaAssetRepository));
    }

    public async Task<Result<GetMediaByPublicIdResponse>> ExecuteAsync(
        GetMediaByPublicIdRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            if (string.IsNullOrWhiteSpace(request.PublicId))
            {
                return Result<GetMediaByPublicIdResponse>.Failure(
                    MediaErrors.MediaAsset.PublicIdRequired);
            }

            string publicId = request.PublicId.Trim();

            if (publicId.Length != 26)
            {
                return Result<GetMediaByPublicIdResponse>.Failure(
                    MediaErrors.MediaAsset.PublicIdInvalid);
            }

            MediaAsset? mediaAsset = await _mediaAssetRepository.GetByPublicIdAsync(
                publicId,
                cancellationToken);

            if (mediaAsset is null)
            {
                return Result<GetMediaByPublicIdResponse>.Failure(
                    MediaErrors.MediaAsset.NotFound);
            }

            return Result<GetMediaByPublicIdResponse>.Success(
                MapResponse(mediaAsset));
        }
        catch (PersistenceException exception)
        {
            return Result<GetMediaByPublicIdResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<GetMediaByPublicIdResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static GetMediaByPublicIdResponse MapResponse(MediaAsset mediaAsset)
    {
        return new GetMediaByPublicIdResponse
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
            CreatedBy = mediaAsset.CreatedBy,

            UpdatedAt = mediaAsset.UpdatedAt,
            UpdatedBy = mediaAsset.UpdatedBy,

            IsDeleted = mediaAsset.IsDeleted,
            DeletedAt = mediaAsset.DeletedAt,
            DeletedBy = mediaAsset.DeletedBy,
            RestoreUntil = mediaAsset.RestoreUntil,

            RestoredAt = mediaAsset.RestoredAt,
            RestoredBy = mediaAsset.RestoredBy,

            Version = mediaAsset.Version
        };
    }

    private static Error MapDomainException(MediaDomainException exception)
    {
        return exception.Code switch
        {
            "MEDIA.MEDIA_ASSET_PUBLIC_ID_REQUIRED" =>
                MediaErrors.MediaAsset.PublicIdRequired,

            "MEDIA.MEDIA_ASSET_PUBLIC_ID_INVALID" =>
                MediaErrors.MediaAsset.PublicIdInvalid,

            "MEDIA.MEDIA_ASSET_INVALID_VERSION" =>
                MediaErrors.MediaAsset.InvalidVersion,

            "MEDIA.MEDIA_ASSET_DELETED_AT_REQUIRED" =>
                MediaErrors.MediaAsset.InvalidDeletedAt,

            "MEDIA.MEDIA_ASSET_DELETED_AT_INVALID" =>
                MediaErrors.MediaAsset.InvalidDeletedAt,

            "MEDIA.MEDIA_ASSET_DELETED_BY_INVALID" =>
                MediaErrors.MediaAsset.InvalidDeletedBy,

            "MEDIA.MEDIA_ASSET_RESTORED_AT_REQUIRED" =>
                MediaErrors.MediaAsset.RestoredAtRequired,

            _ => MediaErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "MEDIA.MEDIA_NOT_FOUND" =>
                MediaErrors.MediaAsset.NotFound,

            "MEDIA.ACTOR_NOT_FOUND" =>
                MediaErrors.Actor.NotFound,

            "MEDIA.CONSTRAINT_VIOLATION" =>
                MediaErrors.ConstraintViolation,

            "MEDIA.CONCURRENT_MODIFICATION" =>
                MediaErrors.ConcurrentModification,

            "MEDIA.DEPENDENCY_UNAVAILABLE" =>
                MediaErrors.DependencyUnavailable,

            "MEDIA.PERSISTENCE_ERROR" =>
                MediaErrors.PersistenceError,

            _ => MediaErrors.PersistenceError
        };
    }
}