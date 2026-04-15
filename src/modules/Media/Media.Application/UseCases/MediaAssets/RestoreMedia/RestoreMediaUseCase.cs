using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.MediaAsset.Requests;
using Media.Application.Contracts.MediaAsset.Responses;
using Media.Application.Errors;
using Media.Application.Ports.Persistence;
using Media.Domain.Exceptions;

namespace Media.Application.UseCases.MediaAssets.RestoreMedia;

public sealed class RestoreMediaUseCase : IRestoreMediaUseCase
{
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly IRequestContext _requestContext;

    public RestoreMediaUseCase(
        IMediaAssetRepository mediaAssetRepository,
        IMediaUnitOfWork unitOfWork,
        IRequestContext requestContext)
    {
        _mediaAssetRepository = mediaAssetRepository;
        _unitOfWork = unitOfWork;
        _requestContext = requestContext;
    }

    public async Task<Result<RestoreMediaResponse>> ExecuteAsync(
        RestoreMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.MediaId <= 0)
            {
                return Result<RestoreMediaResponse>.Failure(MediaErrors.MediaAsset.InvalidMediaId);
            }

            long? actorUserId = request.ActorUserId ?? _requestContext.CurrentUserId;

            var existingMedia = await _mediaAssetRepository.GetByIdAsync(
                request.MediaId,
                cancellationToken);

            if (existingMedia is null)
            {
                return Result<RestoreMediaResponse>.Failure(MediaErrors.MediaAsset.NotFound);
            }

            if (!existingMedia.IsDeleted)
            {
                return Result<RestoreMediaResponse>.Failure(MediaErrors.MediaAsset.NotDeleted);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                int affectedRows = await _mediaAssetRepository.RestoreAsync(
                    request.MediaId,
                    actorUserId,
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                if (affectedRows <= 0)
                {
                    return Result<RestoreMediaResponse>.Failure(MediaErrors.MediaAsset.RestoreWindowExpired);
                }

                return Result<RestoreMediaResponse>.Success(new RestoreMediaResponse
                {
                    MediaId = request.MediaId,
                    IsRestored = affectedRows > 0,
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
            return Result<RestoreMediaResponse>.Failure(MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<RestoreMediaResponse>.Failure(MapDomainException(exception));
        }
    }

    private static Error MapDomainException(MediaDomainException exception)
    {
        return exception.Code switch
        {
            "MEDIA.MEDIA_ASSET_INVALID_MEDIA_ID" => MediaErrors.MediaAsset.InvalidMediaId,
            "MEDIA.MEDIA_ASSET_NOT_DELETED" => MediaErrors.MediaAsset.NotDeleted,
            "MEDIA.RESTORE_WINDOW_EXPIRED" => MediaErrors.MediaAsset.RestoreWindowExpired,
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