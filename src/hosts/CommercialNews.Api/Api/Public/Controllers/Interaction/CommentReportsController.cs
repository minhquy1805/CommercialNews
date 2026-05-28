using CommercialNews.Api.Api.Common.ErrorHandling;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.Api.Api.Public.Contracts.Interaction.CommentReports.Requests;
using CommercialNews.Api.Api.Public.Contracts.Interaction.CommentReports.Responses;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.UseCases.CommentReports.CreateCommentReport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CreateCommentReportApplicationRequest =
    Interaction.Application.Contracts.CommentReports.CreateCommentReport.CreateCommentReportRequestDto;
using CreateCommentReportApplicationResponse =
    Interaction.Application.Contracts.CommentReports.CreateCommentReport.CreateCommentReportResponseDto;

namespace CommercialNews.Api.Api.Public.Controllers.Interaction;

[ApiController]
[Authorize]
[Route("api/v1/comments/{commentPublicId}/reports")]
public sealed class CommentReportsController : ControllerBase
{
    private readonly ICreateCommentReportUseCase _createCommentReportUseCase;

    public CommentReportsController(
        ICreateCommentReportUseCase createCommentReportUseCase)
    {
        _createCommentReportUseCase = createCommentReportUseCase
            ?? throw new ArgumentNullException(nameof(createCommentReportUseCase));
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(CreateCommentReportResponse),
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
    public async Task<IActionResult> CreateCommentReportAsync(
        [FromRoute] string commentPublicId,
        [FromBody] CreateCommentReportRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new CreateCommentReportApplicationRequest
        {
            CommentPublicId = commentPublicId,
            ReasonCode = request.ReasonCode,
            Description = request.Description
        };

        Result<CreateCommentReportApplicationResponse> result =
            await _createCommentReportUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<CreateCommentReportResponse>.Failure(result.Error!));
        }

        var value = result.Value!;

        var response = new CreateCommentReportResponse
        {
            CommentReportPublicId = value.CommentReportPublicId,
            CommentPublicId = value.CommentPublicId,
            Status = value.Status,
            CreatedAtUtc = value.CreatedAtUtc
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }
}