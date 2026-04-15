using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Interaction.Application.Models.QueryModels;

namespace Interaction.Application.Ports.Persistence.Read;

public interface ICommentQueryRepository
{
    Task<PagedQueryResult<CommentListItem>> SelectVisibleByArticleIdAsync(
        CommentListQuery query,
        CancellationToken cancellationToken = default);

    Task<long> GetVisibleCountByArticleIdAsync(
        long articleId,
        CancellationToken cancellationToken = default);
}