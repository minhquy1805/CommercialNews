using CommercialNews.BuildingBlocks.Results;
using Interaction.Application.Contracts.Comments.Requests;
using Interaction.Application.Contracts.Comments.Responses;

namespace Interaction.Application.UseCases.Comments.UpdateComment;

public interface IUpdateCommentUseCase
{
    Task<Result<UpdateCommentResponse>> ExecuteAsync(
        UpdateCommentRequest request,
        CancellationToken cancellationToken = default);
}