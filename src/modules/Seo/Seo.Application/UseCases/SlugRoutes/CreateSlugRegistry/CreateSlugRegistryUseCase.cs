using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;
using Seo.Application.Errors;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Entities;
using Seo.Domain.Exceptions;

namespace Seo.Application.UseCases.SlugRoutes.CreateSlugRegistry;

public sealed class CreateSlugRegistryUseCase : ICreateSlugRegistryUseCase
{
    private readonly ISlugRegistryRepository _slugRegistryRepository;
    private readonly ISeoUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContext _requestContext;

    public CreateSlugRegistryUseCase(
        ISlugRegistryRepository slugRegistryRepository,
        ISeoUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IRequestContext requestContext)
    {
        _slugRegistryRepository = slugRegistryRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _requestContext = requestContext;
    }

    public async Task<Result<CreateSlugRegistryResponse>> ExecuteAsync(
        CreateSlugRegistryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.ArticleId <= 0)
            {
                return Result<CreateSlugRegistryResponse>.Failure(
                    SeoErrors.Article.InvalidArticleId);
            }

            if (string.IsNullOrWhiteSpace(request.Scope))
            {
                return Result<CreateSlugRegistryResponse>.Failure(
                    SeoErrors.SlugRegistry.InvalidScope);
            }

            if (string.IsNullOrWhiteSpace(request.Slug))
            {
                return Result<CreateSlugRegistryResponse>.Failure(
                    SeoErrors.SlugRegistry.SlugRequired);
            }

            DateTime nowUtc = _dateTimeProvider.UtcNow;
            long? actorUserId = request.CreatedByUserId ?? _requestContext.CurrentUserId;

            SlugRegistry slugRegistry = SlugRegistry.Create(
                articleId: request.ArticleId,
                slug: request.Slug,
                scope: request.Scope,
                canonicalUrl: request.CanonicalUrl,
                isIndexable: request.IsIndexable,
                isActive: request.IsActive,
                nowUtc: nowUtc,
                actorUserId: actorUserId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                long slugId = await _slugRegistryRepository.InsertAsync(
                    slugRegistry,
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<CreateSlugRegistryResponse>.Success(
                    new CreateSlugRegistryResponse
                    {
                        SlugId = slugId,
                        ArticleId = slugRegistry.ArticleId,
                        Slug = slugRegistry.Slug,
                        Scope = slugRegistry.Scope,
                        CanonicalUrl = slugRegistry.CanonicalUrl,
                        IsIndexable = slugRegistry.IsIndexable,
                        IsActive = slugRegistry.IsActive,
                        Version = slugRegistry.Version,
                        CreatedAt = slugRegistry.CreatedAt,
                        CreatedByUserId = slugRegistry.CreatedByUserId,
                        UpdatedAt = slugRegistry.UpdatedAt,
                        UpdatedByUserId = slugRegistry.UpdatedByUserId
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
            return Result<CreateSlugRegistryResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (SeoDomainException exception)
        {
            return Result<CreateSlugRegistryResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(SeoDomainException exception)
    {
        return exception.Code switch
        {
            "SEO.ARTICLE_INVALID_ARTICLE_ID" => SeoErrors.Article.InvalidArticleId,

            "SEO.INVALID_SCOPE" => SeoErrors.SlugRegistry.InvalidScope,
            "SEO.INVALID_SLUG" => SeoErrors.SlugRegistry.SlugRequired,
            "SEO.SLUG_TOO_LONG" => SeoErrors.SlugRegistry.SlugTooLong,
            "SEO.SLUG_REGISTRY_INVALID_SLUG_ID" => SeoErrors.SlugRegistry.InvalidSlugId,
            "SEO.SLUG_REGISTRY_INVALID_ARTICLE_ID" => SeoErrors.SlugRegistry.InvalidArticleId,
            "SEO.SLUG_REGISTRY_INVALID_VERSION" => SeoErrors.SlugRegistry.InvalidVersion,
            "SEO.SLUG_REGISTRY_INVALID_UPDATED_AT" => SeoErrors.SlugRegistry.InvalidUpdatedAt,
            "SEO.CANONICAL_URL_TOO_LONG" => SeoErrors.SlugRegistry.CanonicalUrlTooLong,

            _ => SeoErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "SEO.SLUG_CONFLICT" => SeoErrors.SlugRegistry.Conflict,
            "SEO.VERSION_MISMATCH" => SeoErrors.SlugRegistry.VersionMismatch,
            "SEO.STALE_WRITE_CONFLICT" => SeoErrors.SlugRegistry.StaleWriteConflict,
            _ => SeoErrors.ValidationFailed
        };
    }
}