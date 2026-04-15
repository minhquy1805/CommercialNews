using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Seo.Application.Models.QueryModels;
using Seo.Domain.Entities;

namespace Seo.Application.Ports.Persistence;

public interface ISeoMetadataRepository
{
    Task<long> InsertAsync(
        SeoMetadata seoMetadata,
        CancellationToken cancellationToken = default);

    Task<SeoMetadata?> GetByIdAsync(
        long seoId,
        CancellationToken cancellationToken = default);

    Task<SeoMetadata?> GetByArticleIdAsync(
        long articleId,
        CancellationToken cancellationToken = default);

    Task<int> UpdateAsync(
        SeoMetadata seoMetadata,
        int expectedVersion,
        CancellationToken cancellationToken = default);

    Task<SeoMetadataResult?> SelectMetadataByArticleIdAsync(
        long articleId,
        CancellationToken cancellationToken = default);

    Task<ArticleSeoSettingsResult?> GetArticleSeoSettingsByArticleIdAsync(
        long articleId,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<SeoMetadataListResultItem>> SelectSkipAndTakeAsync(
        SeoMetadataListQuery query,
        CancellationToken cancellationToken = default);
}