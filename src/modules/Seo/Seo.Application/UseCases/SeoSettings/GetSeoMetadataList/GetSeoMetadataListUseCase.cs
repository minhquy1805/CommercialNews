using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SeoMetadata.Requests;
using Seo.Application.Contracts.SeoMetadata.Responses;
using Seo.Application.Errors;
using Seo.Application.Models.Queries;
using Seo.Application.Models.Results;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Exceptions;

namespace Seo.Application.UseCases.SeoSettings.GetSeoMetadataList;

public sealed class GetSeoMetadataListUseCase : IGetSeoMetadataListUseCase
{
    private readonly ISeoMetadataRepository _seoMetadataRepository;

    public GetSeoMetadataListUseCase(
        ISeoMetadataRepository seoMetadataRepository)
    {
        _seoMetadataRepository = seoMetadataRepository
            ?? throw new ArgumentNullException(nameof(seoMetadataRepository));
    }

    public async Task<Result<PagedQueryResult<GetSeoMetadataListResponse>>> ExecuteAsync(
        GetSeoMetadataListRequest request,
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

            SeoMetadataListQuery query = new()
            {
                Skip = (page - 1) * pageSize,
                Take = pageSize,
                Scope = request.Scope,
                ResourceType = request.ResourceType,
                ResourcePublicId = request.ResourcePublicId,
                IsManualOverride = request.IsManualOverride,
                UpdatedByUserId = request.UpdatedByUserId,
                Keyword = request.Keyword,
                SortBy = string.IsNullOrWhiteSpace(request.SortBy)
                    ? "UpdatedAtUtc"
                    : request.SortBy,
                SortDirection = string.IsNullOrWhiteSpace(request.SortDirection)
                    ? "DESC"
                    : request.SortDirection
            };

            PagedQueryResult<SeoMetadataListResultItem> result =
                await _seoMetadataRepository.SelectSkipAndTakeAsync(
                    query,
                    cancellationToken);

            PagedQueryResult<GetSeoMetadataListResponse> response = new()
            {
                Page = result.Page,
                PageSize = result.PageSize,
                TotalItems = result.TotalItems,
                Items = result.Items
                    .Select(item => new GetSeoMetadataListResponse
                    {
                        SeoId = item.SeoId,

                        Scope = item.Scope,
                        ResourceType = item.ResourceType,
                        ResourcePublicId = item.ResourcePublicId,

                        Slug = item.Slug,
                        CanonicalUrl = item.CanonicalUrl,

                        MetaTitle = item.MetaTitle,
                        MetaDescription = item.MetaDescription,

                        OgTitle = item.OgTitle,
                        OgDescription = item.OgDescription,
                        OgImageUrl = item.OgImageUrl,

                        TwitterTitle = item.TwitterTitle,
                        TwitterDescription = item.TwitterDescription,
                        TwitterImageUrl = item.TwitterImageUrl,

                        Robots = item.Robots,

                        IsManualOverride = item.IsManualOverride,

                        SourceAggregateVersion = item.SourceAggregateVersion,
                        LastAppliedMessageId = item.LastAppliedMessageId,
                        LastSyncedAtUtc = item.LastSyncedAtUtc,

                        Version = item.Version,

                        CreatedAtUtc = item.CreatedAtUtc,
                        UpdatedAtUtc = item.UpdatedAtUtc,
                        UpdatedByUserId = item.UpdatedByUserId
                    })
                    .ToArray()
            };

            return Result<PagedQueryResult<GetSeoMetadataListResponse>>.Success(response);
        }
        catch (PersistenceException exception)
        {
            return Result<PagedQueryResult<GetSeoMetadataListResponse>>.Failure(
                MapPersistenceException(exception));
        }
        catch (SeoDomainException exception)
        {
            return Result<PagedQueryResult<GetSeoMetadataListResponse>>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(SeoDomainException exception)
    {
        return exception.Code switch
        {
            "SEO.INVALID_SCOPE" => SeoErrors.SeoMetadata.InvalidScope,
            "SEO.INVALID_RESOURCE_TYPE" => SeoErrors.Resource.InvalidResourceType,
            "SEO.INVALID_RESOURCE_PUBLIC_ID" => SeoErrors.Resource.InvalidResourcePublicId,
            "SEO.SEO_METADATA_INVALID_VERSION" => SeoErrors.SeoMetadata.InvalidVersion,
            "SEO.SEO_METADATA_INVALID_UPDATED_AT" => SeoErrors.SeoMetadata.InvalidUpdatedAt,
            _ => SeoErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "SEO.INVALID_SCOPE" => SeoErrors.SeoMetadata.InvalidScope,
            "SEO.INVALID_RESOURCE_TYPE" => SeoErrors.Resource.InvalidResourceType,
            "SEO.INVALID_RESOURCE_PUBLIC_ID" => SeoErrors.Resource.InvalidResourcePublicId,
            "SEO.STORE_UNAVAILABLE" => SeoErrors.Infrastructure.StoreUnavailable,
            _ => SeoErrors.ValidationFailed
        };
    }
}