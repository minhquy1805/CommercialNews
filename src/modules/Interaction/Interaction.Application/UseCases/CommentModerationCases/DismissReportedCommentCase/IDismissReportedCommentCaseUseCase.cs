using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.CommentModerationCases.DismissReportedCommentCase;

namespace Interaction.Application.UseCases.CommentModerationCases.DismissReportedCommentCase;

public interface IDismissReportedCommentCaseUseCase
{
    Task<Result<DismissReportedCommentCaseResponseDto>> ExecuteAsync(
        DismissReportedCommentCaseRequestDto request,
        CancellationToken cancellationToken = default);
}