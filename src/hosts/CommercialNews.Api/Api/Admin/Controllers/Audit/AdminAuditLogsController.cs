using Audit.Application.Models.Queries.AuditLogs;
using Audit.Application.Models.Results.AuditLogs;
using CommercialNews.Api.Api.Admin.Contracts.Audit.AuditLog.Mapping;
using CommercialNews.Api.Api.Admin.Contracts.Audit.AuditLog.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Audit.AuditLog.Responses;
using CommercialNews.Api.Api.Common.Contracts;
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
[Route("api/v1/admin/audit")]
public sealed class AdminAuditLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminAuditLogsController(
        IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("logs")]
    [Authorize(Policy = AuthorizationPolicies.AuditLogsRead)]
    [ProducesResponseType(typeof(PagedResponse<AuditLogListItemHttpResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetLogsAsync(
        [FromQuery] GetAuditLogsHttpRequest request,
        CancellationToken cancellationToken)
    {
        return GetLogListAsync(
            AuditLogQueryMapper.ToQuery(
                request,
                sourceModuleOverride: null,
                resourceTypeOverride: null,
                resourceIdOverride: null,
                actorUserIdOverride: null),
            cancellationToken);
    }

    [HttpGet("logs/{publicId}")]
    [Authorize(Policy = AuthorizationPolicies.AuditLogsReadDetail)]
    [ProducesResponseType(typeof(GetAuditLogByIdHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetLogByPublicIdAsync(
        [FromRoute] string publicId,
        CancellationToken cancellationToken)
    {
        return GetLogDetailAsync(
            new GetAuditLogDetailQuery(publicId),
            cancellationToken);
    }

    [HttpGet("logs/by-message/{messageId}")]
    [Authorize(Policy = AuthorizationPolicies.AuditLogsReadByMessage)]
    [ProducesResponseType(typeof(GetAuditLogByIdHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetLogByMessageIdAsync(
        [FromRoute] string messageId,
        CancellationToken cancellationToken)
    {
        return GetLogDetailAsync(
            new GetAuditLogByMessageIdQuery(messageId),
            cancellationToken);
    }

    [HttpGet("logs/by-correlation/{correlationId}")]
    [Authorize(Policy = AuthorizationPolicies.AuditLogsReadByCorrelation)]
    [ProducesResponseType(typeof(PagedResponse<AuditLogListItemHttpResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLogsByCorrelationIdAsync(
        [FromRoute] string correlationId,
        [FromQuery] GetAuditLogsByCorrelationIdHttpRequest request,
        CancellationToken cancellationToken)
    {
        Result<PagedQueryResult<AuditLogListItemResult>> result =
            await _mediator.Send(
                new GetAuditLogsByCorrelationIdQuery(
                    correlationId,
                    request.FromUtc,
                    request.ToUtc,
                    request.Page,
                    request.PageSize),
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<PagedResponse<AuditLogListItemHttpResponse>>.Failure(result.Error!));
        }

        var response = PagedResponse<AuditLogListItemHttpResponse>.From(
            result.Value,
            AuditLogHttpMapper.ToListItem);

        return this.ToActionResult(
            Result<PagedResponse<AuditLogListItemHttpResponse>>.Success(response));
    }

    [HttpGet("modules/{sourceModule}/logs")]
    [Authorize(Policy = AuthorizationPolicies.AuditLogsRead)]
    [ProducesResponseType(typeof(PagedResponse<AuditLogListItemHttpResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetModuleLogsAsync(
        [FromRoute] string sourceModule,
        [FromQuery] GetAuditLogsHttpRequest request,
        CancellationToken cancellationToken)
    {
        return GetLogListAsync(
            AuditLogQueryMapper.ToQuery(
                request,
                sourceModuleOverride: sourceModule,
                resourceTypeOverride: null,
                resourceIdOverride: null,
                actorUserIdOverride: null),
            cancellationToken);
    }

    [HttpGet("resources/{resourceType}/{resourceId}/timeline")]
    [Authorize(Policy = AuthorizationPolicies.AuditLogsRead)]
    [ProducesResponseType(typeof(PagedResponse<AuditLogListItemHttpResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetResourceTimelineAsync(
        [FromRoute] string resourceType,
        [FromRoute] string resourceId,
        [FromQuery] GetAuditTimelineHttpRequest request,
        CancellationToken cancellationToken)
    {
        return GetLogListAsync(
            AuditLogQueryMapper.ToQuery(
                request,
                sourceModuleOverride: request.SourceModule,
                resourceTypeOverride: resourceType,
                resourceIdOverride: resourceId,
                actorUserIdOverride: null),
            cancellationToken);
    }

    [HttpGet("actors/{actorUserId}/timeline")]
    [Authorize(Policy = AuthorizationPolicies.AuditLogsRead)]
    [ProducesResponseType(typeof(PagedResponse<AuditLogListItemHttpResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetActorTimelineAsync(
        [FromRoute] string actorUserId,
        [FromQuery] GetAuditTimelineHttpRequest request,
        CancellationToken cancellationToken)
    {
        return GetLogListAsync(
            AuditLogQueryMapper.ToQuery(
                request,
                sourceModuleOverride: request.SourceModule,
                resourceTypeOverride: null,
                resourceIdOverride: null,
                actorUserIdOverride: actorUserId),
            cancellationToken);
    }

    private async Task<IActionResult> GetLogListAsync(
        GetAuditLogListQuery query,
        CancellationToken cancellationToken)
    {
        Result<PagedQueryResult<AuditLogListItemResult>> result =
            await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<PagedResponse<AuditLogListItemHttpResponse>>.Failure(result.Error!));
        }

        var response = PagedResponse<AuditLogListItemHttpResponse>.From(
            result.Value,
            AuditLogHttpMapper.ToListItem);

        return this.ToActionResult(
            Result<PagedResponse<AuditLogListItemHttpResponse>>.Success(response));
    }

    private async Task<IActionResult> GetLogDetailAsync<TQuery>(
        TQuery query,
        CancellationToken cancellationToken)
        where TQuery : IRequest<Result<AuditLogDetailResult>>
    {
        Result<AuditLogDetailResult> result =
            await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<GetAuditLogByIdHttpResponse>.Failure(result.Error!));
        }

        return this.ToActionResult(
            Result<GetAuditLogByIdHttpResponse>.Success(
                AuditLogHttpMapper.ToDetail(result.Value)));
    }
}
