using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Seo.Application.Models.Commands;
using Seo.Application.Models.Queries;
using Seo.Application.Models.Results;
using Seo.Domain.Entities;

namespace Seo.Application.Ports.Persistence;

public interface ISeoMetadataRepository
{
    Task<SeoMetadata?> GetByIdAsync(
        long seoId,
        CancellationToken cancellationToken = default);

    Task<SeoMetadata?> GetByResourceAsync(
        string scope,
        string resourceType,
        string resourcePublicId,
        CancellationToken cancellationToken = default);

    Task<SeoMetadata?> UpsertAsync(
        SeoMetadataUpsertCommand command,
        CancellationToken cancellationToken = default);

    Task<SeoApplyResultModel> ApplyContentDefaultsAsync(
        ApplyContentMetadataDefaultsCommand command,
        CancellationToken cancellationToken = default);

    Task<SeoMetadataResult?> SelectMetadataByResourceAsync(
        string scope,
        string resourceType,
        string resourcePublicId,
        CancellationToken cancellationToken = default);

    Task<ArticleSeoSettingsResult?> GetArticleSeoSettingsByArticlePublicIdAsync(
        string articlePublicId,
        string scope,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<SeoMetadataListResultItem>> SelectSkipAndTakeAsync(
        SeoMetadataListQuery query,
        CancellationToken cancellationToken = default);
}