using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Media.Application.Contracts.ArticleMedia.Requests;
using Media.Application.Contracts.ArticleMedia.Responses;
using Media.Application.Errors;
using Media.Application.Models.Commands;
using Media.Application.Models.Results;
using Media.Application.Ports.Persistence;
using Media.Application.Ports.Services;
using Media.Domain.Exceptions;

namespace Media.Application.UseCases.ArticleMedia.AttachMediaToArticle;

public sealed class AttachMediaToArticleUseCase : IAttachMediaToArticleUseCase
{
    private readonly IArticleMediaRepository _articleMediaRepository;
    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly IMediaOutboxWriter _mediaOutboxWriter;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContext _requestContext;

    public AttachMediaToArticleUseCase(
        IArticleMediaRepository articleMediaRepository,
        IMediaUnitOfWork unitOfWork,
        IMediaOutboxWriter mediaOutboxWriter,
        IDateTimeProvider dateTimeProvider,
        IRequestContext requestContext)
    {
        _articleMediaRepository = articleMediaRepository
            ?? throw new ArgumentNullException(nameof(articleMediaRepository));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _mediaOutboxWriter = mediaOutboxWriter
            ?? throw new ArgumentNullException(nameof(mediaOutboxWriter));

        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));

        _requestContext = requestContext
            ?? throw new ArgumentNullException(nameof(requestContext));
    }

    public async Task<Result<AttachMediaToArticleResponse>> ExecuteAsync(
        AttachMediaToArticleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            if (request.ArticleId <= 0)
            {
                return Result<AttachMediaToArticleResponse>.Failure(
                    MediaErrors.ArticleMedia.InvalidArticleId);
            }

            if (request.MediaId <= 0)
            {
                return Result<AttachMediaToArticleResponse>.Failure(
                    MediaErrors.ArticleMedia.InvalidMediaId);
            }

            DateTime nowUtc = _dateTimeProvider.UtcNow;
            long? actorUserId = _requestContext.CurrentUserId;

            if (actorUserId is null or <= 0)
            {
                return Result<AttachMediaToArticleResponse>.Failure(
                    MediaErrors.Actor.NotFound);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                ArticleMediaAttachResult attachResult =
                    await _articleMediaRepository.AttachAsync(
                        new AttachArticleMediaCommand(
                            ArticleId: request.ArticleId,
                            MediaId: request.MediaId,
                            IsPrimary: request.IsPrimary,
                            CreatedBy: actorUserId),
                        cancellationToken);

                if (!attachResult.Succeeded)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<AttachMediaToArticleResponse>.Failure(
                        MapMutationResultCode(attachResult.ResultCode));
                }

                int attachmentSetVersion = attachResult.NewVersion
                    ?? throw new InvalidOperationException(
                        "Media_ArticleMedia_Attach did not return NewVersion.");

                if (attachResult.AffectedRows > 0)
                {
                    await _mediaOutboxWriter.EnqueueArticleMediaAttachedAsync(
                        unitOfWork: _unitOfWork,
                        articleId: request.ArticleId,
                        mediaId: request.MediaId,
                        articleMediaId: attachResult.ArticleMediaId,
                        isPrimary: request.IsPrimary,
                        primaryChanged: attachResult.PrimaryChanged,
                        actorUserId: actorUserId.Value,
                        attachmentSetVersion: attachmentSetVersion,
                        attachedAtUtc: nowUtc,
                        correlationId: _requestContext.CorrelationId,
                        cancellationToken: cancellationToken);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<AttachMediaToArticleResponse>.Success(
                    new AttachMediaToArticleResponse
                    {
                        ArticleMediaId = attachResult.ArticleMediaId,
                        ArticleId = request.ArticleId,
                        MediaId = request.MediaId,
                        Attached = attachResult.AffectedRows > 0,
                        IsPrimary = request.IsPrimary,
                        PrimaryChanged = attachResult.PrimaryChanged,
                        AffectedRows = attachResult.AffectedRows,
                        AttachmentSetVersion = attachmentSetVersion
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
            return Result<AttachMediaToArticleResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<AttachMediaToArticleResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapMutationResultCode(int resultCode)
    {
        return resultCode switch
        {
            1 => MediaErrors.MediaAsset.NotFound,
            2 => MediaErrors.MediaAsset.Deleted,
            5 => MediaErrors.ArticleMedia.AlreadyExists,
            _ => MediaErrors.PersistenceError
        };
    }

    private static Error MapDomainException(MediaDomainException exception)
    {
        return exception.Code switch
        {
            "MEDIA.ARTICLE_MEDIA_INVALID_ARTICLE_ID" =>
                MediaErrors.ArticleMedia.InvalidArticleId,

            "MEDIA.ARTICLE_MEDIA_INVALID_MEDIA_ID" =>
                MediaErrors.ArticleMedia.InvalidMediaId,

            "MEDIA.PRIMARY_CONSTRAINT_VIOLATION" =>
                MediaErrors.ArticleMedia.PrimaryConstraintViolation,

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

            "MEDIA.ARTICLE_NOT_FOUND" =>
                MediaErrors.Article.NotFound,

            "MEDIA.ATTACHMENT_ALREADY_EXISTS" =>
                MediaErrors.ArticleMedia.AlreadyExists,

            "MEDIA.PRIMARY_CONSTRAINT_VIOLATION" =>
                MediaErrors.ArticleMedia.PrimaryConstraintViolation,

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