using CommercialNews.BuildingBlocks.Results;
using Interaction.Application.Contracts.Comments.Requests;
using Interaction.Application.Contracts.Comments.Responses;

namespace Interaction.Application.UseCases.Comments.DeleteComment;

public interface IDeleteCommentUseCase
{
    Task<Result<DeleteCommentResponse>> ExecuteAsync(
        DeleteCommentRequest request,
        CancellationToken cancellationToken = default);
}