using CommercialNews.Api.Api.Common.Contracts;
using CommercialNews.Api.Api.Common.ErrorHandling;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.Api.Api.Public.Contracts.Interaction.Comments.Requests;
using CommercialNews.Api.Api.Public.Contracts.Interaction.Comments.Responses;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.UseCases.Comments.CreateComment;
using Interaction.Application.UseCases.Comments.DeleteOwnComment;
using Interaction.Application.UseCases.Comments.GetPublicComments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CreateCommentApplicationRequest =
    Interaction.Application.Contracts.Comments.CreateComment.CreateCommentRequestDto;
using CreateCommentApplicationResponse =
    Interaction.Application.Contracts.Comments.CreateComment.CreateCommentResponseDto;
using DeleteOwnCommentApplicationRequest =
    Interaction.Application.Contracts.Comments.DeleteOwnComment.DeleteOwnCommentRequestDto;
using DeleteOwnCommentApplicationResponse =
    Interaction.Application.Contracts.Comments.DeleteOwnComment.DeleteOwnCommentResponseDto;
using GetPublicCommentsApplicationRequest =
    Interaction.Application.Contracts.Comments.GetPublicComments.GetPublicCommentsRequestDto;
using GetPublicCommentItemApplicationResponse =
    Interaction.Application.Contracts.Comments.GetPublicComments.GetPublicCommentItemResponseDto;

namespace CommercialNews.Api.Api.Public.Controllers.Interaction;

[ApiController]
[Route("api/v1")]
public sealed class ArticleCommentsController : ControllerBase
{
    private readonly IGetPublicCommentsUseCase _getPublicCommentsUseCase;
    private readonly ICreateCommentUseCase _createCommentUseCase;
    private readonly IDeleteOwnCommentUseCase _deleteOwnCommentUseCase;

    public ArticleCommentsController(
        IGetPublicCommentsUseCase getPublicCommentsUseCase,
        ICreateCommentUseCase createCommentUseCase,
        IDeleteOwnCommentUseCase deleteOwnCommentUseCase)
    {
        _getPublicCommentsUseCase = getPublicCommentsUseCase
            ?? throw new ArgumentNullException(nameof(getPublicCommentsUseCase));

        _createCommentUseCase = createCommentUseCase
            ?? throw new ArgumentNullException(nameof(createCommentUseCase));

        _deleteOwnCommentUseCase = deleteOwnCommentUseCase
            ?? throw new ArgumentNullException(nameof(deleteOwnCommentUseCase));
    }

    [HttpGet("articles/{articlePublicId}/comments")]
    [ProducesResponseType(
        typeof(PagedResponse<PublicCommentItemResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicCommentsAsync(
        [FromRoute] string articlePublicId,
        [FromQuery] GetPublicCommentsRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetPublicCommentsApplicationRequest
        {
            ArticlePublicId = articlePublicId,
            Page = request.Page,
            PageSize = request.PageSize,
            SortDirection = request.SortDirection
        };

        var result = await _getPublicCommentsUseCase.ExecuteAsync(
            useCaseRequest,
            cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<PagedResponse<PublicCommentItemResponse>>.Failure(
                    result.Error!));
        }

        var value = result.Value!;

        var response = new PagedResponse<PublicCommentItemResponse>
        {
            Items = value.Items
                .Select(MapPublicCommentResponse)
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
            Result<PagedResponse<PublicCommentItemResponse>>.Success(response));
    }

    [Authorize]
    [HttpPost("articles/{articlePublicId}/comments")]
    [ProducesResponseType(
        typeof(CreateCommentResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CreateCommentAsync(
        [FromRoute] string articlePublicId,
        [FromBody] CreateCommentRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new CreateCommentApplicationRequest
        {
            ArticlePublicId = articlePublicId,
            Content = request.Content
        };

        Result<CreateCommentApplicationResponse> result =
            await _createCommentUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<CreateCommentResponse>.Failure(result.Error!));
        }

        var value = result.Value!;

        var response = new CreateCommentResponse
        {
            CommentPublicId = value.CommentPublicId,
            ArticlePublicId = value.ArticlePublicId,
            Status = value.Status,
            CreatedAtUtc = value.CreatedAtUtc,
            Version = value.Version
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize]
    [HttpDelete("comments/{commentPublicId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteOwnCommentAsync(
        [FromRoute] string commentPublicId,
        [FromQuery] DeleteOwnCommentRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new DeleteOwnCommentApplicationRequest
        {
            CommentPublicId = commentPublicId,
            ExpectedVersion = request.ExpectedVersion
        };

        Result<DeleteOwnCommentApplicationResponse> result =
            await _deleteOwnCommentUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result.Failure(result.Error!));
        }

        return NoContent();
    }

    private static PublicCommentItemResponse MapPublicCommentResponse(
        GetPublicCommentItemApplicationResponse item)
    {
        return new PublicCommentItemResponse
        {
            CommentPublicId = item.CommentPublicId,
            ArticlePublicId = item.ArticlePublicId,
            Content = item.Content,
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