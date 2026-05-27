using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.CommentReports.CreateCommentReport;

namespace Interaction.Application.UseCases.CommentReports.CreateCommentReport;

public interface ICreateCommentReportUseCase
{
    Task<Result<CreateCommentReportResponseDto>> ExecuteAsync(
        CreateCommentReportRequestDto request,
        CancellationToken cancellationToken = default);
}