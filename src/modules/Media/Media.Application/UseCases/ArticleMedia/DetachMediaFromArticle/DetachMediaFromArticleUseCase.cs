using CommercialNews.BuildingBlocks.Abstractions.Execution;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;
using Media.Application.Contracts.ArticleMedia.Requests;
using Media.Application.Contracts.ArticleMedia.Responses;
using Media.Application.Errors;
using Media.Application.Ports.Persistence;
using Media.Domain.Exceptions;

namespace Media.Application.UseCases.ArticleMedia.DetachMediaFromArticle;

public sealed class DetachMediaFromArticleUseCase : IDetachMediaFromArticleUseCase
{
    private readonly IArticleMediaRepository _articleMediaRepository;
    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly IRequestContext _requestContext;

    public DetachMediaFromArticleUseCase(
        IArticleMediaRepository articleMediaRepository,
        IMediaUnitOfWork unitOfWork,
        IRequestContext requestContext)
    {
        _articleMediaRepository = articleMediaRepository;
        _unitOfWork = unitOfWork;
        _requestContext = requestContext;
    }

    public async Task<Result<DetachMediaFromArticleResponse>> ExecuteAsync(
        DetachMediaFromArticleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.ArticleId <= 0)
            {
                return Result<DetachMediaFromArticleResponse>.Failure(MediaErrors.ArticleMedia.InvalidArticleId);
            }

            if (request.MediaId <= 0)
            {
                return Result<DetachMediaFromArticleResponse>.Failure(MediaErrors.ArticleMedia.InvalidMediaId);
            }

            long? actorUserId = request.ActorUserId ?? _requestContext.CurrentUserId;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                int affectedRows = await _articleMediaRepository.DetachAsync(
                    request.ArticleId,
                    request.MediaId,
                    actorUserId,
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                if (affectedRows <= 0)
                {
                    return Result<DetachMediaFromArticleResponse>.Failure(MediaErrors.ArticleMedia.NotFound);
                }

                return Result<DetachMediaFromArticleResponse>.Success(new DetachMediaFromArticleResponse
                {
                    ArticleId = request.ArticleId,
                    MediaId = request.MediaId,
                    Detached = affectedRows > 0,
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
            return Result<DetachMediaFromArticleResponse>.Failure(MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<DetachMediaFromArticleResponse>.Failure(MapDomainException(exception));
        }
    }

    private static Error MapDomainException(MediaDomainException exception)
    {
        return exception.Code switch
        {
            "MEDIA.ARTICLE_MEDIA_INVALID_ARTICLE_ID" => MediaErrors.ArticleMedia.InvalidArticleId,
            "MEDIA.ARTICLE_MEDIA_INVALID_MEDIA_ID" => MediaErrors.ArticleMedia.InvalidMediaId,
            "MEDIA.ARTICLE_MEDIA_ALREADY_DELETED" => MediaErrors.ArticleMedia.AlreadyDeleted,
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