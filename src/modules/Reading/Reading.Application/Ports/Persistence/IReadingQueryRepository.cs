using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Reading.Application.Models.QueryModels;

namespace Reading.Application.Ports.Persistence;

public interface IReadingQueryRepository
{
    Task<PagedQueryResult<ReadingArticleListItem>> GetArticlesAsync(
        ReadingArticleListQuery query,
        CancellationToken cancellationToken = default);

    Task<ReadingArticleDetailResult?> GetArticleByIdAsync(
        long articleId,
        CancellationToken cancellationToken = default);

    Task<ReadingArticleDetailResult?> GetArticleBySlugAsync(
        string scope,
        string slug,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReadingArticleListItem>> GetRelatedArticlesAsync(
        ReadingRelatedArticlesQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<ReadingArticleListItem>> SearchArticlesAsync(
        ReadingSearchArticlesQuery query,
        CancellationToken cancellationToken = default);
}