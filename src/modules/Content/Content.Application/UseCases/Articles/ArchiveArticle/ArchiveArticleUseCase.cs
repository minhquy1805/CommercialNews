using CommercialNews.BuildingBlocks.Abstractions.Execution;
using CommercialNews.BuildingBlocks.Abstractions.Time;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;
using Content.Domain.Entities;
using Content.Domain.Exceptions;

namespace Content.Application.UseCases.Articles.ArchiveArticle
{
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
            _articleRepository = articleRepository;
            _articleLifecycleEventRepository = articleLifecycleEventRepository;
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
            _requestContext = requestContext;
        }

        public async Task<Result<ArchiveArticleResponseDto>> ExecuteAsync(
            ArchiveArticleRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request.ArticleId <= 0)
            {
                return Result<ArchiveArticleResponseDto>.Failure(ContentErrors.Article.InvalidArticleId);
            }

            if (request.ExpectedVersion <= 0)
            {
                return Result<ArchiveArticleResponseDto>.Failure(ContentErrors.Article.InvalidVersion);
            }

            try
            {
                Article? article = await _articleRepository.GetByIdAsync(
                    request.ArticleId,
                    cancellationToken);

                if (article is null)
                {
                    return Result<ArchiveArticleResponseDto>.Failure(ContentErrors.Article.NotFound);
                }

                DateTime nowUtc = _dateTimeProvider.UtcNow;
                long? actorUserId = _requestContext.CurrentUserId;
                string fromStatus = article.Status;

                article.Archive(
                    nowUtc: nowUtc,
                    actorUserId: actorUserId);

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    Article? updatedArticle = await _articleRepository.ArchiveAsync(
                        articleId: request.ArticleId,
                        actorUserId: actorUserId,
                        expectedVersion: request.ExpectedVersion,
                        cancellationToken: cancellationToken);

                    if (updatedArticle is null)
                    {
                        await _unitOfWork.RollbackAsync(cancellationToken);
                        return Result<ArchiveArticleResponseDto>.Failure(ContentErrors.ConcurrencyConflict);
                    }

                    await _articleLifecycleEventRepository.InsertAsync(
                        articleId: updatedArticle.ArticleId,
                        actionType: "Archive",
                        fromStatus: fromStatus,
                        toStatus: updatedArticle.Status,
                        reason: null,
                        occurredAt: nowUtc,
                        actorUserId: actorUserId,
                        cancellationToken: cancellationToken);

                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result<ArchiveArticleResponseDto>.Success(new ArchiveArticleResponseDto
                    {
                        ArticleId = updatedArticle.ArticleId,
                        PublicId = updatedArticle.PublicId,
                        Status = updatedArticle.Status,
                        ArchivedAt = updatedArticle.ArchivedAt,
                        Version = updatedArticle.Version,
                        UpdatedAt = updatedArticle.UpdatedAt
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
                return Result<ArchiveArticleResponseDto>.Failure(MapPersistenceException(exception));
            }
            catch (ContentDomainException ex)
            {
                return Result<ArchiveArticleResponseDto>.Failure(MapDomainException(ex));
            }
        }

        private static Error MapDomainException(ContentDomainException ex)
        {
            return ex.Code switch
            {
                "CONTENT.ARTICLE_PUBLIC_ID_REQUIRED" => ContentErrors.Article.PublicIdRequired,
                "CONTENT.ARTICLE_AUTHOR_USER_ID_INVALID" => ContentErrors.Article.AuthorUserIdInvalid,
                "CONTENT.ARTICLE_TITLE_REQUIRED" => ContentErrors.Article.TitleRequired,
                "CONTENT.ARTICLE_TITLE_TOO_LONG" => ContentErrors.Article.TitleTooLong,
                "CONTENT.ARTICLE_BODY_REQUIRED" => ContentErrors.Article.BodyRequired,
                "CONTENT.INVALID_STATE_TRANSITION" => ContentErrors.InvalidStateTransition,
                "CONTENT.UNPUBLISH_REASON_REQUIRED" => ContentErrors.UnpublishReasonRequired,
                "CONTENT.ARTICLE_INVALID_ARTICLE_ID" => ContentErrors.Article.InvalidArticleId,
                "CONTENT.ARTICLE_INVALID_VERSION" => ContentErrors.Article.InvalidVersion,
                "CONTENT.ARTICLE_ALREADY_PUBLISHED" => ContentErrors.Article.AlreadyPublished,
                "CONTENT.ARTICLE_NOT_PUBLISHED" => ContentErrors.Article.NotPublished,
                "CONTENT.ARTICLE_ALREADY_ARCHIVED" => ContentErrors.Article.AlreadyArchived,
                "CONTENT.ARTICLE_NOT_ARCHIVED" => ContentErrors.Article.NotArchived,
                "CONTENT.ARTICLE_ALREADY_DELETED" => ContentErrors.Article.AlreadyDeleted,
                _ => ContentErrors.ValidationFailed
            };
        }

        private static Error MapPersistenceException(PersistenceException exception)
        {
            return exception.Code switch
            {
                "CONTENT.CONCURRENCY_CONFLICT" => ContentErrors.ConcurrencyConflict,
                _ => ContentErrors.ValidationFailed
            };
        }
    }
}