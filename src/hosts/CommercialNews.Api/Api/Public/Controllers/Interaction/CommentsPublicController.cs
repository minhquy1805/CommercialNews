using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.Api.Api.Public.Contracts.Interaction.Comments.Requests;
using CommercialNews.Api.Api.Public.Contracts.Interaction.Comments.Responses;
using CommercialNews.BuildingBlocks.Abstractions.Execution;
using CommercialNews.BuildingBlocks.Contracts.Common;
using CommercialNews.BuildingBlocks.Results;
using Interaction.Application.Contracts.Comments.Requests;
using Interaction.Application.Contracts.Comments.Responses;
using Interaction.Application.Errors;
using Interaction.Application.UseCases.Comments.CreateComment;
using Interaction.Application.UseCases.Comments.DeleteComment;
using Interaction.Application.UseCases.Comments.GetComments;
using Interaction.Application.UseCases.Comments.UpdateComment;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.Public.Controllers.Interaction;

[ApiController]
[Route("api/v1/interaction")]
public sealed class CommentsPublicController : ControllerBase
{
    private readonly IRequestContext _requestContext;
    private readonly ICreateCommentUseCase _createCommentUseCase;
    private readonly IGetCommentsUseCase _getCommentsUseCase;
    private readonly IUpdateCommentUseCase _updateCommentUseCase;
    private readonly IDeleteCommentUseCase _deleteCommentUseCase;

    public CommentsPublicController(
        IRequestContext requestContext,
        ICreateCommentUseCase createCommentUseCase,
        IGetCommentsUseCase getCommentsUseCase,
        IUpdateCommentUseCase updateCommentUseCase,
        IDeleteCommentUseCase deleteCommentUseCase)
    {
        _requestContext = requestContext;
        _createCommentUseCase = createCommentUseCase;
        _getCommentsUseCase = getCommentsUseCase;
        _updateCommentUseCase = updateCommentUseCase;
        _deleteCommentUseCase = deleteCommentUseCase;
    }

    [HttpGet("articles/{articleId:long}/comments")]
    [ProducesResponseType(typeof(GetCommentsHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCommentsAsync(
        [FromRoute] long articleId,
        [FromQuery] GetCommentsHttpRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetCommentsRequest
        {
            ArticleId = articleId,
            ParentCommentId = request.ParentCommentId,
            Page = request.Page,
            PageSize = request.PageSize,
            SortBy = request.SortBy,
            SortDirection = request.SortDirection
        };

        Result<GetCommentsResponse> result =
            await _getCommentsUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<GetCommentsHttpResponse>.Failure(result.Error!));
        }

        var response = new GetCommentsHttpResponse
        {
            Items = result.Value!.Items
                .Select(static item => new CommentItemHttpResponse
                {
                    CommentId = item.CommentId,
                    ArticleId = item.ArticleId,
                    UserId = item.UserId,
                    ParentCommentId = item.ParentCommentId,
                    Content = item.Content,
                    Status = item.Status,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,
                    EditCount = item.EditCount
                })
                .ToArray(),
            PageInfo = new PageInfo
            {
                Page = result.Value.PageInfo.Page,
                PageSize = result.Value.PageInfo.PageSize,
                TotalItems = result.Value.PageInfo.TotalItems,
                TotalPages = result.Value.PageInfo.TotalPages
            }
        };

        return this.ToActionResult(Result<GetCommentsHttpResponse>.Success(response));
    }

    [HttpPost("articles/{articleId:long}/comments")]
    [ProducesResponseType(typeof(CreateCommentHttpResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateCommentAsync(
        [FromRoute] long articleId,
        [FromBody] CreateCommentHttpRequest request,
        CancellationToken cancellationToken)
    {
        if (!_requestContext.CurrentUserId.HasValue)
        {
            return this.ToActionResult(
                Result<CreateCommentHttpResponse>.Failure(
                    InteractionErrors.Comment.AuthenticationRequired));
        }

        var useCaseRequest = new CreateCommentRequest
        {
            ArticleId = articleId,
            UserId = _requestContext.CurrentUserId.Value,
            ParentCommentId = request.ParentCommentId,
            Content = request.Content
        };

        Result<CreateCommentResponse> result =
            await _createCommentUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<CreateCommentHttpResponse>.Failure(result.Error!));
        }

        var response = new CreateCommentHttpResponse
        {
            CommentId = result.Value!.CommentId,
            CreatedAt = result.Value.CreatedAt
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("comments/{commentId:long}")]
    [ProducesResponseType(typeof(UpdateCommentHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCommentAsync(
        [FromRoute] long commentId,
        [FromBody] UpdateCommentHttpRequest request,
        CancellationToken cancellationToken)
    {
        if (!_requestContext.CurrentUserId.HasValue)
        {
            return this.ToActionResult(
                Result<UpdateCommentHttpResponse>.Failure(
                    InteractionErrors.Comment.AuthenticationRequired));
        }

        var useCaseRequest = new UpdateCommentRequest
        {
            CommentId = commentId,
            UserId = _requestContext.CurrentUserId.Value,
            Content = request.Content
        };

        Result<UpdateCommentResponse> result =
            await _updateCommentUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<UpdateCommentHttpResponse>.Failure(result.Error!));
        }

        var response = new UpdateCommentHttpResponse
        {
            Updated = result.Value!.Updated
        };

        return this.ToActionResult(Result<UpdateCommentHttpResponse>.Success(response));
    }

    [HttpDelete("comments/{commentId:long}")]
    [ProducesResponseType(typeof(DeleteCommentHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCommentAsync(
        [FromRoute] long commentId,
        CancellationToken cancellationToken)
    {
        if (!_requestContext.CurrentUserId.HasValue)
        {
            return this.ToActionResult(
                Result<DeleteCommentHttpResponse>.Failure(
                    InteractionErrors.Comment.AuthenticationRequired));
        }

        var useCaseRequest = new DeleteCommentRequest
        {
            CommentId = commentId,
            UserId = _requestContext.CurrentUserId.Value
        };

        Result<DeleteCommentResponse> result =
            await _deleteCommentUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<DeleteCommentHttpResponse>.Failure(result.Error!));
        }

        var response = new DeleteCommentHttpResponse
        {
            Deleted = result.Value!.Deleted
        };

        return this.ToActionResult(Result<DeleteCommentHttpResponse>.Success(response));
    }
}