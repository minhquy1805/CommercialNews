using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Comments.CreateComment;

namespace Interaction.Application.UseCases.Comments.CreateComment;

public interface ICreateCommentUseCase
{
    Task<Result<CreateCommentResponseDto>> ExecuteAsync(
        CreateCommentRequestDto request,
        CancellationToken cancellationToken = default);
}