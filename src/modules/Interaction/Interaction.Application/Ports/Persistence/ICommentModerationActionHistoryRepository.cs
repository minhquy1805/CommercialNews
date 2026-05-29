using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Interaction.Application.Models.Queries;
using Interaction.Application.Models.Results;

namespace Interaction.Application.Ports.Persistence;

public interface ICommentModerationActionHistoryRepository
{
    Task<PagedQueryResult<CommentModerationHistoryItemResult>> GetByCommentPublicIdAsync(
        GetCommentModerationHistoryQuery query,
        CancellationToken cancellationToken = default);
}