using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Comments.HideComment;

namespace Interaction.Application.UseCases.Comments.HideComment;

public interface IHideCommentUseCase
{
    Task<Result<HideCommentResponseDto>> ExecuteAsync(
        HideCommentRequestDto request,
        CancellationToken cancellationToken = default);
}