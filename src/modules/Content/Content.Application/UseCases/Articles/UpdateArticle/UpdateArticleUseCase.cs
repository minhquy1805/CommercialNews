using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;
using Content.Application.Ports.Services;
using Content.Domain.Entities;
using Content.Domain.Exceptions;

namespace Content.Application.UseCases.Articles.UpdateArticle;

public sealed class UpdateArticleUseCase : IUpdateArticleUseCase
{
    private readonly IArticleRepository _articleRepository;
    private readonly IArticleRevisionRepository _articleRevisionRepository;
    private readonly IArticleTagRepository _articleTagRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IContentUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContext _requestContext;
    private readonly IContentOutboxWriter _contentOutboxWriter;

    public UpdateArticleUseCase(
        IArticleRepository articleRepository,
        IArticleRevisionRepository articleRevisionRepository,
        IArticleTagRepository articleTagRepository,
        ICategoryRepository categoryRepository,
        ITagRepository tagRepository,
        IContentUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IRequestContext requestContext,
        IContentOutboxWriter contentOutboxWriter)
    {
        _articleRepository = articleRepository ?? throw new ArgumentNullException(nameof(articleRepository));
        _articleRevisionRepository = articleRevisionRepository ?? throw new ArgumentNullException(nameof(articleRevisionRepository));
        _articleTagRepository = articleTagRepository ?? throw new ArgumentNullException(nameof(articleTagRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _contentOutboxWriter = contentOutboxWriter ?? throw new ArgumentNullException(nameof(contentOutboxWriter));
    }

    public async Task<Result<UpdateArticleResponseDto>> ExecuteAsync(
        UpdateArticleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.ArticleId <= 0)
        {
            return Result<UpdateArticleResponseDto>.Failure(
                ContentErrors.Article.InvalidArticleId);
        }

        if (request.ExpectedVersion <= 0)
        {
            return Result<UpdateArticleResponseDto>.Failure(
                ContentErrors.Article.InvalidVersion);
        }

        if (request.CategoryId <= 0)
        {
            return Result<UpdateArticleResponseDto>.Failure(
                ContentErrors.Article.CategoryIdInvalid);
        }

        IReadOnlyCollection<long> distinctTagIds = request.TagIds
            .Distinct()
            .ToArray();

        if (distinctTagIds.Any(tagId => tagId <= 0))
        {
            return Result<UpdateArticleResponseDto>.Failure(
                ContentErrors.ArticleTag.InvalidTagId);
        }

        try
        {
            Article? article = await _articleRepository.GetByIdAsync(
                request.ArticleId,
                cancellationToken);

            if (article is null)
            {
                return Result<UpdateArticleResponseDto>.Failure(
                    ContentErrors.Article.NotFound);
            }

            if (article.Version != request.ExpectedVersion)
            {
                return Result<UpdateArticleResponseDto>.Failure(
                    ContentErrors.ConcurrencyConflict);
            }

            bool categoryUsable = await _categoryRepository.ExistsActiveByIdAsync(
                request.CategoryId,
                cancellationToken);

            if (!categoryUsable)
            {
                return Result<UpdateArticleResponseDto>.Failure(
                    ContentErrors.Category.InactiveOrDeleted);
            }

            foreach (long tagId in distinctTagIds)
            {
                bool tagUsable = await _tagRepository.ExistsActiveByIdAsync(
                    tagId,
                    cancellationToken);

                if (!tagUsable)
                {
                    return Result<UpdateArticleResponseDto>.Failure(
                        ContentErrors.ArticleTag.TagNotAttachable);
                }
            }

            DateTime nowUtc = _dateTimeProvider.UtcNow;

            long actorUserId =
                request.ActorUserId
                ?? _requestContext.CurrentUserId
                ?? article.AuthorUserId;

            string? oldTitle = article.Title;
            string? oldSummary = article.Summary;
            string? oldBody = article.Body;

            article.UpdateDraft(
                categoryId: request.CategoryId,
                title: request.Title,
                summary: request.Summary,
                body: request.Body,
                coverMediaId: request.CoverMediaId,
                nowUtc: nowUtc,
                actorUserId: actorUserId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                Article? updatedArticle = await _articleRepository.UpdateAsync(
                    article,
                    request.ExpectedVersion,
                    cancellationToken);

                if (updatedArticle is null)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<UpdateArticleResponseDto>.Failure(
                        ContentErrors.ConcurrencyConflict);
                }

                ArticleRevision revision = ArticleRevision.Create(
                    articleId: updatedArticle.ArticleId,
                    editedByUserId: actorUserId,
                    articleVersion: updatedArticle.Version,
                    correlationId: _requestContext.CorrelationId,
                    changeSummary: NormalizeOptional(request.ChangeSummary),
                    oldTitle: oldTitle,
                    oldSummary: oldSummary,
                    oldBody: oldBody,
                    nowUtc: nowUtc);

                ArticleRevision? insertedRevision = await _articleRevisionRepository.InsertAsync(
                    revision,
                    cancellationToken);

                if (insertedRevision is null)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<UpdateArticleResponseDto>.Failure(
                        ContentErrors.WriteCommitFailed);
                }

                await _articleTagRepository.DeleteAllByArticleIdAsync(
                    updatedArticle.ArticleId,
                    cancellationToken);

                foreach (long tagId in distinctTagIds)
                {
                    ArticleTag articleTag = ArticleTag.Attach(
                        articleId: updatedArticle.ArticleId,
                        tagId: tagId,
                        nowUtc: nowUtc,
                        actorUserId: actorUserId);

                    ArticleTag? attachedTag = await _articleTagRepository.InsertAsync(
                        articleTag,
                        cancellationToken);

                    if (attachedTag is null)
                    {
                        await _unitOfWork.RollbackAsync(cancellationToken);

                        return Result<UpdateArticleResponseDto>.Failure(
                            ContentErrors.WriteCommitFailed);
                    }
                }

                await _contentOutboxWriter.EnqueueArticleUpdatedAsync(
                    unitOfWork: _unitOfWork,
                    articleId: updatedArticle.ArticleId,
                    articlePublicId: updatedArticle.ArticlePublicId,
                    status: updatedArticle.Status,
                    categoryId: updatedArticle.CategoryId,
                    actorUserId: actorUserId,
                    revisionId: insertedRevision.RevisionId,
                    changeSummary: insertedRevision.ChangeSummary,
                    slug: null,
                    canonicalUrl: null,
                    title: updatedArticle.Title,
                    summary: updatedArticle.Summary,
                    coverImageUrl: null,
                    tagIds: distinctTagIds,
                    version: updatedArticle.Version,
                    updatedAtUtc: updatedArticle.UpdatedAt,
                    correlationId: _requestContext.CorrelationId,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<UpdateArticleResponseDto>.Success(
                    new UpdateArticleResponseDto
                    {
                        ArticleId = updatedArticle.ArticleId,
                        ArticlePublicId = updatedArticle.ArticlePublicId,
                        CategoryId = updatedArticle.CategoryId,
                        AuthorUserId = updatedArticle.AuthorUserId,
                        Title = updatedArticle.Title,
                        Summary = updatedArticle.Summary,
                        Body = updatedArticle.Body,
                        Status = updatedArticle.Status,
                        CoverMediaId = updatedArticle.CoverMediaId,
                        TagIds = distinctTagIds,
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
            await RollbackIfNeededAsync(cancellationToken);

            return Result<UpdateArticleResponseDto>.Failure(
                MapPersistenceException(exception));
        }
        catch (ContentDomainException exception)
        {
            await RollbackIfNeededAsync(cancellationToken);

            return Result<UpdateArticleResponseDto>.Failure(
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

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static Error MapDomainException(ContentDomainException exception)
    {
        return exception.Code switch
        {
            "CONTENT.ARTICLE_PUBLIC_ID_REQUIRED" => ContentErrors.Article.PublicIdRequired,
            "CONTENT.ARTICLE_PUBLIC_ID_INVALID" => ContentErrors.Article.PublicIdInvalid,

            "CONTENT.ARTICLE_INVALID_ARTICLE_ID" => ContentErrors.Article.InvalidArticleId,
            "CONTENT.ARTICLE_INVALID_VERSION" => ContentErrors.Article.InvalidVersion,
            "CONTENT.ARTICLE_CATEGORY_ID_INVALID" => ContentErrors.Article.CategoryIdInvalid,
            "CONTENT.ARTICLE_AUTHOR_USER_ID_INVALID" => ContentErrors.Article.AuthorUserIdInvalid,
            "CONTENT.ARTICLE_ACTOR_USER_ID_INVALID" => ContentErrors.Article.ActorUserIdInvalid,

            "CONTENT.ARTICLE_TITLE_REQUIRED" => ContentErrors.Article.TitleRequired,
            "CONTENT.ARTICLE_TITLE_TOO_LONG" => ContentErrors.Article.TitleTooLong,
            "CONTENT.ARTICLE_SUMMARY_REQUIRED" => ContentErrors.Article.SummaryRequired,
            "CONTENT.ARTICLE_SUMMARY_TOO_LONG" => ContentErrors.Article.SummaryTooLong,
            "CONTENT.ARTICLE_BODY_REQUIRED" => ContentErrors.Article.BodyRequired,
            "CONTENT.ARTICLE_BODY_TOO_LONG" => ContentErrors.Article.BodyTooLong,

            "CONTENT.ARTICLE_ALREADY_ARCHIVED" => ContentErrors.Article.AlreadyArchived,
            "CONTENT.ARTICLE_ALREADY_DELETED" => ContentErrors.Article.AlreadyDeleted,
            "CONTENT.ARTICLE_ALREADY_SOFT_DELETED" => ContentErrors.Article.AlreadySoftDeleted,
            "CONTENT.ARTICLE_NOT_DRAFT" => ContentErrors.Article.NotDraft,

            "CONTENT.ARTICLE_REVISION_INVALID_ARTICLE_ID" => ContentErrors.Revision.InvalidArticleId,
            "CONTENT.ARTICLE_REVISION_INVALID_EDITOR_USER_ID" => ContentErrors.Revision.InvalidEditorUserId,
            "CONTENT.ARTICLE_REVISION_INVALID_ARTICLE_VERSION" => ContentErrors.Revision.InvalidArticleVersion,
            "CONTENT.ARTICLE_REVISION_PREVIOUS_SNAPSHOT_REQUIRED" => ContentErrors.Revision.PreviousSnapshotRequired,

            "CONTENT.ARTICLE_TAG_INVALID_ARTICLE_ID" => ContentErrors.ArticleTag.InvalidArticleId,
            "CONTENT.ARTICLE_TAG_INVALID_TAG_ID" => ContentErrors.ArticleTag.InvalidTagId,

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
            "CONTENT.ARTICLE_CATEGORY_ID_INVALID" => ContentErrors.Article.CategoryIdInvalid,

            "CONTENT.ARTICLE_TITLE_REQUIRED" => ContentErrors.Article.TitleRequired,
            "CONTENT.ARTICLE_SUMMARY_REQUIRED" => ContentErrors.Article.SummaryRequired,
            "CONTENT.ARTICLE_BODY_REQUIRED" => ContentErrors.Article.BodyRequired,

            "CONTENT.CATEGORY_INACTIVE_OR_DELETED" => ContentErrors.Category.InactiveOrDeleted,

            "CONTENT.ARTICLE_TAG_ARTICLE_NOT_DRAFT" => ContentErrors.ArticleTag.ArticleNotDraft,
            "CONTENT.ARTICLE_TAG_TAG_NOT_ATTACHABLE" => ContentErrors.ArticleTag.TagNotAttachable,
            "CONTENT.ARTICLE_TAG_ALREADY_EXISTS" => ContentErrors.ArticleTag.AlreadyExists,

            "CONTENT.ARTICLE_REVISION_INVALID_ARTICLE_ID" => ContentErrors.Revision.InvalidArticleId,
            "CONTENT.ARTICLE_REVISION_INVALID_EDITOR_USER_ID" => ContentErrors.Revision.InvalidEditorUserId,
            "CONTENT.ARTICLE_REVISION_INVALID_ARTICLE_VERSION" => ContentErrors.Revision.InvalidArticleVersion,
            "CONTENT.ARTICLE_REVISION_PREVIOUS_SNAPSHOT_REQUIRED" => ContentErrors.Revision.PreviousSnapshotRequired,

            _ => ContentErrors.WriteCommitFailed
        };
    }
}
