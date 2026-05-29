using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.CommentModerationCases.GetModerationCases;

namespace Interaction.Application.UseCases.CommentModerationCases.GetModerationCases;

public interface IGetModerationCasesUseCase
{
    Task<Result<PagedQueryResult<GetModerationCaseItemResponseDto>>> ExecuteAsync(
        GetModerationCasesRequestDto request,
        CancellationToken cancellationToken = default);
}