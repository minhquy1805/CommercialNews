using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
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

namespace Content.Application.UseCases.Articles.CreateArticle;

public sealed class CreateArticleUseCase : ICreateArticleUseCase
{
    private readonly IArticleRepository _articleRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IArticleTagRepository _articleTagRepository;
    private readonly IContentUnitOfWork _unitOfWork;
    private readonly IPublicIdGenerator _publicIdGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContext _requestContext;
    private readonly IContentOutboxWriter _contentOutboxWriter;

    public CreateArticleUseCase(
        IArticleRepository articleRepository,
        ICategoryRepository categoryRepository,
        ITagRepository tagRepository,
        IArticleTagRepository articleTagRepository,
        IContentUnitOfWork unitOfWork,
        IPublicIdGenerator publicIdGenerator,
        IDateTimeProvider dateTimeProvider,
        IRequestContext requestContext,
        IContentOutboxWriter contentOutboxWriter)
    {
        _articleRepository = articleRepository ?? throw new ArgumentNullException(nameof(articleRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        _articleTagRepository = articleTagRepository ?? throw new ArgumentNullException(nameof(articleTagRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _publicIdGenerator = publicIdGenerator ?? throw new ArgumentNullException(nameof(publicIdGenerator));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        _contentOutboxWriter = contentOutboxWriter ?? throw new ArgumentNullException(nameof(contentOutboxWriter));
    }

    public async Task<Result<CreateArticleResponseDto>> ExecuteAsync(
        CreateArticleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.CategoryId <= 0)
        {
            return Result<CreateArticleResponseDto>.Failure(
                ContentErrors.Article.CategoryIdInvalid);
        }

        if (request.AuthorUserId <= 0)
        {
            return Result<CreateArticleResponseDto>.Failure(
                ContentErrors.Article.AuthorUserIdInvalid);
        }

        IReadOnlyCollection<long> distinctTagIds = request.TagIds
            .Distinct()
            .ToArray();

        if (distinctTagIds.Any(tagId => tagId <= 0))
        {
            return Result<CreateArticleResponseDto>.Failure(
                ContentErrors.ArticleTag.InvalidTagId);
        }

        try
        {
            bool categoryUsable = await _categoryRepository.ExistsActiveByIdAsync(
                request.CategoryId,
                cancellationToken);

            if (!categoryUsable)
            {
                return Result<CreateArticleResponseDto>.Failure(
                    ContentErrors.Category.InactiveOrDeleted);
            }

            foreach (long tagId in distinctTagIds)
            {
                bool tagUsable = await _tagRepository.ExistsActiveByIdAsync(
                    tagId,
                    cancellationToken);

                if (!tagUsable)
                {
                    return Result<CreateArticleResponseDto>.Failure(
                        ContentErrors.ArticleTag.TagNotAttachable);
                }
            }

            DateTime nowUtc = _dateTimeProvider.UtcNow;
            string articlePublicId = _publicIdGenerator.NewId();

            long createdByUserId =
                request.ActorUserId
                ?? _requestContext.CurrentUserId
                ?? request.AuthorUserId;

            Article article = Article.CreateDraft(
                articlePublicId: articlePublicId,
                categoryId: request.CategoryId,
                authorUserId: request.AuthorUserId,
                title: request.Title,
                summary: request.Summary,
                body: request.Body,
                coverMediaId: request.CoverMediaId,
                nowUtc: nowUtc,
                actorUserId: createdByUserId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                (long articleId, long version) = await _articleRepository.InsertAsync(
                    article,
                    cancellationToken);

                foreach (long tagId in distinctTagIds)
                {
                    ArticleTag articleTag = ArticleTag.Attach(
                        articleId: articleId,
                        tagId: tagId,
                        nowUtc: nowUtc,
                        actorUserId: createdByUserId);

                    ArticleTag? attachedTag = await _articleTagRepository.InsertAsync(
                        articleTag,
                        cancellationToken);

                    if (attachedTag is null)
                    {
                        await _unitOfWork.RollbackAsync(cancellationToken);

                        return Result<CreateArticleResponseDto>.Failure(
                            ContentErrors.WriteCommitFailed);
                    }
                }

                await _contentOutboxWriter.EnqueueArticleCreatedAsync(
                    unitOfWork: _unitOfWork,
                    articleId: articleId,
                    articlePublicId: article.ArticlePublicId,
                    categoryId: article.CategoryId,
                    authorUserId: article.AuthorUserId,
                    createdByUserId: createdByUserId,
                    status: article.Status,
                    slug: null,
                    canonicalUrl: null,
                    title: article.Title,
                    summary: article.Summary,
                    body: article.Body,
                    coverMediaId: article.CoverMediaId,
                    coverImageUrl: null,
                    tagIds: distinctTagIds,
                    version: version,
                    createdAtUtc: article.CreatedAt,
                    correlationId: _requestContext.CorrelationId,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<CreateArticleResponseDto>.Success(
                    new CreateArticleResponseDto
                    {
                        ArticleId = articleId,
                        ArticlePublicId = article.ArticlePublicId,
                        CategoryId = article.CategoryId,
                        AuthorUserId = article.AuthorUserId,
                        Title = article.Title,
                        Summary = article.Summary,
                        Status = article.Status,
                        CoverMediaId = article.CoverMediaId,
                        TagIds = distinctTagIds,
                        Version = version,
                        CreatedAt = article.CreatedAt
                    });
            }
            catch
            {
                await RollbackIfNeededAsync(cancellationToken);
                throw;
            }
        }
        catch (PersistenceException exception)
        {
            await RollbackIfNeededAsync(cancellationToken);

            return Result<CreateArticleResponseDto>.Failure(
                MapPersistenceException(exception));
        }
        catch (ContentDomainException exception)
        {
            await RollbackIfNeededAsync(cancellationToken);

            return Result<CreateArticleResponseDto>.Failure(
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
            "CONTENT.ARTICLE_PUBLIC_ID_REQUIRED" => ContentErrors.Article.PublicIdRequired,
            "CONTENT.ARTICLE_PUBLIC_ID_INVALID" => ContentErrors.Article.PublicIdInvalid,
            "CONTENT.ARTICLE_CATEGORY_ID_INVALID" => ContentErrors.Article.CategoryIdInvalid,
            "CONTENT.ARTICLE_AUTHOR_USER_ID_INVALID" => ContentErrors.Article.AuthorUserIdInvalid,
            "CONTENT.ARTICLE_CREATED_BY_USER_ID_INVALID" => ContentErrors.Article.CreatedByUserIdInvalid,
            "CONTENT.ARTICLE_TITLE_REQUIRED" => ContentErrors.Article.TitleRequired,
            "CONTENT.ARTICLE_TITLE_TOO_LONG" => ContentErrors.Article.TitleTooLong,
            "CONTENT.ARTICLE_SUMMARY_REQUIRED" => ContentErrors.Article.SummaryRequired,
            "CONTENT.ARTICLE_SUMMARY_TOO_LONG" => ContentErrors.Article.SummaryTooLong,
            "CONTENT.ARTICLE_BODY_REQUIRED" => ContentErrors.Article.BodyRequired,
            "CONTENT.ARTICLE_BODY_TOO_LONG" => ContentErrors.Article.BodyTooLong,
            "CONTENT.ARTICLE_TAG_INVALID_ARTICLE_ID" => ContentErrors.ArticleTag.InvalidArticleId,
            "CONTENT.ARTICLE_TAG_INVALID_TAG_ID" => ContentErrors.ArticleTag.InvalidTagId,
            _ => ContentErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "CONTENT.ARTICLE_PUBLIC_ID_INVALID" => ContentErrors.Article.PublicIdInvalid,
            "CONTENT.ARTICLE_PUBLIC_ID_ALREADY_EXISTS" => ContentErrors.Article.PublicIdAlreadyExists,
            "CONTENT.ARTICLE_CATEGORY_ID_INVALID" => ContentErrors.Article.CategoryIdInvalid,
            "CONTENT.ARTICLE_AUTHOR_USER_ID_INVALID" => ContentErrors.Article.AuthorUserIdInvalid,
            "CONTENT.ARTICLE_CREATED_BY_USER_ID_INVALID" => ContentErrors.Article.CreatedByUserIdInvalid,
            "CONTENT.ARTICLE_TITLE_REQUIRED" => ContentErrors.Article.TitleRequired,
            "CONTENT.ARTICLE_SUMMARY_REQUIRED" => ContentErrors.Article.SummaryRequired,
            "CONTENT.ARTICLE_BODY_REQUIRED" => ContentErrors.Article.BodyRequired,
            "CONTENT.CATEGORY_INACTIVE_OR_DELETED" => ContentErrors.Category.InactiveOrDeleted,
            "CONTENT.ARTICLE_TAG_ARTICLE_NOT_DRAFT" => ContentErrors.ArticleTag.ArticleNotDraft,
            "CONTENT.ARTICLE_TAG_TAG_NOT_ATTACHABLE" => ContentErrors.ArticleTag.TagNotAttachable,
            "CONTENT.ARTICLE_TAG_ALREADY_EXISTS" => ContentErrors.ArticleTag.AlreadyExists,
            "CONTENT.CONCURRENCY_CONFLICT" => ContentErrors.ConcurrencyConflict,
            _ => ContentErrors.WriteCommitFailed
        };
    }
}
