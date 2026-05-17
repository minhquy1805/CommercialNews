using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;
using Seo.Application.Errors;
using Seo.Application.Models.Results;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Constants;
using Seo.Domain.Exceptions;

namespace Seo.Application.UseCases.SlugRoutes.ResolveByScopeAndSlug;

public sealed class ResolveByScopeAndSlugUseCase : IResolveByScopeAndSlugUseCase
{
    private readonly ISlugRegistryRepository _slugRegistryRepository;

    public ResolveByScopeAndSlugUseCase(
        ISlugRegistryRepository slugRegistryRepository)
    {
        _slugRegistryRepository = slugRegistryRepository
            ?? throw new ArgumentNullException(nameof(slugRegistryRepository));
    }

    public async Task<Result<ResolveByScopeAndSlugResponse>> ExecuteAsync(
        ResolveByScopeAndSlugRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            string scope = string.IsNullOrWhiteSpace(request.Scope)
                ? SeoScopes.Public
                : request.Scope.Trim();

            if (!SeoScopes.IsValid(scope))
            {
                return Result<ResolveByScopeAndSlugResponse>.Failure(
                    SeoErrors.SlugRegistry.InvalidScope);
            }

            if (string.IsNullOrWhiteSpace(request.Slug))
            {
                return Result<ResolveByScopeAndSlugResponse>.Failure(
                    SeoErrors.SlugRegistry.SlugRequired);
            }

            string slug = request.Slug.Trim();

            if (slug.Length > 200)
            {
                return Result<ResolveByScopeAndSlugResponse>.Failure(
                    SeoErrors.SlugRegistry.SlugTooLong);
            }

            ResolvedSlugRouteResult? result =
                await _slugRegistryRepository.ResolveByScopeAndSlugAsync(
                    scope,
                    slug,
                    cancellationToken);

            if (result is null)
            {
                return Result<ResolveByScopeAndSlugResponse>.Failure(
                    SeoErrors.SlugRegistry.SafeNotFound);
            }

            return Result<ResolveByScopeAndSlugResponse>.Success(
                new ResolveByScopeAndSlugResponse
                {
                    Scope = result.Scope,
                    Slug = result.Slug,
                    ResourceType = result.ResourceType,
                    ResourcePublicId = result.ResourcePublicId,
                    CanonicalUrl = result.CanonicalUrl,
                    IsIndexable = result.IsIndexable,
                    Status = result.Status,
                    SourceAggregateVersion = result.SourceAggregateVersion,
                    Version = result.Version
                });
        }
        catch (PersistenceException exception)
        {
            return Result<ResolveByScopeAndSlugResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (SeoDomainException exception)
        {
            return Result<ResolveByScopeAndSlugResponse>.Failure(
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

            "SEO.INVALID_RESOURCE_TYPE" => SeoErrors.Resource.InvalidResourceType,
            "SEO.INVALID_RESOURCE_PUBLIC_ID" => SeoErrors.Resource.InvalidResourcePublicId,

            "SEO.SLUG_REGISTRY_INVALID_SLUG_ID" => SeoErrors.SlugRegistry.InvalidSlugId,
            "SEO.SLUG_REGISTRY_INVALID_VERSION" => SeoErrors.SlugRegistry.InvalidVersion,
            "SEO.SLUG_REGISTRY_INVALID_UPDATED_AT" => SeoErrors.SlugRegistry.InvalidUpdatedAt,

            "SEO.CANONICAL_URL_TOO_LONG" => SeoErrors.SlugRegistry.CanonicalUrlTooLong,
            "SEO.INVALID_SOURCE_AGGREGATE_VERSION" => SeoErrors.Sync.InvalidSourceAggregateVersion,
            "SEO.INVALID_LAST_APPLIED_MESSAGE_ID" => SeoErrors.Sync.InvalidLastAppliedMessageId,

            _ => SeoErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "SEO.INVALID_SCOPE" => SeoErrors.SlugRegistry.InvalidScope,
            "SEO.INVALID_SLUG" => SeoErrors.SlugRegistry.SlugRequired,
            "SEO.INVALID_RESOURCE_TYPE" => SeoErrors.Resource.InvalidResourceType,
            "SEO.INVALID_RESOURCE_PUBLIC_ID" => SeoErrors.Resource.InvalidResourcePublicId,
            "SEO.STORE_UNAVAILABLE" => SeoErrors.Infrastructure.StoreUnavailable,

            _ => SeoErrors.ValidationFailed
        };
    }
}