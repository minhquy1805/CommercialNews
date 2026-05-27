using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Comments.GetCommentModerationHistory;

namespace Interaction.Application.UseCases.Comments.GetCommentModerationHistory;

public interface IGetCommentModerationHistoryUseCase
{
    Task<Result<PagedQueryResult<GetCommentModerationHistoryItemResponseDto>>> ExecuteAsync(
        GetCommentModerationHistoryRequestDto request,
        CancellationToken cancellationToken = default);
}