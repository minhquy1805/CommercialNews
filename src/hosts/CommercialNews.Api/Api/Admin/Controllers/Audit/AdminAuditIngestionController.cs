using Audit.Application.Models.Queries.Ingestion;
using Audit.Application.Models.Results.Ingestion;
using CommercialNews.Api.Api.Admin.Contracts.Audit.Common;
using CommercialNews.Api.Api.Admin.Contracts.Audit.Ingestion.Mapping;
using CommercialNews.Api.Api.Admin.Contracts.Audit.Ingestion.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Audit.Ingestion.Responses;
using CommercialNews.Api.Api.Common.ErrorHandling;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.Api.Authorization;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.Admin.Controllers.Audit;

[ApiController]
[Route("api/v1/admin/audit/ingestions")]
public sealed class AdminAuditIngestionController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminAuditIngestionController(
        IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.AuditIngestionRead)]
    [ProducesResponseType(typeof(GetAuditIngestionListHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetIngestionAsync(
        [FromQuery] GetAuditIngestionListHttpRequest request,
        CancellationToken cancellationToken)
    {
        Result<PagedQueryResult<AuditIngestionListItemResult>> result =
            await _mediator.Send(
                AuditIngestionQueryMapper.ToQuery(request),
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<GetAuditIngestionListHttpResponse>.Failure(result.Error!));
        }

        var response = new GetAuditIngestionListHttpResponse
        {
            Items = result.Value.Items
                .Select(AuditIngestionHttpMapper.ToListItem)
                .ToArray(),
            PageInfo = PageInfoHttpMapper.ToPageInfo(result.Value)
        };

        return this.ToActionResult(
            Result<GetAuditIngestionListHttpResponse>.Success(response));
    }

    [HttpGet("failed")]
    [Authorize(Policy = AuthorizationPolicies.AuditIngestionRead)]
    [ProducesResponseType(typeof(GetFailedAuditIngestionListHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetFailedIngestionAsync(
        [FromQuery] GetFailedAuditIngestionListHttpRequest request,
        CancellationToken cancellationToken)
    {
        Result<PagedQueryResult<AuditIngestionListItemResult>> result =
            await _mediator.Send(
                AuditIngestionQueryMapper.ToQuery(request),
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<GetFailedAuditIngestionListHttpResponse>.Failure(result.Error!));
        }

        var response = new GetFailedAuditIngestionListHttpResponse
        {
            Items = result.Value.Items
                .Select(AuditIngestionHttpMapper.ToListItem)
                .ToArray(),
            PageInfo = PageInfoHttpMapper.ToPageInfo(result.Value)
        };

        return this.ToActionResult(
            Result<GetFailedAuditIngestionListHttpResponse>.Success(response));
    }

    [HttpGet("{publicId}")]
    [Authorize(Policy = AuthorizationPolicies.AuditIngestionReadDetail)]
    [ProducesResponseType(typeof(AuditIngestionDetailHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetIngestionByPublicIdAsync(
        [FromRoute] string publicId,
        CancellationToken cancellationToken)
    {
        return GetIngestionDetailAsync(
            new GetAuditIngestionDetailQuery(publicId),
            cancellationToken);
    }

    [HttpGet("by-message/{messageId}")]
    [Authorize(Policy = AuthorizationPolicies.AuditIngestionReadDetail)]
    [ProducesResponseType(typeof(AuditIngestionDetailHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetIngestionByMessageIdAsync(
        [FromRoute] string messageId,
        CancellationToken cancellationToken)
    {
        return GetIngestionDetailAsync(
            new GetAuditIngestionByMessageIdQuery(messageId),
            cancellationToken);
    }

    private async Task<IActionResult> GetIngestionDetailAsync<TQuery>(
        TQuery query,
        CancellationToken cancellationToken)
        where TQuery : IRequest<Result<AuditIngestionDetailResult>>
    {
        Result<AuditIngestionDetailResult> result =
            await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<AuditIngestionDetailHttpResponse>.Failure(result.Error!));
        }

        return this.ToActionResult(
            Result<AuditIngestionDetailHttpResponse>.Success(
                AuditIngestionHttpMapper.ToDetail(result.Value)));
    }
}
