using CommercialNews.Api.Api.Admin.Contracts.Interaction.ArticleInteractionStats.Responses;
using CommercialNews.Api.Api.Common.ErrorHandling;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.Api.Authorization;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.UseCases.ArticleInteractionStats.GetArticleInteractionStats;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GetArticleInteractionStatsApplicationRequest =
    Interaction.Application.Contracts.ArticleInteractionStats.GetArticleInteractionStats.GetArticleInteractionStatsRequestDto;
using GetArticleInteractionStatsApplicationResponse =
    Interaction.Application.Contracts.ArticleInteractionStats.GetArticleInteractionStats.GetArticleInteractionStatsResponseDto;

namespace CommercialNews.Api.Api.Admin.Controllers.Interaction;

[ApiController]
[Route("api/v1/admin/interaction/articles")]
public sealed class AdminInteractionStatsController : ControllerBase
{
    private readonly IGetArticleInteractionStatsUseCase _getArticleInteractionStatsUseCase;

    public AdminInteractionStatsController(
        IGetArticleInteractionStatsUseCase getArticleInteractionStatsUseCase)
    {
        _getArticleInteractionStatsUseCase = getArticleInteractionStatsUseCase
            ?? throw new ArgumentNullException(nameof(getArticleInteractionStatsUseCase));
    }

    [Authorize(Policy = AuthorizationPolicies.InteractionCountersRead)]
    [HttpGet("{articlePublicId}/stats")]
    [ProducesResponseType(
        typeof(ArticleInteractionStatsResponse),
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
    public async Task<IActionResult> GetArticleInteractionStatsAsync(
        [FromRoute] string articlePublicId,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetArticleInteractionStatsApplicationRequest
        {
            ArticlePublicId = articlePublicId
        };

        Result<GetArticleInteractionStatsApplicationResponse> result =
            await _getArticleInteractionStatsUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<ArticleInteractionStatsResponse>.Failure(
                    result.Error!));
        }

        var response = MapResponse(result.Value!);

        return this.ToActionResult(
            Result<ArticleInteractionStatsResponse>.Success(response));
    }

    private static ArticleInteractionStatsResponse MapResponse(
        GetArticleInteractionStatsApplicationResponse item)
    {
        return new ArticleInteractionStatsResponse
        {
            ArticlePublicId = item.ArticlePublicId,
            ViewCount = item.ViewCount,
            LikeCount = item.LikeCount,
            VisibleCommentCount = item.VisibleCommentCount,
            StatsVersion = item.StatsVersion,
            LastMaterializedAtUtc = item.LastMaterializedAtUtc,
            LastPublishedAtUtc = item.LastPublishedAtUtc
        };
    }
}