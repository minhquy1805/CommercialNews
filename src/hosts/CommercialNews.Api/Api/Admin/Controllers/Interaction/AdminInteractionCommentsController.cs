using CommercialNews.Api.Api.Admin.Contracts.Interaction.Comments.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Interaction.Comments.Responses;
using CommercialNews.Api.Api.Common.Contracts;
using CommercialNews.Api.Api.Common.ErrorHandling;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.Api.Authorization;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.UseCases.Comments.GetAdminCommentByPublicId;
using Interaction.Application.UseCases.Comments.GetAdminComments;
using Interaction.Application.UseCases.Comments.GetCommentModerationHistory;
using Interaction.Application.UseCases.Comments.HideComment;
using Interaction.Application.UseCases.Comments.RestoreComment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GetAdminCommentByPublicIdApplicationRequest =
    Interaction.Application.Contracts.Comments.GetAdminCommentByPublicId.GetAdminCommentByPublicIdRequestDto;
using GetAdminCommentByPublicIdApplicationResponse =
    Interaction.Application.Contracts.Comments.GetAdminCommentByPublicId.GetAdminCommentByPublicIdResponseDto;
using GetAdminCommentsApplicationRequest =
    Interaction.Application.Contracts.Comments.GetAdminComments.GetAdminCommentsRequestDto;
using GetAdminCommentItemApplicationResponse =
    Interaction.Application.Contracts.Comments.GetAdminComments.GetAdminCommentItemResponseDto;
using GetCommentModerationHistoryApplicationRequest =
    Interaction.Application.Contracts.Comments.GetCommentModerationHistory.GetCommentModerationHistoryRequestDto;
using GetCommentModerationHistoryItemApplicationResponse =
    Interaction.Application.Contracts.Comments.GetCommentModerationHistory.GetCommentModerationHistoryItemResponseDto;
using HideCommentApplicationRequest =
    Interaction.Application.Contracts.Comments.HideComment.HideCommentRequestDto;
using HideCommentApplicationResponse =
    Interaction.Application.Contracts.Comments.HideComment.HideCommentResponseDto;
using RestoreCommentApplicationRequest =
    Interaction.Application.Contracts.Comments.RestoreComment.RestoreCommentRequestDto;
using RestoreCommentApplicationResponse =
    Interaction.Application.Contracts.Comments.RestoreComment.RestoreCommentResponseDto;

namespace CommercialNews.Api.Api.Admin.Controllers.Interaction;

[ApiController]
[Route("api/v1/admin/interaction/comments")]
public sealed class AdminInteractionCommentsController : ControllerBase
{
    private readonly IGetAdminCommentsUseCase _getAdminCommentsUseCase;
    private readonly IGetAdminCommentByPublicIdUseCase _getAdminCommentByPublicIdUseCase;
    private readonly IHideCommentUseCase _hideCommentUseCase;
    private readonly IRestoreCommentUseCase _restoreCommentUseCase;
    private readonly IGetCommentModerationHistoryUseCase _getCommentModerationHistoryUseCase;

    public AdminInteractionCommentsController(
        IGetAdminCommentsUseCase getAdminCommentsUseCase,
        IGetAdminCommentByPublicIdUseCase getAdminCommentByPublicIdUseCase,
        IHideCommentUseCase hideCommentUseCase,
        IRestoreCommentUseCase restoreCommentUseCase,
        IGetCommentModerationHistoryUseCase getCommentModerationHistoryUseCase)
    {
        _getAdminCommentsUseCase = getAdminCommentsUseCase
            ?? throw new ArgumentNullException(nameof(getAdminCommentsUseCase));

        _getAdminCommentByPublicIdUseCase = getAdminCommentByPublicIdUseCase
            ?? throw new ArgumentNullException(nameof(getAdminCommentByPublicIdUseCase));

        _hideCommentUseCase = hideCommentUseCase
            ?? throw new ArgumentNullException(nameof(hideCommentUseCase));

        _restoreCommentUseCase = restoreCommentUseCase
            ?? throw new ArgumentNullException(nameof(restoreCommentUseCase));

        _getCommentModerationHistoryUseCase = getCommentModerationHistoryUseCase
            ?? throw new ArgumentNullException(nameof(getCommentModerationHistoryUseCase));
    }

    [Authorize(Policy = AuthorizationPolicies.InteractionCommentsRead)]
    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponse<AdminCommentItemResponse>),
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
    public async Task<IActionResult> GetAdminCommentsAsync(
        [FromQuery] GetAdminCommentsRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetAdminCommentsApplicationRequest
        {
            Status = request.Status,
            ArticlePublicId = request.ArticlePublicId,
            AuthorUserId = request.AuthorUserId,
            Page = request.Page,
            PageSize = request.PageSize
        };

        var result = await _getAdminCommentsUseCase.ExecuteAsync(
            useCaseRequest,
            cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<PagedResponse<AdminCommentItemResponse>>.Failure(
                    result.Error!));
        }

        var value = result.Value!;

        var response = new PagedResponse<AdminCommentItemResponse>
        {
            Items = value.Items
                .Select(MapAdminCommentItemResponse)
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
            Result<PagedResponse<AdminCommentItemResponse>>.Success(response));
    }

    [Authorize(Policy = AuthorizationPolicies.InteractionCommentsRead)]
    [HttpGet("{commentPublicId}")]
    [ProducesResponseType(
        typeof(AdminCommentDetailResponse),
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
    public async Task<IActionResult> GetAdminCommentByPublicIdAsync(
        [FromRoute] string commentPublicId,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetAdminCommentByPublicIdApplicationRequest
        {
            CommentPublicId = commentPublicId
        };

        Result<GetAdminCommentByPublicIdApplicationResponse> result =
            await _getAdminCommentByPublicIdUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<AdminCommentDetailResponse>.Failure(result.Error!));
        }

        var response = MapAdminCommentDetailResponse(result.Value!);

        return this.ToActionResult(
            Result<AdminCommentDetailResponse>.Success(response));
    }

    [Authorize(Policy = AuthorizationPolicies.InteractionCommentsModerate)]
    [HttpPost("{commentPublicId}/hide")]
    [ProducesResponseType(
        typeof(HideCommentResponse),
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
    public async Task<IActionResult> HideCommentAsync(
        [FromRoute] string commentPublicId,
        [FromBody] HideCommentRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new HideCommentApplicationRequest
        {
            CommentPublicId = commentPublicId,
            ExpectedVersion = request.ExpectedVersion,
            ReasonCode = request.ReasonCode,
            Note = request.Note
        };

        Result<HideCommentApplicationResponse> result =
            await _hideCommentUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<HideCommentResponse>.Failure(result.Error!));
        }

        var value = result.Value!;

        var response = new HideCommentResponse
        {
            CommentPublicId = value.CommentPublicId,
            Status = value.Status,
            Version = value.Version
        };

        return this.ToActionResult(
            Result<HideCommentResponse>.Success(response));
    }

    [Authorize(Policy = AuthorizationPolicies.InteractionCommentsModerate)]
    [HttpPost("{commentPublicId}/restore")]
    [ProducesResponseType(
        typeof(RestoreCommentResponse),
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
    public async Task<IActionResult> RestoreCommentAsync(
        [FromRoute] string commentPublicId,
        [FromBody] RestoreCommentRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new RestoreCommentApplicationRequest
        {
            CommentPublicId = commentPublicId,
            ExpectedVersion = request.ExpectedVersion,
            Note = request.Note
        };

        Result<RestoreCommentApplicationResponse> result =
            await _restoreCommentUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<RestoreCommentResponse>.Failure(result.Error!));
        }

        var value = result.Value!;

        var response = new RestoreCommentResponse
        {
            CommentPublicId = value.CommentPublicId,
            Status = value.Status,
            Version = value.Version
        };

        return this.ToActionResult(
            Result<RestoreCommentResponse>.Success(response));
    }

    [Authorize(Policy = AuthorizationPolicies.InteractionCommentsRead)]
    [HttpGet("{commentPublicId}/moderation-history")]
    [ProducesResponseType(
        typeof(PagedResponse<CommentModerationHistoryItemResponse>),
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
    public async Task<IActionResult> GetCommentModerationHistoryAsync(
        [FromRoute] string commentPublicId,
        [FromQuery] GetCommentModerationHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetCommentModerationHistoryApplicationRequest
        {
            CommentPublicId = commentPublicId,
            Page = request.Page,
            PageSize = request.PageSize
        };

        var result = await _getCommentModerationHistoryUseCase.ExecuteAsync(
            useCaseRequest,
            cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<PagedResponse<CommentModerationHistoryItemResponse>>.Failure(
                    result.Error!));
        }

        var value = result.Value!;

        var response = new PagedResponse<CommentModerationHistoryItemResponse>
        {
            Items = value.Items
                .Select(MapModerationHistoryItemResponse)
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
            Result<PagedResponse<CommentModerationHistoryItemResponse>>.Success(
                response));
    }

    private static AdminCommentItemResponse MapAdminCommentItemResponse(
        GetAdminCommentItemApplicationResponse item)
    {
        return new AdminCommentItemResponse
        {
            CommentPublicId = item.CommentPublicId,
            ArticlePublicId = item.ArticlePublicId,
            AuthorUserId = item.AuthorUserId,
            Content = item.Content,
            Status = item.Status,
            ParentCommentPublicId = item.ParentCommentPublicId,
            CreatedAtUtc = item.CreatedAtUtc,
            UpdatedAtUtc = item.UpdatedAtUtc,
            DeletedAtUtc = item.DeletedAtUtc,
            Version = item.Version
        };
    }

    private static AdminCommentDetailResponse MapAdminCommentDetailResponse(
        GetAdminCommentByPublicIdApplicationResponse item)
    {
        return new AdminCommentDetailResponse
        {
            CommentPublicId = item.CommentPublicId,
            ArticlePublicId = item.ArticlePublicId,
            AuthorUserId = item.AuthorUserId,
            Content = item.Content,
            Status = item.Status,

            /*
             * Reply comments are not supported in Interaction V1.
             * Do not expose Application's internal ParentCommentId through HTTP.
             */
            ParentCommentPublicId = null,

            CreatedAtUtc = item.CreatedAtUtc,
            UpdatedAtUtc = item.UpdatedAtUtc,
            DeletedAtUtc = item.DeletedAtUtc,
            Version = item.Version
        };
    }

    private static CommentModerationHistoryItemResponse MapModerationHistoryItemResponse(
        GetCommentModerationHistoryItemApplicationResponse item)
    {
        return new CommentModerationHistoryItemResponse
        {
            HistoryPublicId = item.HistoryPublicId,
            CommentPublicId = item.CommentPublicId,
            CommentModerationCasePublicId = item.CommentModerationCasePublicId,
            ActionType = item.ActionType,
            FromStatus = item.FromStatus,
            ToStatus = item.ToStatus,
            ActorUserId = item.ActorUserId,
            ActorType = item.ActorType,
            ReasonCode = item.ReasonCode,
            Note = item.Note,
            OccurredAtUtc = item.OccurredAtUtc,
            CorrelationId = item.CorrelationId
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