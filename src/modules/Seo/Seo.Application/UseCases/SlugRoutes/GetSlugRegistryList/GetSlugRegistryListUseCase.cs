using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;
using Seo.Application.Errors;
using Seo.Application.Models.Queries;
using Seo.Application.Models.Results;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Exceptions;

namespace Seo.Application.UseCases.SlugRoutes.GetSlugRegistryList;

public sealed class GetSlugRegistryListUseCase : IGetSlugRegistryListUseCase
{
    private readonly ISlugRegistryRepository _slugRegistryRepository;

    public GetSlugRegistryListUseCase(
        ISlugRegistryRepository slugRegistryRepository)
    {
        _slugRegistryRepository = slugRegistryRepository
            ?? throw new ArgumentNullException(nameof(slugRegistryRepository));
    }

    public async Task<Result<PagedQueryResult<GetSlugRegistryListResponse>>> ExecuteAsync(
        GetSlugRegistryListRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            int page = request.Page <= 0 ? 1 : request.Page;
            int pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

            if (pageSize > 200)
            {
                pageSize = 200;
            }

            SlugRegistryListQuery query = new()
            {
                Skip = (page - 1) * pageSize,
                Take = pageSize,
                Scope = request.Scope,
                ResourceType = request.ResourceType,
                ResourcePublicId = request.ResourcePublicId,
                IsActive = request.IsActive,
                IsIndexable = request.IsIndexable,
                Keyword = request.Keyword,
                SortBy = string.IsNullOrWhiteSpace(request.SortBy)
                    ? "UpdatedAtUtc"
                    : request.SortBy,
                SortDirection = string.IsNullOrWhiteSpace(request.SortDirection)
                    ? "DESC"
                    : request.SortDirection
            };

            PagedQueryResult<SlugRegistryListResultItem> result =
                await _slugRegistryRepository.SelectSkipAndTakeAsync(
                    query,
                    cancellationToken);

            PagedQueryResult<GetSlugRegistryListResponse> response = new()
            {
                Page = result.Page,
                PageSize = result.PageSize,
                TotalItems = result.TotalItems,
                Items = result.Items
                    .Select(item => new GetSlugRegistryListResponse
                    {
                        SlugId = item.SlugId,

                        Scope = item.Scope,
                        Slug = item.Slug,

                        ResourceType = item.ResourceType,
                        ResourcePublicId = item.ResourcePublicId,

                        CanonicalUrl = item.CanonicalUrl,

                        IsIndexable = item.IsIndexable,
                        IsActive = item.IsActive,

                        SourceAggregateVersion = item.SourceAggregateVersion,
                        LastAppliedMessageId = item.LastAppliedMessageId,
                        LastSyncedAtUtc = item.LastSyncedAtUtc,

                        CreatedAtUtc = item.CreatedAtUtc,
                        CreatedByUserId = item.CreatedByUserId,

                        UpdatedAtUtc = item.UpdatedAtUtc,
                        UpdatedByUserId = item.UpdatedByUserId,

                        Version = item.Version
                    })
                    .ToArray()
            };

            return Result<PagedQueryResult<GetSlugRegistryListResponse>>.Success(response);
        }
        catch (PersistenceException exception)
        {
            return Result<PagedQueryResult<GetSlugRegistryListResponse>>.Failure(
                MapPersistenceException(exception));
        }
        catch (SeoDomainException exception)
        {
            return Result<PagedQueryResult<GetSlugRegistryListResponse>>.Failure(
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

            "SEO.VERSION_MISMATCH" => SeoErrors.SlugRegistry.VersionMismatch,
            "SEO.STORE_UNAVAILABLE" => SeoErrors.Infrastructure.StoreUnavailable,

            _ => SeoErrors.ValidationFailed
        };
    }
}