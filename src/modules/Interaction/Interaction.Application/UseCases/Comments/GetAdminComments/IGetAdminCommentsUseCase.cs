using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Comments.GetAdminComments;

namespace Interaction.Application.UseCases.Comments.GetAdminComments;

public interface IGetAdminCommentsUseCase
{
    Task<Result<PagedQueryResult<GetAdminCommentItemResponseDto>>> ExecuteAsync(
        GetAdminCommentsRequestDto request,
        CancellationToken cancellationToken = default);
}