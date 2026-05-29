using CommercialNews.Api.Api.Common.ErrorHandling;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.Api.Api.Public.Contracts.Interaction.Likes.Responses;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.UseCases.Likes.GetMyArticleLike;
using Interaction.Application.UseCases.Likes.LikeArticle;
using Interaction.Application.UseCases.Likes.UnlikeArticle;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GetMyArticleLikeApplicationRequest =
    Interaction.Application.Contracts.Likes.GetMyArticleLike.GetMyArticleLikeRequestDto;
using GetMyArticleLikeApplicationResponse =
    Interaction.Application.Contracts.Likes.GetMyArticleLike.GetMyArticleLikeResponseDto;
using LikeArticleApplicationRequest =
    Interaction.Application.Contracts.Likes.LikeArticle.LikeArticleRequestDto;
using LikeArticleApplicationResponse =
    Interaction.Application.Contracts.Likes.LikeArticle.LikeArticleResponseDto;
using UnlikeArticleApplicationRequest =
    Interaction.Application.Contracts.Likes.UnlikeArticle.UnlikeArticleRequestDto;
using UnlikeArticleApplicationResponse =
    Interaction.Application.Contracts.Likes.UnlikeArticle.UnlikeArticleResponseDto;

namespace CommercialNews.Api.Api.Public.Controllers.Interaction;

[ApiController]
[Authorize]
[Route("api/v1/articles/{articlePublicId}")]
public sealed class ArticleLikesController : ControllerBase
{
    private readonly ILikeArticleUseCase _likeArticleUseCase;
    private readonly IUnlikeArticleUseCase _unlikeArticleUseCase;
    private readonly IGetMyArticleLikeUseCase _getMyArticleLikeUseCase;

    public ArticleLikesController(
        ILikeArticleUseCase likeArticleUseCase,
        IUnlikeArticleUseCase unlikeArticleUseCase,
        IGetMyArticleLikeUseCase getMyArticleLikeUseCase)
    {
        _likeArticleUseCase = likeArticleUseCase
            ?? throw new ArgumentNullException(nameof(likeArticleUseCase));

        _unlikeArticleUseCase = unlikeArticleUseCase
            ?? throw new ArgumentNullException(nameof(unlikeArticleUseCase));

        _getMyArticleLikeUseCase = getMyArticleLikeUseCase
            ?? throw new ArgumentNullException(nameof(getMyArticleLikeUseCase));
    }

    [HttpPost("likes")]
    [ProducesResponseType(
        typeof(LikeArticleResponse),
        StatusCodes.Status200OK)]
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
        StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> LikeArticleAsync(
        [FromRoute] string articlePublicId,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new LikeArticleApplicationRequest
        {
            ArticlePublicId = articlePublicId
        };

        Result<LikeArticleApplicationResponse> result =
            await _likeArticleUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<LikeArticleResponse>.Failure(result.Error!));
        }

        var value = result.Value!;

        var response = new LikeArticleResponse
        {
            ArticlePublicId = value.ArticlePublicId,
            Liked = value.Liked,
            Version = value.Version
        };

        return Ok(response);
    }

    [HttpDelete("likes")]
    [ProducesResponseType(
        typeof(UnlikeArticleResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> UnlikeArticleAsync(
        [FromRoute] string articlePublicId,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new UnlikeArticleApplicationRequest
        {
            ArticlePublicId = articlePublicId
        };

        Result<UnlikeArticleApplicationResponse> result =
            await _unlikeArticleUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<UnlikeArticleResponse>.Failure(result.Error!));
        }

        var value = result.Value!;

        var response = new UnlikeArticleResponse
        {
            ArticlePublicId = value.ArticlePublicId,
            Liked = value.Liked,
            Version = value.Version
        };

        return Ok(response);
    }

    [HttpGet("my-like")]
    [ProducesResponseType(
        typeof(GetMyArticleLikeResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyArticleLikeAsync(
        [FromRoute] string articlePublicId,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetMyArticleLikeApplicationRequest
        {
            ArticlePublicId = articlePublicId
        };

        Result<GetMyArticleLikeApplicationResponse> result =
            await _getMyArticleLikeUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<GetMyArticleLikeResponse>.Failure(result.Error!));
        }

        var value = result.Value!;

        var response = new GetMyArticleLikeResponse
        {
            ArticlePublicId = value.ArticlePublicId,
            Liked = value.Liked,
            Version = value.Version
        };

        return Ok(response);
    }
}