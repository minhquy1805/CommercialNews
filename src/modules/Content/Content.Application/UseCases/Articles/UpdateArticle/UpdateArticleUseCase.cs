using CommercialNews.BuildingBlocks.Abstractions.Execution;
using CommercialNews.BuildingBlocks.Abstractions.Time;
using CommercialNews.BuildingBlocks.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;
using Content.Domain.Entities;
using Content.Domain.Exceptions;

namespace Content.Application.UseCases.Articles.UpdateArticle
{
    public sealed class UpdateArticleUseCase : IUpdateArticleUseCase
    {
        private readonly IArticleRepository _articleRepository;
        private readonly IArticleRevisionRepository _articleRevisionRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IContentUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IRequestContext _requestContext;

        public UpdateArticleUseCase(
            IArticleRepository articleRepository,
            IArticleRevisionRepository articleRevisionRepository,
            ICategoryRepository categoryRepository,
            IContentUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider,
            IRequestContext requestContext)
        {
            _articleRepository = articleRepository;
            _articleRevisionRepository = articleRevisionRepository;
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
            _requestContext = requestContext;
        }

        public async Task<Result<UpdateArticleResponseDto>> ExecuteAsync(
            UpdateArticleRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request.ArticleId <= 0)
            {
                return Result<UpdateArticleResponseDto>.Failure(ContentErrors.Article.InvalidArticleId);
            }

            if (request.ExpectedVersion <= 0)
            {
                return Result<UpdateArticleResponseDto>.Failure(ContentErrors.Article.InvalidVersion);
            }

            try
            {
                Article? article = await _articleRepository.GetByIdAsync(
                    request.ArticleId,
                    cancellationToken);

                if (article is null)
                {
                    return Result<UpdateArticleResponseDto>.Failure(ContentErrors.Article.NotFound);
                }

                if (request.CategoryId.HasValue)
                {
                    bool categoryExists = await _categoryRepository.ExistsByIdAsync(
                        request.CategoryId.Value,
                        cancellationToken);

                    if (!categoryExists)
                    {
                        return Result<UpdateArticleResponseDto>.Failure(ContentErrors.Category.NotFound);
                    }
                }

                DateTime nowUtc = _dateTimeProvider.UtcNow;
                long? actorUserId = _requestContext.CurrentUserId;

                string oldTitle = article.Title;
                string? oldSummary = article.Summary;
                string oldBody = article.Body;
                long? oldCategoryId = article.CategoryId;
                string oldStatus = article.Status;
                long? oldCoverMediaId = article.CoverMediaId;

                article.UpdateDraft(
                    title: request.Title,
                    body: request.Body,
                    summary: request.Summary,
                    categoryId: request.CategoryId,
                    coverMediaId: request.CoverMediaId,
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
                        return Result<UpdateArticleResponseDto>.Failure(ContentErrors.ConcurrencyConflict);
                    }

                    await _articleRevisionRepository.InsertAsync(
                        articleId: article.ArticleId,
                        revisionNumber: article.Version,
                        titleSnapshot: oldTitle,
                        summarySnapshot: oldSummary,
                        bodySnapshot: oldBody,
                        categoryIdSnapshot: oldCategoryId,
                        statusSnapshot: oldStatus,
                        coverMediaIdSnapshot: oldCoverMediaId,
                        changedAt: nowUtc,
                        changedByUserId: actorUserId,
                        changeType: "Update",
                        changeSummary: NormalizeOptional(request.ChangeSummary),
                        cancellationToken: cancellationToken);

                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result<UpdateArticleResponseDto>.Success(new UpdateArticleResponseDto
                    {
                        ArticleId = article.ArticleId,
                        PublicId = article.PublicId,
                        Title = article.Title,
                        Summary = article.Summary,
                        Body = article.Body,
                        Status = article.Status,
                        CategoryId = article.CategoryId,
                        CoverMediaId = article.CoverMediaId,
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
                return Result<UpdateArticleResponseDto>.Failure(MapDomainException(ex));
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

        private static string? NormalizeOptional(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }
    }
}