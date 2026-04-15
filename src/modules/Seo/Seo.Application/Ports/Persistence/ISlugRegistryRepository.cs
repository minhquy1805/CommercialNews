using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Seo.Application.Models.QueryModels;
using Seo.Domain.Entities;

namespace Seo.Application.Ports.Persistence;

public interface ISlugRegistryRepository
{
    Task<long> InsertAsync(
        SlugRegistry slugRegistry,
        CancellationToken cancellationToken = default);

    Task<SlugRegistry?> GetByIdAsync(
        long slugId,
        CancellationToken cancellationToken = default);

    Task<SlugRegistry?> GetByScopeAndSlugAsync(
        string scope,
        string slug,
        bool? onlyActive = null,
        CancellationToken cancellationToken = default);

    Task<int> UpdateAsync(
        SlugRegistry slugRegistry,
        int expectedVersion,
        CancellationToken cancellationToken = default);

    Task<int> ActivateAsync(
        long slugId,
        long? updatedByUserId,
        int expectedVersion,
        CancellationToken cancellationToken = default);

    Task<int> DeactivateAsync(
        long slugId,
        long? updatedByUserId,
        int expectedVersion,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SlugRegistryListResultItem>> SelectByArticleIdAsync(
        long articleId,
        string? scope = null,
        bool? onlyActive = null,
        CancellationToken cancellationToken = default);

    Task<ResolveSeoRouteResult?> ResolveByScopeAndSlugAsync(
        string scope,
        string slug,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<SlugRegistryListResultItem>> SelectSkipAndTakeAsync(
        SlugRegistryListQuery query,
        CancellationToken cancellationToken = default);
}