using Audit.Application.Contracts.Requests;
using Audit.Application.Contracts.Responses;
using Audit.Application.UseCases.GetAuditLogByEventId;
using Audit.Application.UseCases.GetAuditLogById;
using Audit.Application.UseCases.GetAuditLogs;
using Audit.Application.UseCases.GetAuditLogsByCorrelationId;
using CommercialNews.Api.Api.Admin.Contracts.Audit.AuditLog.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Audit.AuditLog.Responses;
using CommercialNews.Api.Api.Common.ErrorHandling;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.Admin.Controllers.Audit;

[ApiController]
[Route("api/v1/admin/audit/logs")]
public sealed class AuditLogsAdminController : ControllerBase
{
    private const string GetAuditLogByIdRouteName = "AdminAuditLogs.GetById";
    private const string GetAuditLogByEventIdRouteName = "AdminAuditLogs.GetByEventId";

    private readonly IGetAuditLogsUseCase _getAuditLogsUseCase;
    private readonly IGetAuditLogByIdUseCase _getAuditLogByIdUseCase;
    private readonly IGetAuditLogsByCorrelationIdUseCase _getAuditLogsByCorrelationIdUseCase;
    private readonly IGetAuditLogByEventIdUseCase _getAuditLogByEventIdUseCase;

    public AuditLogsAdminController(
        IGetAuditLogsUseCase getAuditLogsUseCase,
        IGetAuditLogByIdUseCase getAuditLogByIdUseCase,
        IGetAuditLogsByCorrelationIdUseCase getAuditLogsByCorrelationIdUseCase,
        IGetAuditLogByEventIdUseCase getAuditLogByEventIdUseCase)
    {
        _getAuditLogsUseCase = getAuditLogsUseCase;
        _getAuditLogByIdUseCase = getAuditLogByIdUseCase;
        _getAuditLogsByCorrelationIdUseCase = getAuditLogsByCorrelationIdUseCase;
        _getAuditLogByEventIdUseCase = getAuditLogByEventIdUseCase;
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetAuditLogsHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPagedAsync(
        [FromQuery] GetAuditLogsHttpRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetAuditLogsRequest
        {
            FromOccurredAt = request.FromOccurredAt,
            ToOccurredAt = request.ToOccurredAt,
            ActorUserId = request.ActorUserId,
            Action = request.Action,
            ResourceType = request.ResourceType,
            ResourceId = request.ResourceId,
            CorrelationId = request.CorrelationId,
            AuditEventId = request.AuditEventId,
            Outcome = request.Outcome,
            Page = request.Page,
            PageSize = request.PageSize
        };

        Result<GetAuditLogsResponse> result =
            await _getAuditLogsUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<GetAuditLogsHttpResponse>.Failure(result.Error!));
        }

        var response = new GetAuditLogsHttpResponse
        {
            Items = result.Value!.Items.Select(static item => new AuditLogListItemHttpResponse
            {
                AuditId = item.AuditId,
                AuditEventId = item.AuditEventId,
                OccurredAt = item.OccurredAt,
                ActorUserId = item.ActorUserId,
                Action = item.Action,
                ResourceType = item.ResourceType,
                ResourceId = item.ResourceId,
                Outcome = item.Outcome,
                Summary = item.Summary,
                CorrelationId = item.CorrelationId
            }).ToArray(),
            Page = result.Value.Page,
            PageSize = result.Value.PageSize,
            TotalCount = result.Value.TotalCount,
            TotalPages = result.Value.TotalPages
        };

        return this.ToActionResult(Result<GetAuditLogsHttpResponse>.Success(response));
    }

    [HttpGet("{auditId:long}", Name = GetAuditLogByIdRouteName)]
    [ProducesResponseType(typeof(GetAuditLogByIdHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(
        [FromRoute] long auditId,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetAuditLogByIdRequest
        {
            AuditId = auditId
        };

        Result<GetAuditLogByIdResponse> result =
            await _getAuditLogByIdUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<GetAuditLogByIdHttpResponse>.Failure(result.Error!));
        }

        var response = new GetAuditLogByIdHttpResponse
        {
            AuditId = result.Value!.AuditId,
            AuditEventId = result.Value.AuditEventId,
            OccurredAt = result.Value.OccurredAt,
            ActorUserId = result.Value.ActorUserId,
            Action = result.Value.Action,
            ResourceType = result.Value.ResourceType,
            ResourceId = result.Value.ResourceId,
            Outcome = result.Value.Outcome,
            Summary = result.Value.Summary,
            Reason = result.Value.Reason,
            CorrelationId = result.Value.CorrelationId,
            IpAddress = result.Value.IpAddress,
            UserAgent = result.Value.UserAgent,
            OldValuesJson = result.Value.OldValuesJson,
            NewValuesJson = result.Value.NewValuesJson,
            MetadataJson = result.Value.MetadataJson
        };

        return this.ToActionResult(Result<GetAuditLogByIdHttpResponse>.Success(response));
    }

    [HttpGet("by-correlation/{correlationId}")]
    [ProducesResponseType(typeof(GetAuditLogsByCorrelationIdHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByCorrelationIdAsync(
        [FromRoute] string correlationId,
        [FromQuery] GetAuditLogsByCorrelationIdHttpRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetAuditLogsByCorrelationIdRequest
        {
            CorrelationId = correlationId,
            Page = request.Page,
            PageSize = request.PageSize
        };

        Result<GetAuditLogsByCorrelationIdResponse> result =
            await _getAuditLogsByCorrelationIdUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<GetAuditLogsByCorrelationIdHttpResponse>.Failure(result.Error!));
        }

        var response = new GetAuditLogsByCorrelationIdHttpResponse
        {
            Items = result.Value!.Items.Select(static item => new AuditLogListItemHttpResponse
            {
                AuditId = item.AuditId,
                AuditEventId = item.AuditEventId,
                OccurredAt = item.OccurredAt,
                ActorUserId = item.ActorUserId,
                Action = item.Action,
                ResourceType = item.ResourceType,
                ResourceId = item.ResourceId,
                Outcome = item.Outcome,
                Summary = item.Summary,
                CorrelationId = item.CorrelationId
            }).ToArray(),
            Page = result.Value.Page,
            PageSize = result.Value.PageSize,
            TotalCount = result.Value.TotalCount,
            TotalPages = result.Value.TotalPages
        };

        return this.ToActionResult(Result<GetAuditLogsByCorrelationIdHttpResponse>.Success(response));
    }

    [HttpGet("by-event/{auditEventId}", Name = GetAuditLogByEventIdRouteName)]
    [ProducesResponseType(typeof(GetAuditLogByEventIdHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByEventIdAsync(
        [FromRoute] string auditEventId,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetAuditLogByEventIdRequest
        {
            AuditEventId = auditEventId
        };

        Result<GetAuditLogByEventIdResponse> result =
            await _getAuditLogByEventIdUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(Result<GetAuditLogByEventIdHttpResponse>.Failure(result.Error!));
        }

        var response = new GetAuditLogByEventIdHttpResponse
        {
            AuditId = result.Value!.AuditId,
            AuditEventId = result.Value.AuditEventId,
            OccurredAt = result.Value.OccurredAt,
            ActorUserId = result.Value.ActorUserId,
            Action = result.Value.Action,
            ResourceType = result.Value.ResourceType,
            ResourceId = result.Value.ResourceId,
            Outcome = result.Value.Outcome,
            Summary = result.Value.Summary,
            Reason = result.Value.Reason,
            CorrelationId = result.Value.CorrelationId,
            IpAddress = result.Value.IpAddress,
            UserAgent = result.Value.UserAgent,
            OldValuesJson = result.Value.OldValuesJson,
            NewValuesJson = result.Value.NewValuesJson,
            MetadataJson = result.Value.MetadataJson
        };

        return this.ToActionResult(Result<GetAuditLogByEventIdHttpResponse>.Success(response));
    }
}