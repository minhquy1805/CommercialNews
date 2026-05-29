using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Comments.GetPublicComments;

namespace Interaction.Application.UseCases.Comments.GetPublicComments;

public interface IGetPublicCommentsUseCase
{
    Task<Result<PagedQueryResult<GetPublicCommentItemResponseDto>>> ExecuteAsync(
        GetPublicCommentsRequestDto request,
        CancellationToken cancellationToken = default);
}