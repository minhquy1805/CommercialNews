using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.ArticleMedia.Requests;
using Media.Application.Contracts.ArticleMedia.Responses;
using Media.Application.Errors;
using Media.Application.Ports.Persistence;
using Media.Domain.Exceptions;

namespace Media.Application.UseCases.ArticleMedia.RestoreArticleMedia;

public sealed class RestoreArticleMediaUseCase : IRestoreArticleMediaUseCase
{
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IArticleMediaRepository _articleMediaRepository;
    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly IRequestContext _requestContext;

    public RestoreArticleMediaUseCase(
        IMediaAssetRepository mediaAssetRepository,
        IArticleMediaRepository articleMediaRepository,
        IMediaUnitOfWork unitOfWork,
        IRequestContext requestContext)
    {
        _mediaAssetRepository = mediaAssetRepository;
        _articleMediaRepository = articleMediaRepository;
        _unitOfWork = unitOfWork;
        _requestContext = requestContext;
    }

    public async Task<Result<RestoreArticleMediaResponse>> ExecuteAsync(
        RestoreArticleMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.ArticleId <= 0)
            {
                return Result<RestoreArticleMediaResponse>.Failure(MediaErrors.ArticleMedia.InvalidArticleId);
            }

            if (request.MediaId <= 0)
            {
                return Result<RestoreArticleMediaResponse>.Failure(MediaErrors.ArticleMedia.InvalidMediaId);
            }

            long? actorUserId = request.ActorUserId ?? _requestContext.CurrentUserId;

            var mediaAsset = await _mediaAssetRepository.GetByIdAsync(
                request.MediaId,
                cancellationToken);

            if (mediaAsset is null)
            {
                return Result<RestoreArticleMediaResponse>.Failure(MediaErrors.MediaAsset.NotFound);
            }

            if (mediaAsset.IsDeleted)
            {
                return Result<RestoreArticleMediaResponse>.Failure(MediaErrors.MediaAsset.AlreadyDeleted);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                int affectedRows = await _articleMediaRepository.RestoreAsync(
                    request.ArticleId,
                    request.MediaId,
                    actorUserId,
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                if (affectedRows <= 0)
                {
                    return Result<RestoreArticleMediaResponse>.Failure(MediaErrors.ArticleMedia.NotDeleted);
                }

                return Result<RestoreArticleMediaResponse>.Success(new RestoreArticleMediaResponse
                {
                    ArticleId = request.ArticleId,
                    MediaId = request.MediaId,
                    Restored = affectedRows > 0,
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
            return Result<RestoreArticleMediaResponse>.Failure(MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<RestoreArticleMediaResponse>.Failure(MapDomainException(exception));
        }
    }

    private static Error MapDomainException(MediaDomainException exception)
    {
        return exception.Code switch
        {
            "MEDIA.ARTICLE_MEDIA_INVALID_ARTICLE_ID" => MediaErrors.ArticleMedia.InvalidArticleId,
            "MEDIA.ARTICLE_MEDIA_INVALID_MEDIA_ID" => MediaErrors.ArticleMedia.InvalidMediaId,
            "MEDIA.ARTICLE_MEDIA_NOT_DELETED" => MediaErrors.ArticleMedia.NotDeleted,
            "MEDIA.MEDIA_ASSET_ALREADY_DELETED" => MediaErrors.MediaAsset.AlreadyDeleted,
            _ => MediaErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "MEDIA.ATTACHMENT_NOT_FOUND" => MediaErrors.ArticleMedia.NotFound,
            _ => MediaErrors.ValidationFailed
        };
    }
}