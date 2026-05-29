using CommercialNews.Api.Api.Common.ErrorHandling;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.Api.Api.Public.Contracts.Interaction.Views.Responses;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.UseCases.Views.TrackArticleView;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrackArticleViewApplicationRequest =
    Interaction.Application.Contracts.Views.TrackArticleView.TrackArticleViewRequestDto;
using TrackArticleViewApplicationResponse =
    Interaction.Application.Contracts.Views.TrackArticleView.TrackArticleViewResponseDto;

namespace CommercialNews.Api.Api.Public.Controllers.Interaction;

[ApiController]
[Route("api/v1/articles/{articlePublicId}/views")]
public sealed class ArticleViewsController : ControllerBase
{
    private readonly ITrackArticleViewUseCase _trackArticleViewUseCase;

    public ArticleViewsController(
        ITrackArticleViewUseCase trackArticleViewUseCase)
    {
        _trackArticleViewUseCase = trackArticleViewUseCase
            ?? throw new ArgumentNullException(nameof(trackArticleViewUseCase));
    }

    [AllowAnonymous]
    [HttpPost]
    [ProducesResponseType(
        typeof(TrackArticleViewResponse),
        StatusCodes.Status202Accepted)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> TrackArticleViewAsync(
        [FromRoute] string articlePublicId,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new TrackArticleViewApplicationRequest
        {
            ArticlePublicId = articlePublicId
        };

        Result<TrackArticleViewApplicationResponse> result =
            await _trackArticleViewUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<TrackArticleViewResponse>.Failure(result.Error!));
        }

        var value = result.Value!;

        var response = new TrackArticleViewResponse
        {
            Accepted = value.Accepted
        };

        return Accepted(response);
    }
}