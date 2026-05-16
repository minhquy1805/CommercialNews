using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;
using Seo.Application.Errors;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Entities;
using Seo.Domain.Exceptions;

namespace Seo.Application.UseCases.SlugRoutes.GetSlugRegistryById;

public sealed class GetSlugRegistryByIdUseCase : IGetSlugRegistryByIdUseCase
{
    private readonly ISlugRegistryRepository _slugRegistryRepository;

    public GetSlugRegistryByIdUseCase(
        ISlugRegistryRepository slugRegistryRepository)
    {
        _slugRegistryRepository = slugRegistryRepository
            ?? throw new ArgumentNullException(nameof(slugRegistryRepository));
    }

    public async Task<Result<GetSlugRegistryByIdResponse>> ExecuteAsync(
        GetSlugRegistryByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            if (request.SlugId <= 0)
            {
                return Result<GetSlugRegistryByIdResponse>.Failure(
                    SeoErrors.SlugRegistry.InvalidSlugId);
            }

            SlugRegistry? existing = await _slugRegistryRepository.GetByIdAsync(
                request.SlugId,
                cancellationToken);

            if (existing is null)
            {
                return Result<GetSlugRegistryByIdResponse>.Failure(
                    SeoErrors.SlugRegistry.NotFound);
            }

            return Result<GetSlugRegistryByIdResponse>.Success(
                new GetSlugRegistryByIdResponse
                {
                    SlugId = existing.SlugId,

                    Scope = existing.Scope,
                    Slug = existing.Slug,

                    ResourceType = existing.ResourceType,
                    ResourcePublicId = existing.ResourcePublicId,

                    CanonicalUrl = existing.CanonicalUrl,

                    IsIndexable = existing.IsIndexable,
                    IsActive = existing.IsActive,

                    SourceAggregateVersion = existing.SourceAggregateVersion,
                    LastAppliedMessageId = existing.LastAppliedMessageId,
                    LastSyncedAtUtc = existing.LastSyncedAtUtc,

                    Version = existing.Version,

                    CreatedAtUtc = existing.CreatedAtUtc,
                    CreatedByUserId = existing.CreatedByUserId,

                    UpdatedAtUtc = existing.UpdatedAtUtc,
                    UpdatedByUserId = existing.UpdatedByUserId
                });
        }
        catch (PersistenceException exception)
        {
            return Result<GetSlugRegistryByIdResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (SeoDomainException exception)
        {
            return Result<GetSlugRegistryByIdResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(SeoDomainException exception)
    {
        return exception.Code switch
        {
            "SEO.SLUG_REGISTRY_INVALID_SLUG_ID" => SeoErrors.SlugRegistry.InvalidSlugId,

            "SEO.INVALID_SCOPE" => SeoErrors.SlugRegistry.InvalidScope,
            "SEO.INVALID_RESOURCE_TYPE" => SeoErrors.Resource.InvalidResourceType,
            "SEO.INVALID_RESOURCE_PUBLIC_ID" => SeoErrors.Resource.InvalidResourcePublicId,

            "SEO.INVALID_SLUG" => SeoErrors.SlugRegistry.SlugRequired,
            "SEO.SLUG_TOO_LONG" => SeoErrors.SlugRegistry.SlugTooLong,

            "SEO.CANONICAL_URL_TOO_LONG" => SeoErrors.SlugRegistry.CanonicalUrlTooLong,

            "SEO.SLUG_REGISTRY_INVALID_VERSION" => SeoErrors.SlugRegistry.InvalidVersion,
            "SEO.SLUG_REGISTRY_INVALID_UPDATED_AT" => SeoErrors.SlugRegistry.InvalidUpdatedAt,

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