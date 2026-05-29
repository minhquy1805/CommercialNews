using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Comments.GetAdminCommentByPublicId;

namespace Interaction.Application.UseCases.Comments.GetAdminCommentByPublicId;

public interface IGetAdminCommentByPublicIdUseCase
{
    Task<Result<GetAdminCommentByPublicIdResponseDto>> ExecuteAsync(
        GetAdminCommentByPublicIdRequestDto request,
        CancellationToken cancellationToken = default);
}