using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Media.Application.Contracts.MediaAsset.Requests;
using Media.Application.Contracts.MediaAsset.Responses;
using Media.Application.Errors;
using Media.Application.Models.Commands;
using Media.Application.Models.Results;
using Media.Application.Ports.Persistence;
using Media.Application.Ports.Services;
using Media.Domain.Entities;
using Media.Domain.Exceptions;

namespace Media.Application.UseCases.MediaAssets.UpdateMediaAsset;

public sealed class UpdateMediaAssetUseCase : IUpdateMediaAssetUseCase
{
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly IMediaOutboxWriter _mediaOutboxWriter;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContext _requestContext;

    public UpdateMediaAssetUseCase(
        IMediaAssetRepository mediaAssetRepository,
        IMediaUnitOfWork unitOfWork,
        IMediaOutboxWriter mediaOutboxWriter,
        IDateTimeProvider dateTimeProvider,
        IRequestContext requestContext)
    {
        _mediaAssetRepository = mediaAssetRepository
            ?? throw new ArgumentNullException(nameof(mediaAssetRepository));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _mediaOutboxWriter = mediaOutboxWriter
            ?? throw new ArgumentNullException(nameof(mediaOutboxWriter));

        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));

        _requestContext = requestContext
            ?? throw new ArgumentNullException(nameof(requestContext));
    }

    public async Task<Result<UpdateMediaAssetResponse>> ExecuteAsync(
        UpdateMediaAssetRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            if (request.MediaId <= 0)
            {
                return Result<UpdateMediaAssetResponse>.Failure(
                    MediaErrors.MediaAsset.InvalidMediaId);
            }

            DateTime nowUtc = _dateTimeProvider.UtcNow;
            long? actorUserId = _requestContext.CurrentUserId;

            if (actorUserId is null or <= 0)
            {
                return Result<UpdateMediaAssetResponse>.Failure(
                    MediaErrors.Actor.NotFound);
            }

            MediaAsset? mediaAsset = await _mediaAssetRepository.GetByIdAsync(
                request.MediaId,
                cancellationToken);

            if (mediaAsset is null)
            {
                return Result<UpdateMediaAssetResponse>.Failure(
                    MediaErrors.MediaAsset.NotFound);
            }

            if (mediaAsset.IsDeleted)
            {
                return Result<UpdateMediaAssetResponse>.Failure(
                    MediaErrors.MediaAsset.Deleted);
            }

            // Domain validation for safe metadata update.
            mediaAsset.UpdateSafeMetadata(
                altText: request.AltText,
                metadataJson: request.MetadataJson,
                nowUtc: nowUtc,
                actorUserId: actorUserId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                MediaAssetMutationResult updateResult =
                    await _mediaAssetRepository.UpdateMetadataAsync(
                        new UpdateMediaMetadataCommand(
                            MediaId: request.MediaId,
                            AltText: mediaAsset.AltText,
                            MetadataJson: mediaAsset.MetadataJson,
                            UpdatedBy: actorUserId),
                        cancellationToken);

                if (!updateResult.Succeeded)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<UpdateMediaAssetResponse>.Failure(
                        MapMutationResultCode(updateResult.ResultCode));
                }

                int newVersion = updateResult.NewVersion
                    ?? throw new InvalidOperationException(
                        "Media_MediaAsset_UpdateMetadata did not return NewVersion.");

                await _mediaOutboxWriter.EnqueueMediaAssetUpdatedAsync(
                    unitOfWork: _unitOfWork,
                    mediaId: mediaAsset.MediaId,
                    mediaPublicId: mediaAsset.PublicId,
                    altText: mediaAsset.AltText,
                    metadataJson: mediaAsset.MetadataJson,
                    actorUserId: actorUserId.Value,
                    version: newVersion,
                    updatedAtUtc: nowUtc,
                    correlationId: _requestContext.CorrelationId,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<UpdateMediaAssetResponse>.Success(
                    new UpdateMediaAssetResponse
                    {
                        MediaId = mediaAsset.MediaId,
                        PublicId = mediaAsset.PublicId,
                        AltText = mediaAsset.AltText,
                        MetadataJson = mediaAsset.MetadataJson,
                        UpdatedAt = nowUtc,
                        UpdatedBy = actorUserId,
                        Version = newVersion
                    });
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (PersistenceException exception)
        {
            return Result<UpdateMediaAssetResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<UpdateMediaAssetResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapMutationResultCode(int resultCode)
    {
        return resultCode switch
        {
            1 => MediaErrors.MediaAsset.NotFound,
            2 => MediaErrors.MediaAsset.Deleted,
            3 => MediaErrors.VersionConflict,
            _ => MediaErrors.PersistenceError
        };
    }

    private static Error MapDomainException(MediaDomainException exception)
    {
        return exception.Code switch
        {
            "MEDIA.MEDIA_DELETED" =>
                MediaErrors.MediaAsset.Deleted,

            "MEDIA.MEDIA_ASSET_ALT_TEXT_TOO_LONG" =>
                MediaErrors.MediaAsset.AltTextTooLong,

            _ => MediaErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "MEDIA.MEDIA_NOT_FOUND" =>
                MediaErrors.MediaAsset.NotFound,

            "MEDIA.MEDIA_DELETED" =>
                MediaErrors.MediaAsset.Deleted,

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