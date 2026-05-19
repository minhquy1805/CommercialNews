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

namespace Media.Application.UseCases.MediaAssets.RestoreMedia;

public sealed class RestoreMediaUseCase : IRestoreMediaUseCase
{
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly IMediaOutboxWriter _mediaOutboxWriter;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContext _requestContext;

    public RestoreMediaUseCase(
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

    public async Task<Result<RestoreMediaResponse>> ExecuteAsync(
        RestoreMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            if (request.MediaId <= 0)
            {
                return Result<RestoreMediaResponse>.Failure(
                    MediaErrors.MediaAsset.InvalidMediaId);
            }

            DateTime nowUtc = _dateTimeProvider.UtcNow;
            long? actorUserId = _requestContext.CurrentUserId;

            if (actorUserId is null or <= 0)
            {
                return Result<RestoreMediaResponse>.Failure(
                    MediaErrors.Actor.NotFound);
            }

            MediaAsset? mediaAsset = await _mediaAssetRepository.GetByIdAsync(
                request.MediaId,
                cancellationToken);

            if (mediaAsset is null)
            {
                return Result<RestoreMediaResponse>.Failure(
                    MediaErrors.MediaAsset.NotFound);
            }

            if (mediaAsset.RestoreUntil.HasValue &&
                mediaAsset.RestoreUntil.Value < nowUtc)
            {
                return Result<RestoreMediaResponse>.Failure(
                    MediaErrors.MediaAsset.RestoreWindowExpired);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                MediaAssetMutationResult restoreResult =
                    await _mediaAssetRepository.RestoreAsync(
                        new RestoreMediaAssetCommand(
                            MediaId: request.MediaId,
                            RestoredBy: actorUserId),
                        cancellationToken);

                if (!restoreResult.Succeeded)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<RestoreMediaResponse>.Failure(
                        MapMutationResultCode(restoreResult.ResultCode));
                }

                int newVersion = restoreResult.NewVersion
                    ?? mediaAsset.Version;

                if (restoreResult.AffectedRows > 0)
                {
                    await _mediaOutboxWriter.EnqueueMediaAssetRestoredAsync(
                        unitOfWork: _unitOfWork,
                        mediaId: mediaAsset.MediaId,
                        mediaPublicId: mediaAsset.PublicId,
                        isDeleted: false,
                        actorUserId: actorUserId.Value,
                        version: newVersion,
                        restoredAtUtc: nowUtc,
                        correlationId: _requestContext.CorrelationId,
                        cancellationToken: cancellationToken);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<RestoreMediaResponse>.Success(
                    new RestoreMediaResponse
                    {
                        MediaId = mediaAsset.MediaId,
                        PublicId = mediaAsset.PublicId,
                        IsRestored = restoreResult.AffectedRows > 0,
                        IsDeleted = false,
                        RestoredAt = nowUtc,
                        RestoredBy = actorUserId,
                        AffectedRows = restoreResult.AffectedRows,
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
            return Result<RestoreMediaResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<RestoreMediaResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapMutationResultCode(int resultCode)
    {
        return resultCode switch
        {
            1 => MediaErrors.MediaAsset.NotFound,
            2 => MediaErrors.MediaAsset.NotDeleted,
            3 => MediaErrors.MediaAsset.RestoreWindowExpired,
            _ => MediaErrors.PersistenceError
        };
    }

    private static Error MapDomainException(MediaDomainException exception)
    {
        return exception.Code switch
        {
            "MEDIA.MEDIA_ASSET_INVALID_MEDIA_ID" =>
                MediaErrors.MediaAsset.InvalidMediaId,

            "MEDIA.MEDIA_ASSET_NOT_DELETED" =>
                MediaErrors.MediaAsset.NotDeleted,

            "MEDIA.RESTORE_WINDOW_EXPIRED" =>
                MediaErrors.MediaAsset.RestoreWindowExpired,

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