using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;
using Content.Domain.Constants;
using Content.Domain.Entities;
using Content.Domain.Exceptions;

namespace Content.Application.UseCases.Articles.ArchiveArticle;

public sealed class ArchiveArticleUseCase : IArchiveArticleUseCase
{
    private readonly IArticleRepository _articleRepository;
    private readonly IArticleLifecycleEventRepository _articleLifecycleEventRepository;
    private readonly IContentUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContext _requestContext;

    public ArchiveArticleUseCase(
        IArticleRepository articleRepository,
        IArticleLifecycleEventRepository articleLifecycleEventRepository,
        IContentUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IRequestContext requestContext)
    {
        _articleRepository = articleRepository ?? throw new ArgumentNullException(nameof(articleRepository));
        _articleLifecycleEventRepository = articleLifecycleEventRepository
            ?? throw new ArgumentNullException(nameof(articleLifecycleEventRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
    }

    public async Task<Result<ArchiveArticleResponseDto>> ExecuteAsync(
        ArchiveArticleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.ArticleId <= 0)
        {
            return Result<ArchiveArticleResponseDto>.Failure(
                ContentErrors.Article.InvalidArticleId);
        }

        if (request.ExpectedVersion <= 0)
        {
            return Result<ArchiveArticleResponseDto>.Failure(
                ContentErrors.Article.InvalidVersion);
        }

        Article? article = await _articleRepository.GetByIdAsync(
            request.ArticleId,
            cancellationToken);

        if (article is null)
        {
            return Result<ArchiveArticleResponseDto>.Failure(
                ContentErrors.Article.NotFound);
        }

        if (article.Version != request.ExpectedVersion)
        {
            return Result<ArchiveArticleResponseDto>.Failure(
                ContentErrors.ConcurrencyConflict);
        }

        DateTime nowUtc = _dateTimeProvider.UtcNow;

        long actorUserId =
            request.ActorUserId
            ?? _requestContext.CurrentUserId
            ?? article.AuthorUserId;

        string fromStatus = article.Status;

        try
        {
            article.Archive(
                nowUtc: nowUtc,
                actorUserId: actorUserId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            Article? archivedArticle = await _articleRepository.ArchiveAsync(
                articleId: request.ArticleId,
                actorUserId: actorUserId,
                expectedVersion: request.ExpectedVersion,
                cancellationToken: cancellationToken);

            if (archivedArticle is null)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);

                return Result<ArchiveArticleResponseDto>.Failure(
                    ContentErrors.ConcurrencyConflict);
            }

            ArticleLifecycleEvent lifecycleEvent = ArticleLifecycleEvent.Create(
                articleId: archivedArticle.ArticleId,
                articleVersion: archivedArticle.Version,
                actionType: ArticleLifecycleActionTypes.Archive,
                fromStatus: fromStatus,
                toStatus: archivedArticle.Status,
                reason: null,
                actorUserId: actorUserId,
                occurredAt: nowUtc,
                correlationId: _requestContext.CorrelationId,
                metadataJson: null);

            ArticleLifecycleEvent? createdLifecycleEvent =
                await _articleLifecycleEventRepository.InsertAsync(
                    lifecycleEvent,
                    cancellationToken);

            if (createdLifecycleEvent is null)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);

                return Result<ArchiveArticleResponseDto>.Failure(
                    ContentErrors.WriteCommitFailed);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<ArchiveArticleResponseDto>.Success(
                new ArchiveArticleResponseDto
                {
                    ArticleId = archivedArticle.ArticleId,
                    ArticlePublicId = archivedArticle.ArticlePublicId,
                    Status = archivedArticle.Status,
                    ArchivedAt = archivedArticle.ArchivedAt,
                    Version = archivedArticle.Version,
                    UpdatedAt = archivedArticle.UpdatedAt
                });
        }
        catch (PersistenceException exception)
        {
            await RollbackIfNeededAsync(cancellationToken);

            return Result<ArchiveArticleResponseDto>.Failure(
                MapPersistenceException(exception));
        }
        catch (ContentDomainException exception)
        {
            await RollbackIfNeededAsync(cancellationToken);

            return Result<ArchiveArticleResponseDto>.Failure(
                MapDomainException(exception));
        }
    }

    private async Task RollbackIfNeededAsync(CancellationToken cancellationToken)
    {
        if (_unitOfWork.HasActiveTransaction)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
        }
    }

    private static Error MapDomainException(ContentDomainException exception)
    {
        return exception.Code switch
        {
            "CONTENT.ARTICLE_ACTOR_USER_ID_INVALID" => ContentErrors.Article.ActorUserIdInvalid,
            "CONTENT.ARTICLE_INVALID_ARTICLE_ID" => ContentErrors.Article.InvalidArticleId,
            "CONTENT.ARTICLE_INVALID_VERSION" => ContentErrors.Article.InvalidVersion,

            "CONTENT.ARTICLE_ALREADY_ARCHIVED" => ContentErrors.Article.AlreadyArchived,
            "CONTENT.ARTICLE_ALREADY_DELETED" => ContentErrors.Article.AlreadyDeleted,
            "CONTENT.ARTICLE_ALREADY_SOFT_DELETED" => ContentErrors.Article.AlreadySoftDeleted,

            "CONTENT.INVALID_STATE_TRANSITION" => ContentErrors.InvalidStateTransition,

            "CONTENT.ARTICLE_LIFECYCLE_EVENT_INVALID_ACTOR_USER_ID" => ContentErrors.LifecycleEvent.InvalidActorUserId,
            "CONTENT.ARTICLE_LIFECYCLE_EVENT_INVALID_ARTICLE_ID" => ContentErrors.LifecycleEvent.InvalidArticleId,
            "CONTENT.ARTICLE_LIFECYCLE_EVENT_INVALID_ARTICLE_VERSION" => ContentErrors.LifecycleEvent.InvalidArticleVersion,
            "CONTENT.ARTICLE_LIFECYCLE_EVENT_ACTION_TYPE_INVALID" => ContentErrors.LifecycleEvent.InvalidActionType,
            "CONTENT.ARTICLE_LIFECYCLE_EVENT_STATUS_INVALID" => ContentErrors.LifecycleEvent.InvalidStatus,

            _ => ContentErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "CONTENT.CONCURRENCY_CONFLICT" => ContentErrors.ConcurrencyConflict,

            "CONTENT.ARTICLE_INVALID_ARTICLE_ID" => ContentErrors.Article.InvalidArticleId,
            "CONTENT.ARTICLE_INVALID_VERSION" => ContentErrors.Article.InvalidVersion,

            "CONTENT.ARTICLE_ALREADY_ARCHIVED" => ContentErrors.Article.AlreadyArchived,
            "CONTENT.ARTICLE_ALREADY_SOFT_DELETED" => ContentErrors.Article.AlreadySoftDeleted,

            "CONTENT.ARTICLE_LIFECYCLE_EVENT_INVALID_ARTICLE_ID" => ContentErrors.LifecycleEvent.InvalidArticleId,
            "CONTENT.ARTICLE_LIFECYCLE_EVENT_INVALID_ARTICLE_VERSION" => ContentErrors.LifecycleEvent.InvalidArticleVersion,
            "CONTENT.ARTICLE_LIFECYCLE_EVENT_INVALID_ACTOR_USER_ID" => ContentErrors.LifecycleEvent.InvalidActorUserId,
            "CONTENT.ARTICLE_LIFECYCLE_EVENT_ACTION_TYPE_INVALID" => ContentErrors.LifecycleEvent.InvalidActionType,
            "CONTENT.ARTICLE_LIFECYCLE_EVENT_STATUS_INVALID" => ContentErrors.LifecycleEvent.InvalidStatus,

            _ => ContentErrors.WriteCommitFailed
        };
    }
}