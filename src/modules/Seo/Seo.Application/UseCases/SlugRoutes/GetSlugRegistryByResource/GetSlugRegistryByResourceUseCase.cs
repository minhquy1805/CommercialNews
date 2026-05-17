using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;
using Seo.Application.Errors;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Constants;
using Seo.Domain.Entities;
using Seo.Domain.Exceptions;

namespace Seo.Application.UseCases.SlugRoutes.GetSlugRegistryByResource;

public sealed class GetSlugRegistryByResourceUseCase : IGetSlugRegistryByResourceUseCase
{
    private readonly ISlugRegistryRepository _slugRegistryRepository;

    public GetSlugRegistryByResourceUseCase(
        ISlugRegistryRepository slugRegistryRepository)
    {
        _slugRegistryRepository = slugRegistryRepository
            ?? throw new ArgumentNullException(nameof(slugRegistryRepository));
    }

    public async Task<Result<GetSlugRegistryByResourceResponse>> ExecuteAsync(
        GetSlugRegistryByResourceRequest request,
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
                return Result<GetSlugRegistryByResourceResponse>.Failure(
                    SeoErrors.SlugRegistry.InvalidScope);
            }

            if (!SeoResourceTypes.IsValid(request.ResourceType))
            {
                return Result<GetSlugRegistryByResourceResponse>.Failure(
                    SeoErrors.Resource.InvalidResourceType);
            }

            if (string.IsNullOrWhiteSpace(request.ResourcePublicId) ||
                request.ResourcePublicId.Trim().Length != 26)
            {
                return Result<GetSlugRegistryByResourceResponse>.Failure(
                    SeoErrors.Resource.InvalidResourcePublicId);
            }

            SlugRegistry? route = await _slugRegistryRepository.GetByResourceAsync(
                scope: scope,
                resourceType: request.ResourceType.Trim(),
                resourcePublicId: request.ResourcePublicId.Trim(),
                onlyActive: request.OnlyActive,
                cancellationToken: cancellationToken);

            if (route is null)
            {
                return Result<GetSlugRegistryByResourceResponse>.Failure(
                    SeoErrors.SlugRegistry.NotFound);
            }

            return Result<GetSlugRegistryByResourceResponse>.Success(
                new GetSlugRegistryByResourceResponse
                {
                    SlugId = route.SlugId,
                    Scope = route.Scope,
                    Slug = route.Slug,
                    ResourceType = route.ResourceType,
                    ResourcePublicId = route.ResourcePublicId,
                    CanonicalUrl = route.CanonicalUrl,
                    IsIndexable = route.IsIndexable,
                    IsActive = route.IsActive,
                    SourceAggregateVersion = route.SourceAggregateVersion,
                    LastAppliedMessageId = route.LastAppliedMessageId,
                    LastSyncedAtUtc = route.LastSyncedAtUtc,
                    Version = route.Version,
                    CreatedAtUtc = route.CreatedAtUtc,
                    CreatedByUserId = route.CreatedByUserId,
                    UpdatedAtUtc = route.UpdatedAtUtc,
                    UpdatedByUserId = route.UpdatedByUserId
                });
        }
        catch (PersistenceException exception)
        {
            return Result<GetSlugRegistryByResourceResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (SeoDomainException exception)
        {
            return Result<GetSlugRegistryByResourceResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(SeoDomainException exception)
    {
        return exception.Code switch
        {
            "SEO.INVALID_SCOPE" => SeoErrors.SlugRegistry.InvalidScope,
            "SEO.INVALID_RESOURCE_TYPE" => SeoErrors.Resource.InvalidResourceType,
            "SEO.INVALID_RESOURCE_PUBLIC_ID" => SeoErrors.Resource.InvalidResourcePublicId,
            "SEO.INVALID_SLUG" => SeoErrors.SlugRegistry.SlugRequired,
            "SEO.SLUG_TOO_LONG" => SeoErrors.SlugRegistry.SlugTooLong,
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
            "SEO.INVALID_RESOURCE_TYPE" => SeoErrors.Resource.InvalidResourceType,
            "SEO.INVALID_RESOURCE_PUBLIC_ID" => SeoErrors.Resource.InvalidResourcePublicId,
            "SEO.INVALID_SLUG" => SeoErrors.SlugRegistry.SlugRequired,
            "SEO.STORE_UNAVAILABLE" => SeoErrors.Infrastructure.StoreUnavailable,
            _ => SeoErrors.ValidationFailed
        };
    }
}