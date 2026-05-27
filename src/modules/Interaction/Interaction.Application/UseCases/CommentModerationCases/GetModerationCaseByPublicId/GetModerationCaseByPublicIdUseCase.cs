using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.CommentModerationCases.GetModerationCaseByPublicId;
using Interaction.Application.Errors;
using Interaction.Application.Models.Results;
using Interaction.Application.Ports.Persistence;
using Interaction.Application.Validation.CommentModerationCases;

namespace Interaction.Application.UseCases.CommentModerationCases.GetModerationCaseByPublicId;

public sealed class GetModerationCaseByPublicIdUseCase
    : IGetModerationCaseByPublicIdUseCase
{
    private readonly ICommentModerationCaseRepository _moderationCaseRepository;

    public GetModerationCaseByPublicIdUseCase(
        ICommentModerationCaseRepository moderationCaseRepository)
    {
        _moderationCaseRepository = moderationCaseRepository
            ?? throw new ArgumentNullException(nameof(moderationCaseRepository));
    }

    public async Task<Result<GetModerationCaseByPublicIdResponseDto>> ExecuteAsync(
        GetModerationCaseByPublicIdRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError =
            GetModerationCaseByPublicIdValidator.Validate(request);

        if (validationError is not null)
        {
            return Result<GetModerationCaseByPublicIdResponseDto>.Failure(
                validationError);
        }

        var casePublicId =
            GetModerationCaseByPublicIdValidator.NormalizeCasePublicId(
                request.CasePublicId);

        var detail = await _moderationCaseRepository.GetByPublicIdAsync(
            casePublicId,
            cancellationToken);

        if (detail is null)
        {
            return Result<GetModerationCaseByPublicIdResponseDto>.Failure(
                InteractionErrors.CommentModerationCase.NotFound);
        }

        return Result<GetModerationCaseByPublicIdResponseDto>.Success(
            MapResponse(detail));
    }

    private static GetModerationCaseByPublicIdResponseDto MapResponse(
        ModerationCaseDetailResult detail)
    {
        return new GetModerationCaseByPublicIdResponseDto
        {
            CommentModerationCasePublicId = detail.CommentModerationCasePublicId,
            Status = detail.Status,
            Priority = detail.Priority,
            HighestSeverity = detail.HighestSeverity,
            AlertTriggeredAtUtc = detail.AlertTriggeredAtUtc,
            AlertLevel = detail.AlertLevel,
            OpenedAtUtc = detail.OpenedAtUtc,
            ResolvedAtUtc = detail.ResolvedAtUtc,
            ResolutionType = detail.ResolutionType,
            ResolutionReasonCode = detail.ResolutionReasonCode,
            ResolutionNote = detail.ResolutionNote,
            Version = detail.Version,

            Comment = new ModerationCaseCommentResponseDto
            {
                CommentPublicId = detail.Comment.CommentPublicId,
                ArticlePublicId = detail.Comment.ArticlePublicId,
                AuthorUserId = detail.Comment.AuthorUserId,
                Content = detail.Comment.Content,
                Status = detail.Comment.Status,
                Version = detail.Comment.Version
            },

            Reports = detail.Reports
                .Select(MapReportResponse)
                .ToArray()
        };
    }

    private static ModerationCaseReportResponseDto MapReportResponse(
        ModerationCaseReportDetailResult report)
    {
        return new ModerationCaseReportResponseDto
        {
            CommentReportPublicId = report.CommentReportPublicId,
            ReporterUserId = report.ReporterUserId,
            ReasonCode = report.ReasonCode,
            Description = report.Description,
            Status = report.Status,
            CreatedAtUtc = report.CreatedAtUtc
        };
    }
}