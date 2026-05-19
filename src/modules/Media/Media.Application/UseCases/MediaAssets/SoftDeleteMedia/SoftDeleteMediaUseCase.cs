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

namespace Media.Application.UseCases.MediaAssets.SoftDeleteMedia;

public sealed class SoftDeleteMediaUseCase : ISoftDeleteMediaUseCase
{
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly IMediaOutboxWriter _mediaOutboxWriter;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContext _requestContext;

    public SoftDeleteMediaUseCase(
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

    public async Task<Result<SoftDeleteMediaResponse>> ExecuteAsync(
        SoftDeleteMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            if (request.MediaId <= 0)
            {
                return Result<SoftDeleteMediaResponse>.Failure(
                    MediaErrors.MediaAsset.InvalidMediaId);
            }

            DateTime nowUtc = _dateTimeProvider.UtcNow;
            long? actorUserId = _requestContext.CurrentUserId;

            if (actorUserId is null or <= 0)
            {
                return Result<SoftDeleteMediaResponse>.Failure(
                    MediaErrors.Actor.NotFound);
            }

            if (request.RestoreUntil.HasValue &&
                request.RestoreUntil.Value < nowUtc)
            {
                return Result<SoftDeleteMediaResponse>.Failure(
                    MediaErrors.MediaAsset.RestoreUntilInvalid);
            }

            MediaAsset? mediaAsset = await _mediaAssetRepository.GetByIdAsync(
                request.MediaId,
                cancellationToken);

            if (mediaAsset is null)
            {
                return Result<SoftDeleteMediaResponse>.Failure(
                    MediaErrors.MediaAsset.NotFound);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                MediaAssetMutationResult deleteResult =
                    await _mediaAssetRepository.SoftDeleteAsync(
                        new SoftDeleteMediaAssetCommand(
                            MediaId: request.MediaId,
                            DeletedBy: actorUserId,
                            RestoreUntil: request.RestoreUntil),
                        cancellationToken);

                if (!deleteResult.Succeeded)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<SoftDeleteMediaResponse>.Failure(
                        MapMutationResultCode(deleteResult.ResultCode));
                }

                int newVersion = deleteResult.NewVersion
                    ?? mediaAsset.Version;

                if (deleteResult.AffectedRows > 0)
                {
                    await _mediaOutboxWriter.EnqueueMediaAssetSoftDeletedAsync(
                        unitOfWork: _unitOfWork,
                        mediaId: mediaAsset.MediaId,
                        mediaPublicId: mediaAsset.PublicId,
                        isDeleted: true,
                        restoreUntil: request.RestoreUntil,
                        primaryClearedCount: deleteResult.PrimaryClearedCount,
                        actorUserId: actorUserId.Value,
                        version: newVersion,
                        deletedAtUtc: nowUtc,
                        correlationId: _requestContext.CorrelationId,
                        cancellationToken: cancellationToken);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<SoftDeleteMediaResponse>.Success(
                    new SoftDeleteMediaResponse
                    {
                        MediaId = mediaAsset.MediaId,
                        PublicId = mediaAsset.PublicId,
                        IsDeleted = true,
                        DeletedAt = nowUtc,
                        DeletedBy = actorUserId,
                        RestoreUntil = request.RestoreUntil,
                        AffectedRows = deleteResult.AffectedRows,
                        PrimaryClearedCount = deleteResult.PrimaryClearedCount,
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
            return Result<SoftDeleteMediaResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<SoftDeleteMediaResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapMutationResultCode(int resultCode)
    {
        return resultCode switch
        {
            1 => MediaErrors.MediaAsset.NotFound,
            2 => MediaErrors.MediaAsset.AlreadyDeleted,
            _ => MediaErrors.PersistenceError
        };
    }

    private static Error MapDomainException(MediaDomainException exception)
    {
        return exception.Code switch
        {
            "MEDIA.MEDIA_ASSET_INVALID_MEDIA_ID" =>
                MediaErrors.MediaAsset.InvalidMediaId,

            "MEDIA.MEDIA_ASSET_RESTORE_UNTIL_INVALID" =>
                MediaErrors.MediaAsset.RestoreUntilInvalid,

            "MEDIA.MEDIA_ASSET_ALREADY_DELETED" =>
                MediaErrors.MediaAsset.AlreadyDeleted,

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