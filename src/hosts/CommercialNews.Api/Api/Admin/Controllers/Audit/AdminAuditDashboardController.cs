using Audit.Application.Models.Queries.Dashboard;
using Audit.Application.Models.Results.Dashboard;
using CommercialNews.Api.Api.Admin.Contracts.Audit.Dashboard.Mapping;
using CommercialNews.Api.Api.Admin.Contracts.Audit.Dashboard.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Audit.Dashboard.Responses;
using CommercialNews.Api.Api.Common.ErrorHandling;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.Api.Authorization;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.Admin.Controllers.Audit;

[ApiController]
[Route("api/v1/admin/audit/dashboard")]
public sealed class AdminAuditDashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminAuditDashboardController(
        IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("summary")]
    [Authorize(Policy = AuthorizationPolicies.AuditDashboardRead)]
    [ProducesResponseType(typeof(GetAuditDashboardSummaryHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDashboardSummaryAsync(
        [FromQuery] GetAuditDashboardSummaryHttpRequest request,
        CancellationToken cancellationToken)
    {
        Result<AuditDashboardSummaryResult> result =
            await _mediator.Send(
                new GetAuditDashboardSummaryQuery(
                    request.FromUtc,
                    request.ToUtc,
                    request.SourceModule),
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<GetAuditDashboardSummaryHttpResponse>.Failure(result.Error!));
        }

        return this.ToActionResult(
            Result<GetAuditDashboardSummaryHttpResponse>.Success(
                AuditDashboardHttpMapper.ToSummary(result.Value)));
    }

    [HttpGet("recent-risk-events")]
    [Authorize(Policy = AuthorizationPolicies.AuditDashboardRead)]
    [ProducesResponseType(typeof(GetRecentRiskEventsHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRecentRiskEventsAsync(
        [FromQuery] GetRecentRiskEventsHttpRequest request,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<RecentRiskAuditEventResult>> result =
            await _mediator.Send(
                new GetRecentRiskEventsQuery(
                    request.FromUtc,
                    request.ToUtc,
                    request.SourceModule,
                    request.RiskLevel,
                    request.Limit),
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<GetRecentRiskEventsHttpResponse>.Failure(result.Error!));
        }

        return this.ToActionResult(
            Result<GetRecentRiskEventsHttpResponse>.Success(
                AuditDashboardHttpMapper.ToRecentRiskEvents(result.Value)));
    }
}
