using CommercialNews.BuildingBlocks.Abstractions.Execution;
using CommercialNews.BuildingBlocks.Abstractions.Identifiers;
using CommercialNews.BuildingBlocks.Abstractions.Time;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;
using Content.Domain.Entities;
using Content.Domain.Exceptions;

namespace Content.Application.UseCases.Articles.CreateArticle;

public sealed class CreateArticleUseCase : ICreateArticleUseCase
{
    private readonly IArticleRepository _articleRepository;
    private readonly IArticleLifecycleEventRepository _articleLifecycleEventRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IContentUnitOfWork _unitOfWork;
    private readonly IPublicIdGenerator _publicIdGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContext _requestContext;

    public CreateArticleUseCase(
        IArticleRepository articleRepository,
        IArticleLifecycleEventRepository articleLifecycleEventRepository,
        ICategoryRepository categoryRepository,
        IContentUnitOfWork unitOfWork,
        IPublicIdGenerator publicIdGenerator,
        IDateTimeProvider dateTimeProvider,
        IRequestContext requestContext)
    {
        _articleRepository = articleRepository;
        _articleLifecycleEventRepository = articleLifecycleEventRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _publicIdGenerator = publicIdGenerator;
        _dateTimeProvider = dateTimeProvider;
        _requestContext = requestContext;
    }

    public async Task<Result<CreateArticleResponseDto>> ExecuteAsync(
        CreateArticleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.CategoryId.HasValue)
            {
                bool categoryExists = await _categoryRepository.ExistsByIdAsync(
                    request.CategoryId.Value,
                    cancellationToken);

                if (!categoryExists)
                {
                    return Result<CreateArticleResponseDto>.Failure(ContentErrors.Category.NotFound);
                }
            }

            string publicId = _publicIdGenerator.NewId();
            DateTime nowUtc = _dateTimeProvider.UtcNow;
            long? actorUserId = _requestContext.CurrentUserId;

            Article article = Article.CreateDraft(
                publicId: publicId,
                authorUserId: request.AuthorUserId,
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
                (long articleId, int version) = await _articleRepository.InsertAsync(
                    article,
                    cancellationToken);

                await _articleLifecycleEventRepository.InsertAsync(
                    articleId: articleId,
                    actionType: "Create",
                    fromStatus: null,
                    toStatus: article.Status,
                    reason: null,
                    occurredAt: nowUtc,
                    actorUserId: actorUserId,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<CreateArticleResponseDto>.Success(new CreateArticleResponseDto
                {
                    ArticleId = articleId,
                    PublicId = article.PublicId,
                    Status = article.Status,
                    Version = version,
                    CreatedAt = article.CreatedAt
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
            return Result<CreateArticleResponseDto>.Failure(MapPersistenceException(exception));
        }
        catch (ContentDomainException ex)
        {
            return Result<CreateArticleResponseDto>.Failure(MapDomainException(ex));
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