using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;
using Seo.Application.Errors;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Constants;
using Seo.Domain.Entities;
using Seo.Domain.Exceptions;

namespace Seo.Application.UseCases.SlugRoutes.CheckSlugAvailability;

public sealed class CheckSlugAvailabilityUseCase : ICheckSlugAvailabilityUseCase
{
    private readonly ISlugRegistryRepository _slugRegistryRepository;

    public CheckSlugAvailabilityUseCase(
        ISlugRegistryRepository slugRegistryRepository)
    {
        _slugRegistryRepository = slugRegistryRepository
            ?? throw new ArgumentNullException(nameof(slugRegistryRepository));
    }

    public async Task<Result<CheckSlugAvailabilityResponse>> ExecuteAsync(
        CheckSlugAvailabilityRequest request,
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
                return Result<CheckSlugAvailabilityResponse>.Failure(
                    SeoErrors.SlugRegistry.InvalidScope);
            }

            if (string.IsNullOrWhiteSpace(request.Slug))
            {
                return Result<CheckSlugAvailabilityResponse>.Failure(
                    SeoErrors.SlugRegistry.SlugRequired);
            }

            string slug = request.Slug.Trim();

            if (slug.Length > 200)
            {
                return Result<CheckSlugAvailabilityResponse>.Failure(
                    SeoErrors.SlugRegistry.SlugTooLong);
            }

            string? requestedResourceType = string.IsNullOrWhiteSpace(request.ResourceType)
                ? null
                : request.ResourceType.Trim();

            string? requestedResourcePublicId = string.IsNullOrWhiteSpace(request.ResourcePublicId)
                ? null
                : request.ResourcePublicId.Trim();

            if (requestedResourceType is not null &&
                !SeoResourceTypes.IsValid(requestedResourceType))
            {
                return Result<CheckSlugAvailabilityResponse>.Failure(
                    SeoErrors.Resource.InvalidResourceType);
            }

            if (requestedResourcePublicId is not null &&
                requestedResourcePublicId.Length != 26)
            {
                return Result<CheckSlugAvailabilityResponse>.Failure(
                    SeoErrors.Resource.InvalidResourcePublicId);
            }

            SlugRegistry? existing = await _slugRegistryRepository.GetByScopeAndSlugAsync(
                scope,
                slug,
                onlyActive: true,
                cancellationToken);

            if (existing is null)
            {
                return Result<CheckSlugAvailabilityResponse>.Success(
                    new CheckSlugAvailabilityResponse
                    {
                        Scope = scope,
                        Slug = slug,
                        IsAvailable = true,
                        BelongsToCurrentResource = false
                    });
            }

            bool belongsToCurrentResource =
                requestedResourceType is not null &&
                requestedResourcePublicId is not null &&
                string.Equals(existing.ResourceType, requestedResourceType, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(existing.ResourcePublicId, requestedResourcePublicId, StringComparison.OrdinalIgnoreCase);

            return Result<CheckSlugAvailabilityResponse>.Success(
                new CheckSlugAvailabilityResponse
                {
                    Scope = scope,
                    Slug = slug,
                    IsAvailable = belongsToCurrentResource,
                    BelongsToCurrentResource = belongsToCurrentResource,
                    ExistingResourceType = existing.ResourceType,
                    ExistingResourcePublicId = existing.ResourcePublicId,
                    ExistingSlugId = existing.SlugId
                });
        }
        catch (PersistenceException exception)
        {
            return Result<CheckSlugAvailabilityResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (SeoDomainException exception)
        {
            return Result<CheckSlugAvailabilityResponse>.Failure(
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