using CommercialNews.Api.Api.Common.RequestContext;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.Api.Api.Public.Contracts.Interaction.Counters.Responses;
using CommercialNews.Api.Api.Public.Contracts.Interaction.Likes.Responses;
using CommercialNews.Api.Api.Public.Contracts.Interaction.Views.Requests;
using CommercialNews.Api.Api.Public.Contracts.Interaction.Views.Responses;
using CommercialNews.BuildingBlocks.Abstractions.Execution;
using CommercialNews.BuildingBlocks.Contracts.Common;
using CommercialNews.BuildingBlocks.Results;
using Interaction.Application.Contracts.Counters.Requests;
using Interaction.Application.Contracts.Counters.Responses;
using Interaction.Application.Contracts.Likes.Requests;
using Interaction.Application.Contracts.Likes.Responses;
using Interaction.Application.Contracts.Views.Requests;
using Interaction.Application.Contracts.Views.Responses;
using Interaction.Application.Errors;
using Interaction.Application.UseCases.GetArticleCounters;
using Interaction.Application.UseCases.LikeArticle;
using Interaction.Application.UseCases.TrackArticleView;
using Interaction.Application.UseCases.UnlikeArticle;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.Public.Controllers.Interaction;

[ApiController]
[Route("api/v1/interaction")]
public sealed class InteractionPublicController : ControllerBase
{
    private readonly IRequestContext _requestContext;
    private readonly ITrackArticleViewUseCase _trackArticleViewUseCase;
    private readonly ILikeArticleUseCase _likeArticleUseCase;
    private readonly IUnlikeArticleUseCase _unlikeArticleUseCase;
    private readonly IGetArticleCountersUseCase _getArticleCountersUseCase;

    public InteractionPublicController(
        IRequestContext requestContext,
        ITrackArticleViewUseCase trackArticleViewUseCase,
        ILikeArticleUseCase likeArticleUseCase,
        IUnlikeArticleUseCase unlikeArticleUseCase,
        IGetArticleCountersUseCase getArticleCountersUseCase)
    {
        _requestContext = requestContext;
        _trackArticleViewUseCase = trackArticleViewUseCase;
        _likeArticleUseCase = likeArticleUseCase;
        _unlikeArticleUseCase = unlikeArticleUseCase;
        _getArticleCountersUseCase = getArticleCountersUseCase;
    }

    [HttpPost("articles/{articleId:long}/views")]
    [ProducesResponseType(typeof(TrackArticleViewHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TrackArticleViewAsync(
        [FromRoute] long articleId,
        [FromBody] TrackArticleViewHttpRequest? request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new TrackArticleViewRequest
        {
            ArticleId = articleId,
            UserId = _requestContext.CurrentUserId,
            VisitorKey = request?.VisitorKey,
            IpAddress = _requestContext.IpAddress,
            UserAgent = _requestContext.UserAgent
        };

        Result<TrackArticleViewResponse> result =
            await _trackArticleViewUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<TrackArticleViewHttpResponse>.Failure(result.Error!));
        }

        var response = new TrackArticleViewHttpResponse
        {
            Accepted = result.Value!.Accepted
        };

        return this.ToActionResult(Result<TrackArticleViewHttpResponse>.Success(response));
    }

    [HttpPost("articles/{articleId:long}/likes")]
    [ProducesResponseType(typeof(LikeArticleHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LikeArticleAsync(
        [FromRoute] long articleId,
        CancellationToken cancellationToken)
    {
        if (!_requestContext.CurrentUserId.HasValue)
        {
            return this.ToActionResult(
                Result<LikeArticleHttpResponse>.Failure(
                    InteractionErrors.Like.AuthenticationRequired));
        }

        var useCaseRequest = new LikeArticleRequest
        {
            ArticleId = articleId,
            UserId = _requestContext.CurrentUserId.Value
        };

        Result<LikeArticleResponse> result =
            await _likeArticleUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<LikeArticleHttpResponse>.Failure(result.Error!));
        }

        var response = new LikeArticleHttpResponse
        {
            Liked = result.Value!.Liked
        };

        return this.ToActionResult(Result<LikeArticleHttpResponse>.Success(response));
    }

    [HttpDelete("articles/{articleId:long}/likes")]
    [ProducesResponseType(typeof(UnlikeArticleHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UnlikeArticleAsync(
        [FromRoute] long articleId,
        CancellationToken cancellationToken)
    {
        if (!_requestContext.CurrentUserId.HasValue)
        {
            return this.ToActionResult(
                Result<UnlikeArticleHttpResponse>.Failure(
                    InteractionErrors.Like.AuthenticationRequired));
        }

        var useCaseRequest = new UnlikeArticleRequest
        {
            ArticleId = articleId,
            UserId = _requestContext.CurrentUserId.Value
        };

        Result<UnlikeArticleResponse> result =
            await _unlikeArticleUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<UnlikeArticleHttpResponse>.Failure(result.Error!));
        }

        var response = new UnlikeArticleHttpResponse
        {
            Liked = result.Value!.Liked
        };

        return this.ToActionResult(Result<UnlikeArticleHttpResponse>.Success(response));
    }

    [HttpGet("articles/{articleId:long}/counters")]
    [ProducesResponseType(typeof(GetArticleCountersHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetArticleCountersAsync(
        [FromRoute] long articleId,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetArticleCountersRequest
        {
            ArticleId = articleId
        };

        Result<GetArticleCountersResponse> result =
            await _getArticleCountersUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<GetArticleCountersHttpResponse>.Failure(result.Error!));
        }

        var response = new GetArticleCountersHttpResponse
        {
            ArticleId = result.Value!.ArticleId,
            Views = result.Value.Views,
            Likes = result.Value.Likes,
            Comments = result.Value.Comments,
            Partial = result.Value.Partial
        };

        return this.ToActionResult(Result<GetArticleCountersHttpResponse>.Success(response));
    }
}