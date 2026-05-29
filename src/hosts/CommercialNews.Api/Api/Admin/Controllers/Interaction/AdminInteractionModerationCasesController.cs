using CommercialNews.Api.Api.Admin.Contracts.Interaction.CommentModerationCases.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Interaction.CommentModerationCases.Responses;
using CommercialNews.Api.Api.Common.Contracts;
using CommercialNews.Api.Api.Common.ErrorHandling;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.Api.Authorization;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.UseCases.CommentModerationCases.DismissReportedCommentCase;
using Interaction.Application.UseCases.CommentModerationCases.GetModerationCaseByPublicId;
using Interaction.Application.UseCases.CommentModerationCases.GetModerationCases;
using Interaction.Application.UseCases.CommentModerationCases.HideReportedComment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DismissReportedCommentCaseApplicationRequest =
    Interaction.Application.Contracts.CommentModerationCases.DismissReportedCommentCase.DismissReportedCommentCaseRequestDto;
using DismissReportedCommentCaseApplicationResponse =
    Interaction.Application.Contracts.CommentModerationCases.DismissReportedCommentCase.DismissReportedCommentCaseResponseDto;
using GetModerationCaseByPublicIdApplicationRequest =
    Interaction.Application.Contracts.CommentModerationCases.GetModerationCaseByPublicId.GetModerationCaseByPublicIdRequestDto;
using GetModerationCaseByPublicIdApplicationResponse =
    Interaction.Application.Contracts.CommentModerationCases.GetModerationCaseByPublicId.GetModerationCaseByPublicIdResponseDto;
using GetModerationCaseItemApplicationResponse =
    Interaction.Application.Contracts.CommentModerationCases.GetModerationCases.GetModerationCaseItemResponseDto;
using GetModerationCasesApplicationRequest =
    Interaction.Application.Contracts.CommentModerationCases.GetModerationCases.GetModerationCasesRequestDto;
using HideReportedCommentApplicationRequest =
    Interaction.Application.Contracts.CommentModerationCases.HideReportedComment.HideReportedCommentRequestDto;
using HideReportedCommentApplicationResponse =
    Interaction.Application.Contracts.CommentModerationCases.HideReportedComment.HideReportedCommentResponseDto;
using ModerationCaseCommentApplicationResponse =
    Interaction.Application.Contracts.CommentModerationCases.GetModerationCaseByPublicId.ModerationCaseCommentResponseDto;
using ModerationCaseReportApplicationResponse =
    Interaction.Application.Contracts.CommentModerationCases.GetModerationCaseByPublicId.ModerationCaseReportResponseDto;

namespace CommercialNews.Api.Api.Admin.Controllers.Interaction;

[ApiController]
[Route("api/v1/admin/interaction/comment-moderation-cases")]
public sealed class AdminInteractionModerationCasesController : ControllerBase
{
    private readonly IGetModerationCasesUseCase _getModerationCasesUseCase;
    private readonly IGetModerationCaseByPublicIdUseCase _getModerationCaseByPublicIdUseCase;
    private readonly IDismissReportedCommentCaseUseCase _dismissReportedCommentCaseUseCase;
    private readonly IHideReportedCommentUseCase _hideReportedCommentUseCase;

    public AdminInteractionModerationCasesController(
        IGetModerationCasesUseCase getModerationCasesUseCase,
        IGetModerationCaseByPublicIdUseCase getModerationCaseByPublicIdUseCase,
        IDismissReportedCommentCaseUseCase dismissReportedCommentCaseUseCase,
        IHideReportedCommentUseCase hideReportedCommentUseCase)
    {
        _getModerationCasesUseCase = getModerationCasesUseCase
            ?? throw new ArgumentNullException(nameof(getModerationCasesUseCase));

        _getModerationCaseByPublicIdUseCase = getModerationCaseByPublicIdUseCase
            ?? throw new ArgumentNullException(nameof(getModerationCaseByPublicIdUseCase));

        _dismissReportedCommentCaseUseCase = dismissReportedCommentCaseUseCase
            ?? throw new ArgumentNullException(nameof(dismissReportedCommentCaseUseCase));

        _hideReportedCommentUseCase = hideReportedCommentUseCase
            ?? throw new ArgumentNullException(nameof(hideReportedCommentUseCase));
    }

    [Authorize(Policy = AuthorizationPolicies.InteractionCommentReportsRead)]
    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponse<ModerationCaseItemResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetModerationCasesAsync(
        [FromQuery] GetModerationCasesRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetModerationCasesApplicationRequest
        {
            Status = request.Status,
            Priority = request.Priority,
            ArticlePublicId = request.ArticlePublicId,
            CommentPublicId = request.CommentPublicId,
            AlertTriggered = request.AlertTriggered,
            Page = request.Page,
            PageSize = request.PageSize
        };

        var result = await _getModerationCasesUseCase.ExecuteAsync(
            useCaseRequest,
            cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<PagedResponse<ModerationCaseItemResponse>>.Failure(
                    result.Error!));
        }

        var value = result.Value!;

        var response = new PagedResponse<ModerationCaseItemResponse>
        {
            Items = value.Items
                .Select(MapModerationCaseItemResponse)
                .ToArray(),

            PageInfo = new PageInfo
            {
                Page = value.Page,
                PageSize = value.PageSize,
                TotalItems = value.TotalItems,
                TotalPages = CalculateTotalPages(
                    value.TotalItems,
                    value.PageSize)
            }
        };

        return this.ToActionResult(
            Result<PagedResponse<ModerationCaseItemResponse>>.Success(response));
    }

    [Authorize(Policy = AuthorizationPolicies.InteractionCommentReportsRead)]
    [HttpGet("{casePublicId}")]
    [ProducesResponseType(
        typeof(ModerationCaseDetailResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetModerationCaseByPublicIdAsync(
        [FromRoute] string casePublicId,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetModerationCaseByPublicIdApplicationRequest
        {
            CasePublicId = casePublicId
        };

        Result<GetModerationCaseByPublicIdApplicationResponse> result =
            await _getModerationCaseByPublicIdUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<ModerationCaseDetailResponse>.Failure(result.Error!));
        }

        var response = MapModerationCaseDetailResponse(result.Value!);

        return this.ToActionResult(
            Result<ModerationCaseDetailResponse>.Success(response));
    }

    [Authorize(Policy = AuthorizationPolicies.InteractionCommentReportsResolve)]
    [HttpPost("{casePublicId}/dismiss")]
    [ProducesResponseType(
        typeof(DismissReportedCommentCaseResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DismissReportedCommentCaseAsync(
        [FromRoute] string casePublicId,
        [FromBody] DismissReportedCommentCaseRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new DismissReportedCommentCaseApplicationRequest
        {
            CasePublicId = casePublicId,
            ExpectedCaseVersion = request.ExpectedCaseVersion,
            ReasonCode = request.ReasonCode,
            Note = request.Note
        };

        Result<DismissReportedCommentCaseApplicationResponse> result =
            await _dismissReportedCommentCaseUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<DismissReportedCommentCaseResponse>.Failure(
                    result.Error!));
        }

        var value = result.Value!;

        var response = new DismissReportedCommentCaseResponse
        {
            CommentModerationCasePublicId =
                value.CommentModerationCasePublicId,
            Status = value.Status,
            ResolvedAtUtc = value.ResolvedAtUtc,
            Version = value.Version
        };

        return this.ToActionResult(
            Result<DismissReportedCommentCaseResponse>.Success(response));
    }

    [Authorize(Policy = AuthorizationPolicies.InteractionCommentReportsHideComment)]
    [HttpPost("{casePublicId}/hide-comment")]
    [ProducesResponseType(
        typeof(HideReportedCommentResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> HideReportedCommentAsync(
        [FromRoute] string casePublicId,
        [FromBody] HideReportedCommentRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new HideReportedCommentApplicationRequest
        {
            CasePublicId = casePublicId,
            ExpectedCaseVersion = request.ExpectedCaseVersion,
            ExpectedCommentVersion = request.ExpectedCommentVersion,
            ReasonCode = request.ReasonCode,
            Note = request.Note
        };

        Result<HideReportedCommentApplicationResponse> result =
            await _hideReportedCommentUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<HideReportedCommentResponse>.Failure(result.Error!));
        }

        var value = result.Value!;

        var response = new HideReportedCommentResponse
        {
            CommentModerationCasePublicId =
                value.CommentModerationCasePublicId,
            CaseStatus = value.CaseStatus,
            CaseVersion = value.CaseVersion,
            CommentPublicId = value.CommentPublicId,
            CommentStatus = value.CommentStatus,
            CommentVersion = value.CommentVersion,
            ResolvedAtUtc = value.ResolvedAtUtc,
            HiddenAtUtc = value.HiddenAtUtc
        };

        return this.ToActionResult(
            Result<HideReportedCommentResponse>.Success(response));
    }

    private static ModerationCaseItemResponse MapModerationCaseItemResponse(
        GetModerationCaseItemApplicationResponse item)
    {
        return new ModerationCaseItemResponse
        {
            CommentModerationCasePublicId =
                item.CommentModerationCasePublicId,
            CommentPublicId = item.CommentPublicId,
            ArticlePublicId = item.ArticlePublicId,
            Status = item.Status,
            Priority = item.Priority,
            HighestSeverity = item.HighestSeverity,
            PendingReportCount = item.PendingReportCount,
            DistinctReporterCount = item.DistinctReporterCount,
            AlertTriggered = item.AlertTriggered,
            AlertTriggeredAtUtc = item.AlertTriggeredAtUtc,
            AlertLevel = item.AlertLevel,
            OpenedAtUtc = item.OpenedAtUtc,
            Version = item.Version
        };
    }

    private static ModerationCaseDetailResponse MapModerationCaseDetailResponse(
        GetModerationCaseByPublicIdApplicationResponse item)
    {
        return new ModerationCaseDetailResponse
        {
            CommentModerationCasePublicId =
                item.CommentModerationCasePublicId,
            Status = item.Status,
            Priority = item.Priority,
            HighestSeverity = item.HighestSeverity,
            AlertTriggeredAtUtc = item.AlertTriggeredAtUtc,
            AlertLevel = item.AlertLevel,
            OpenedAtUtc = item.OpenedAtUtc,
            ResolvedAtUtc = item.ResolvedAtUtc,
            ResolutionType = item.ResolutionType,
            ResolutionReasonCode = item.ResolutionReasonCode,
            ResolutionNote = item.ResolutionNote,
            Version = item.Version,
            Comment = MapModerationCaseCommentResponse(item.Comment),
            Reports = item.Reports
                .Select(MapModerationCaseReportResponse)
                .ToArray()
        };
    }

    private static ModerationCaseCommentResponse MapModerationCaseCommentResponse(
        ModerationCaseCommentApplicationResponse item)
    {
        return new ModerationCaseCommentResponse
        {
            CommentPublicId = item.CommentPublicId,
            ArticlePublicId = item.ArticlePublicId,
            AuthorUserId = item.AuthorUserId,
            Content = item.Content,
            Status = item.Status,
            Version = item.Version
        };
    }

    private static ModerationCaseReportResponse MapModerationCaseReportResponse(
        ModerationCaseReportApplicationResponse item)
    {
        return new ModerationCaseReportResponse
        {
            CommentReportPublicId = item.CommentReportPublicId,
            ReporterUserId = item.ReporterUserId,
            ReasonCode = item.ReasonCode,
            Description = item.Description,
            Status = item.Status,
            CreatedAtUtc = item.CreatedAtUtc
        };
    }

    private static int CalculateTotalPages(
        long totalItems,
        int pageSize)
    {
        if (pageSize <= 0)
        {
            return 0;
        }

        return (int)Math.Ceiling(totalItems / (double)pageSize);
    }
}