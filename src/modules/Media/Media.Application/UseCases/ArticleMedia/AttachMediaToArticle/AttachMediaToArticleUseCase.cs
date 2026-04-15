using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.ArticleMedia.Requests;
using Media.Application.Contracts.ArticleMedia.Responses;
using Media.Application.Errors;
using Media.Application.Ports.Persistence;
using Media.Domain.Exceptions;

namespace Media.Application.UseCases.ArticleMedia.AttachMediaToArticle;

public sealed class AttachMediaToArticleUseCase : IAttachMediaToArticleUseCase
{
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IArticleMediaRepository _articleMediaRepository;
    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly IRequestContext _requestContext;

    public AttachMediaToArticleUseCase(
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

    public async Task<Result<AttachMediaToArticleResponse>> ExecuteAsync(
        AttachMediaToArticleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.ArticleId <= 0)
            {
                return Result<AttachMediaToArticleResponse>.Failure(MediaErrors.ArticleMedia.InvalidArticleId);
            }

            if (request.MediaId <= 0)
            {
                return Result<AttachMediaToArticleResponse>.Failure(MediaErrors.ArticleMedia.InvalidMediaId);
            }

            long? actorUserId = request.ActorUserId ?? _requestContext.CurrentUserId;

            var mediaAsset = await _mediaAssetRepository.GetByIdAsync(
                request.MediaId,
                cancellationToken);

            if (mediaAsset is null)
            {
                return Result<AttachMediaToArticleResponse>.Failure(MediaErrors.MediaAsset.NotFound);
            }

            if (mediaAsset.IsDeleted)
            {
                return Result<AttachMediaToArticleResponse>.Failure(MediaErrors.MediaAsset.AlreadyDeleted);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                (long? articleMediaId, int affectedRows) = await _articleMediaRepository.AttachAsync(
                    request.ArticleId,
                    request.MediaId,
                    request.IsPrimary,
                    actorUserId,
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<AttachMediaToArticleResponse>.Success(new AttachMediaToArticleResponse
                {
                    ArticleMediaId = articleMediaId,
                    ArticleId = request.ArticleId,
                    MediaId = request.MediaId,
                    Attached = articleMediaId.HasValue || affectedRows >= 0,
                    IsPrimary = request.IsPrimary,
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
            return Result<AttachMediaToArticleResponse>.Failure(MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<AttachMediaToArticleResponse>.Failure(MapDomainException(exception));
        }
    }

    private static Error MapDomainException(MediaDomainException exception)
    {
        return exception.Code switch
        {
            "MEDIA.ARTICLE_MEDIA_INVALID_ARTICLE_ID" => MediaErrors.ArticleMedia.InvalidArticleId,
            "MEDIA.ARTICLE_MEDIA_INVALID_MEDIA_ID" => MediaErrors.ArticleMedia.InvalidMediaId,
            "MEDIA.ARTICLE_MEDIA_ALREADY_DELETED" => MediaErrors.ArticleMedia.AlreadyDeleted,
            "MEDIA.PRIMARY_CONSTRAINT_VIOLATION" => MediaErrors.ArticleMedia.PrimaryConstraintViolation,
            "MEDIA.MEDIA_ASSET_ALREADY_DELETED" => MediaErrors.MediaAsset.AlreadyDeleted,
            _ => MediaErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "MEDIA.ATTACHMENT_ALREADY_EXISTS" => MediaErrors.ArticleMedia.AlreadyExists,
            "MEDIA.PRIMARY_CONSTRAINT_VIOLATION" => MediaErrors.ArticleMedia.PrimaryConstraintViolation,
            _ => MediaErrors.ValidationFailed
        };
    }
}