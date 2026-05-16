using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Seo.Application.Models.Commands;
using Seo.Application.Models.Queries;
using Seo.Application.Models.Results;
using Seo.Domain.Entities;

namespace Seo.Application.Ports.Persistence;

public interface ISlugRegistryRepository
{
    Task<SlugRegistry?> GetByIdAsync(
        long slugId,
        CancellationToken cancellationToken = default);

    Task<SlugRegistry?> GetByScopeAndSlugAsync(
        string scope,
        string slug,
        bool? onlyActive = null,
        CancellationToken cancellationToken = default);

    Task<SlugRegistry?> GetByResourceAsync(
        string scope,
        string resourceType,
        string resourcePublicId,
        bool? onlyActive = null,
        CancellationToken cancellationToken = default);

    Task<SlugRegistry?> UpsertAsync(
        SlugRegistryUpsertCommand command,
        CancellationToken cancellationToken = default);

    Task<SeoApplyResultModel> ApplyContentVisibilityAsync(
        ApplyContentVisibilityCommand command,
        CancellationToken cancellationToken = default);

    Task<SlugRegistry?> DeactivateByResourceAsync(
        SlugRegistryDeactivateByResourceCommand command,
        CancellationToken cancellationToken = default);

    Task<ResolvedSlugRouteResult?> ResolveByScopeAndSlugAsync(
        string scope,
        string slug,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<SlugRegistryListResultItem>> SelectSkipAndTakeAsync(
        SlugRegistryListQuery query,
        CancellationToken cancellationToken = default);
}