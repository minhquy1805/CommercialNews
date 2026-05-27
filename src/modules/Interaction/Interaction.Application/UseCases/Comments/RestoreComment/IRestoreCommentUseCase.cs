using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Comments.RestoreComment;

namespace Interaction.Application.UseCases.Comments.RestoreComment;

public interface IRestoreCommentUseCase
{
    Task<Result<RestoreCommentResponseDto>> ExecuteAsync(
        RestoreCommentRequestDto request,
        CancellationToken cancellationToken = default);
}