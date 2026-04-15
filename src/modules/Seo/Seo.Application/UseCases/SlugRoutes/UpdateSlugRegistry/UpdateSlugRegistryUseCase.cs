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

namespace Seo.Application.UseCases.SlugRoutes.UpdateSlugRegistry;

public sealed class UpdateSlugRegistryUseCase : IUpdateSlugRegistryUseCase
{
    private readonly ISlugRegistryRepository _slugRegistryRepository;
    private readonly ISeoUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContext _requestContext;

    public UpdateSlugRegistryUseCase(
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

    public async Task<Result<UpdateSlugRegistryResponse>> ExecuteAsync(
        UpdateSlugRegistryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.SlugId <= 0)
            {
                return Result<UpdateSlugRegistryResponse>.Failure(
                    SeoErrors.SlugRegistry.InvalidSlugId);
            }

            if (string.IsNullOrWhiteSpace(request.Scope))
            {
                return Result<UpdateSlugRegistryResponse>.Failure(
                    SeoErrors.SlugRegistry.InvalidScope);
            }

            if (string.IsNullOrWhiteSpace(request.Slug))
            {
                return Result<UpdateSlugRegistryResponse>.Failure(
                    SeoErrors.SlugRegistry.SlugRequired);
            }

            SlugRegistry? existing = await _slugRegistryRepository.GetByIdAsync(
                request.SlugId,
                cancellationToken);

            if (existing is null)
            {
                return Result<UpdateSlugRegistryResponse>.Failure(
                    SeoErrors.SlugRegistry.NotFound);
            }

            DateTime nowUtc = _dateTimeProvider.UtcNow;
            long? actorUserId = request.UpdatedByUserId ?? _requestContext.CurrentUserId;

            existing.UpdateRouting(
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
                int affectedRows = await _slugRegistryRepository.UpdateAsync(
                    existing,
                    request.ExpectedVersion,
                    cancellationToken);

                if (affectedRows <= 0)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<UpdateSlugRegistryResponse>.Failure(
                        SeoErrors.SlugRegistry.VersionMismatch);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<UpdateSlugRegistryResponse>.Success(
                    new UpdateSlugRegistryResponse
                    {
                        SlugId = existing.SlugId,
                        ArticleId = existing.ArticleId,
                        Slug = existing.Slug,
                        Scope = existing.Scope,
                        CanonicalUrl = existing.CanonicalUrl,
                        IsIndexable = existing.IsIndexable,
                        IsActive = existing.IsActive,
                        Version = existing.Version,
                        UpdatedAt = existing.UpdatedAt,
                        UpdatedByUserId = existing.UpdatedByUserId
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
            return Result<UpdateSlugRegistryResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (SeoDomainException exception)
        {
            return Result<UpdateSlugRegistryResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(SeoDomainException exception)
    {
        return exception.Code switch
        {
            "SEO.INVALID_SCOPE" => SeoErrors.SlugRegistry.InvalidScope,
            "SEO.INVALID_SLUG" => SeoErrors.SlugRegistry.SlugRequired,
            "SEO.SLUG_TOO_LONG" => SeoErrors.SlugRegistry.SlugTooLong,
            "SEO.SLUG_REGISTRY_INVALID_SLUG_ID" => SeoErrors.SlugRegistry.InvalidSlugId,
            "SEO.SLUG_REGISTRY_INVALID_ARTICLE_ID" => SeoErrors.SlugRegistry.InvalidArticleId,
            "SEO.SLUG_REGISTRY_INVALID_VERSION" => SeoErrors.SlugRegistry.InvalidVersion,
            "SEO.SLUG_REGISTRY_INVALID_UPDATED_AT" => SeoErrors.SlugRegistry.InvalidUpdatedAt,
            "SEO.SLUG_REGISTRY_ALREADY_ACTIVE" => SeoErrors.SlugRegistry.AlreadyActive,
            "SEO.SLUG_REGISTRY_ALREADY_INACTIVE" => SeoErrors.SlugRegistry.AlreadyInactive,
            "SEO.SLUG_REGISTRY_INACTIVE" => SeoErrors.SlugRegistry.Inactive,
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