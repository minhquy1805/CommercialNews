using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Comments.Requests;
using Interaction.Application.Contracts.Comments.Responses;

namespace Interaction.Application.UseCases.Comments.CreateComment;

public interface ICreateCommentUseCase
{
    Task<Result<CreateCommentResponse>> ExecuteAsync(
        CreateCommentRequest request,
        CancellationToken cancellationToken = default);
}