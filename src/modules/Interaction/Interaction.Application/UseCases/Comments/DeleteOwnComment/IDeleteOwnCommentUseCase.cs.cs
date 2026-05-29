using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Comments.DeleteOwnComment;

namespace Interaction.Application.UseCases.Comments.DeleteOwnComment;

public interface IDeleteOwnCommentUseCase
{
    Task<Result<DeleteOwnCommentResponseDto>> ExecuteAsync(
        DeleteOwnCommentRequestDto request,
        CancellationToken cancellationToken = default);
}