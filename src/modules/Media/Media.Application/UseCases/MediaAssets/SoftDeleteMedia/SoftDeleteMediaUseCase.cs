using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Media.Application.Contracts.MediaAsset.Requests;
using Media.Application.Contracts.MediaAsset.Responses;
using Media.Application.Errors;
using Media.Application.Ports.Persistence;
using Media.Domain.Exceptions;

namespace Media.Application.UseCases.MediaAssets.SoftDeleteMedia;

public sealed class SoftDeleteMediaUseCase : ISoftDeleteMediaUseCase
{
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContext _requestContext;

    public SoftDeleteMediaUseCase(
        IMediaAssetRepository mediaAssetRepository,
        IMediaUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IRequestContext requestContext)
    {
        _mediaAssetRepository = mediaAssetRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _requestContext = requestContext;
    }

    public async Task<Result<SoftDeleteMediaResponse>> ExecuteAsync(
        SoftDeleteMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.MediaId <= 0)
            {
                return Result<SoftDeleteMediaResponse>.Failure(MediaErrors.MediaAsset.InvalidMediaId);
            }

            DateTime nowUtc = _dateTimeProvider.UtcNow;
            long? actorUserId = request.ActorUserId ?? _requestContext.CurrentUserId;

            if (request.RestoreUntil.HasValue && request.RestoreUntil.Value < nowUtc)
            {
                return Result<SoftDeleteMediaResponse>.Failure(MediaErrors.MediaAsset.RestoreUntilInvalid);
            }

            var existingMedia = await _mediaAssetRepository.GetByIdAsync(
                request.MediaId,
                cancellationToken);

            if (existingMedia is null)
            {
                return Result<SoftDeleteMediaResponse>.Failure(MediaErrors.MediaAsset.NotFound);
            }

            if (existingMedia.IsDeleted)
            {
                return Result<SoftDeleteMediaResponse>.Failure(MediaErrors.MediaAsset.AlreadyDeleted);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                int affectedRows = await _mediaAssetRepository.SoftDeleteAsync(
                    request.MediaId,
                    actorUserId,
                    request.RestoreUntil,
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<SoftDeleteMediaResponse>.Success(new SoftDeleteMediaResponse
                {
                    MediaId = request.MediaId,
                    IsDeleted = affectedRows > 0,
                    RestoreUntil = request.RestoreUntil,
                    AffectedRows = affectedRows
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
            return Result<SoftDeleteMediaResponse>.Failure(MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<SoftDeleteMediaResponse>.Failure(MapDomainException(exception));
        }
    }

    private static Error MapDomainException(MediaDomainException exception)
    {
        return exception.Code switch
        {
            "MEDIA.MEDIA_ASSET_INVALID_MEDIA_ID" => MediaErrors.MediaAsset.InvalidMediaId,
            "MEDIA.MEDIA_ASSET_ALREADY_DELETED" => MediaErrors.MediaAsset.AlreadyDeleted,
            "MEDIA.MEDIA_ASSET_RESTORE_UNTIL_INVALID" => MediaErrors.MediaAsset.RestoreUntilInvalid,
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