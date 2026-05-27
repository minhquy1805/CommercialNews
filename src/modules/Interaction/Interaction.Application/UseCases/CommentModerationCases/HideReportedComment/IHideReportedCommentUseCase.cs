using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.CommentModerationCases.HideReportedComment;

namespace Interaction.Application.UseCases.CommentModerationCases.HideReportedComment;

public interface IHideReportedCommentUseCase
{
    Task<Result<HideReportedCommentResponseDto>> ExecuteAsync(
        HideReportedCommentRequestDto request,
        CancellationToken cancellationToken = default);
}