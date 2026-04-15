using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Content.Application.Models.QueryModels;

namespace Content.Application.Ports.Persistence
{
    public interface IArticleRevisionRepository
    {
        Task InsertAsync(
            long articleId,
            int revisionNumber,
            string titleSnapshot,
            string? summarySnapshot,
            string bodySnapshot,
            long? categoryIdSnapshot,
            string statusSnapshot,
            long? coverMediaIdSnapshot,
            DateTime changedAt,
            long? changedByUserId,
            string changeType,
            string? changeSummary,
            CancellationToken cancellationToken = default);

        Task<PagedQueryResult<ArticleRevisionListResultItem>> GetPagedByArticleIdAsync(
            ArticleRevisionListQuery query,
            CancellationToken cancellationToken = default);
        
        Task<ArticleRevisionDetailResultItem?> GetByIdAsync(
            long articleId,
            long revisionId,
            CancellationToken cancellationToken = default);
    }
}