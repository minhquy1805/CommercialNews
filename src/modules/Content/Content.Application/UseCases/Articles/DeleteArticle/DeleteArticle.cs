using CommercialNews.BuildingBlocks.Abstractions.Execution;
using CommercialNews.BuildingBlocks.Abstractions.Time;
using CommercialNews.BuildingBlocks.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;
using Content.Domain.Entities;
using Content.Domain.Exceptions;

namespace Content.Application.UseCases.Articles.DeleteArticle
{
    public sealed class DeleteArticleUseCase : IDeleteArticleUseCase
    {
        private readonly IArticleRepository _articleRepository;
        private readonly IArticleLifecycleEventRepository _articleLifecycleEventRepository;
        private readonly IContentUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IRequestContext _requestContext;

        public DeleteArticleUseCase(
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

        public async Task<Result<DeleteArticleResponseDto>> ExecuteAsync(
            DeleteArticleRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request.ArticleId <= 0)
            {
                return Result<DeleteArticleResponseDto>.Failure(ContentErrors.Article.InvalidArticleId);
            }

            if (request.ExpectedVersion <= 0)
            {
                return Result<DeleteArticleResponseDto>.Failure(ContentErrors.Article.InvalidVersion);
            }

            try
            {
                Article? article = await _articleRepository.GetByIdAsync(
                    request.ArticleId,
                    cancellationToken);

                if (article is null)
                {
                    return Result<DeleteArticleResponseDto>.Failure(ContentErrors.Article.NotFound);
                }

                DateTime nowUtc = _dateTimeProvider.UtcNow;
                long? actorUserId = _requestContext.CurrentUserId;

                string fromStatus = article.Status;

                article.SoftDelete(
                    nowUtc: nowUtc,
                    actorUserId: actorUserId);

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    bool updated = await _articleRepository.UpdateAsync(
                        article,
                        request.ExpectedVersion,
                        cancellationToken);

                    if (!updated)
                    {
                        await _unitOfWork.RollbackAsync(cancellationToken);
                        return Result<DeleteArticleResponseDto>.Failure(ContentErrors.ConcurrencyConflict);
                    }

                    await _articleLifecycleEventRepository.InsertAsync(
                        articleId: article.ArticleId,
                        actionType: "Delete",
                        fromStatus: fromStatus,
                        toStatus: article.Status,
                        reason: null,
                        occurredAt: nowUtc,
                        actorUserId: actorUserId,
                        cancellationToken: cancellationToken);

                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result<DeleteArticleResponseDto>.Success(new DeleteArticleResponseDto
                    {
                        ArticleId = article.ArticleId,
                        PublicId = article.PublicId,
                        IsDeleted = article.IsDeleted,
                        DeletedAt = article.DeletedAt,
                        Version = article.Version,
                        UpdatedAt = article.UpdatedAt
                    });
                }
                catch
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            catch (ContentDomainException ex)
            {
                return Result<DeleteArticleResponseDto>.Failure(MapDomainException(ex));
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
    }
}