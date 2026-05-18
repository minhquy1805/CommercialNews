using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.MediaAsset.Requests;
using Media.Application.Contracts.MediaAsset.Responses;
using Media.Application.Errors;
using Media.Application.Ports.Persistence;
using Media.Domain.Entities;
using Media.Domain.Exceptions;

namespace Media.Application.UseCases.MediaAssets.GetMediaById;

public sealed class GetMediaByIdUseCase : IGetMediaByIdUseCase
{
    private readonly IMediaAssetRepository _mediaAssetRepository;

    public GetMediaByIdUseCase(IMediaAssetRepository mediaAssetRepository)
    {
        _mediaAssetRepository = mediaAssetRepository
            ?? throw new ArgumentNullException(nameof(mediaAssetRepository));
    }

    public async Task<Result<GetMediaByIdResponse>> ExecuteAsync(
        GetMediaByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            if (request.MediaId <= 0)
            {
                return Result<GetMediaByIdResponse>.Failure(
                    MediaErrors.MediaAsset.InvalidMediaId);
            }

            MediaAsset? mediaAsset = await _mediaAssetRepository.GetByIdAsync(
                request.MediaId,
                cancellationToken);

            if (mediaAsset is null)
            {
                return Result<GetMediaByIdResponse>.Failure(
                    MediaErrors.MediaAsset.NotFound);
            }

            return Result<GetMediaByIdResponse>.Success(
                MapResponse(mediaAsset));
        }
        catch (PersistenceException exception)
        {
            return Result<GetMediaByIdResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<GetMediaByIdResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static GetMediaByIdResponse MapResponse(MediaAsset mediaAsset)
    {
        return new GetMediaByIdResponse
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
            "MEDIA.MEDIA_ASSET_INVALID_MEDIA_ID" =>
                MediaErrors.MediaAsset.InvalidMediaId,

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