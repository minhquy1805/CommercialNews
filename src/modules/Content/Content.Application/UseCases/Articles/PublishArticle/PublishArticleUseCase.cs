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

namespace Content.Application.UseCases.Articles.PublishArticle;

public sealed class PublishArticleUseCase : IPublishArticleUseCase
{
    private readonly IArticleRepository _articleRepository;
    private readonly IArticleLifecycleEventRepository _articleLifecycleEventRepository;
    private readonly IContentUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContext _requestContext;

    public PublishArticleUseCase(
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

    public async Task<Result<PublishArticleResponseDto>> ExecuteAsync(
        PublishArticleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.ArticleId <= 0)
        {
            return Result<PublishArticleResponseDto>.Failure(
                ContentErrors.Article.InvalidArticleId);
        }

        if (request.ExpectedVersion <= 0)
        {
            return Result<PublishArticleResponseDto>.Failure(
                ContentErrors.Article.InvalidVersion);
        }

        Article? article = await _articleRepository.GetByIdAsync(
            request.ArticleId,
            cancellationToken);

        if (article is null)
        {
            return Result<PublishArticleResponseDto>.Failure(
                ContentErrors.Article.NotFound);
        }

        if (article.Version != request.ExpectedVersion)
        {
            return Result<PublishArticleResponseDto>.Failure(
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
            article.Publish(
                nowUtc: nowUtc,
                actorUserId: actorUserId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            Article? publishedArticle = await _articleRepository.PublishAsync(
                articleId: request.ArticleId,
                actorUserId: actorUserId,
                expectedVersion: request.ExpectedVersion,
                cancellationToken: cancellationToken);

            if (publishedArticle is null)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);

                return Result<PublishArticleResponseDto>.Failure(
                    ContentErrors.ConcurrencyConflict);
            }

            ArticleLifecycleEvent lifecycleEvent = ArticleLifecycleEvent.Create(
                articleId: publishedArticle.ArticleId,
                articleVersion: publishedArticle.Version,
                actionType: ArticleLifecycleActionTypes.Publish,
                fromStatus: fromStatus,
                toStatus: publishedArticle.Status,
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

                return Result<PublishArticleResponseDto>.Failure(
                    ContentErrors.WriteCommitFailed);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<PublishArticleResponseDto>.Success(
                new PublishArticleResponseDto
                {
                    ArticleId = publishedArticle.ArticleId,
                    ArticlePublicId = publishedArticle.ArticlePublicId,
                    Status = publishedArticle.Status,
                    PublishedAt = publishedArticle.PublishedAt,
                    Version = publishedArticle.Version,
                    UpdatedAt = publishedArticle.UpdatedAt
                });
        }
        catch (PersistenceException exception)
        {
            await RollbackIfNeededAsync(cancellationToken);

            return Result<PublishArticleResponseDto>.Failure(
                MapPersistenceException(exception));
        }
        catch (ContentDomainException exception)
        {
            await RollbackIfNeededAsync(cancellationToken);

            return Result<PublishArticleResponseDto>.Failure(
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

            "CONTENT.ARTICLE_ALREADY_PUBLISHED" => ContentErrors.Article.AlreadyPublished,
            "CONTENT.ARTICLE_ALREADY_ARCHIVED" => ContentErrors.Article.AlreadyArchived,
            "CONTENT.ARTICLE_ALREADY_DELETED" => ContentErrors.Article.AlreadyDeleted,
            "CONTENT.ARTICLE_ALREADY_SOFT_DELETED" => ContentErrors.Article.AlreadySoftDeleted,

            "CONTENT.ARTICLE_TITLE_REQUIRED" => ContentErrors.Article.TitleRequired,
            "CONTENT.ARTICLE_SUMMARY_REQUIRED" => ContentErrors.Article.SummaryRequired,
            "CONTENT.ARTICLE_BODY_REQUIRED" => ContentErrors.Article.BodyRequired,

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

            "CONTENT.ARTICLE_ALREADY_SOFT_DELETED" => ContentErrors.Article.AlreadySoftDeleted,
            "CONTENT.ARTICLE_ALREADY_ARCHIVED" => ContentErrors.Article.AlreadyArchived,
            "CONTENT.ARTICLE_NOT_PUBLISHABLE" => ContentErrors.Article.NotPublishable,
            "CONTENT.ARTICLE_CATEGORY_INACTIVE_OR_DELETED" => ContentErrors.Category.InactiveOrDeleted,

            "CONTENT.ARTICLE_TITLE_REQUIRED" => ContentErrors.Article.TitleRequired,
            "CONTENT.ARTICLE_SUMMARY_REQUIRED" => ContentErrors.Article.SummaryRequired,
            "CONTENT.ARTICLE_BODY_REQUIRED" => ContentErrors.Article.BodyRequired,

            "CONTENT.ARTICLE_LIFECYCLE_EVENT_INVALID_ARTICLE_ID" => ContentErrors.LifecycleEvent.InvalidArticleId,
            "CONTENT.ARTICLE_LIFECYCLE_EVENT_INVALID_ARTICLE_VERSION" => ContentErrors.LifecycleEvent.InvalidArticleVersion,
            "CONTENT.ARTICLE_LIFECYCLE_EVENT_INVALID_ACTOR_USER_ID" => ContentErrors.LifecycleEvent.InvalidActorUserId,
            "CONTENT.ARTICLE_LIFECYCLE_EVENT_ACTION_TYPE_INVALID" => ContentErrors.LifecycleEvent.InvalidActionType,
            "CONTENT.ARTICLE_LIFECYCLE_EVENT_STATUS_INVALID" => ContentErrors.LifecycleEvent.InvalidStatus,

            _ => ContentErrors.WriteCommitFailed
        };
    }
}