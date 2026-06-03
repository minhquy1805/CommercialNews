using Audit.Application.Models.Results.AuditLogs;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using MediatR;

namespace Audit.Application.Models.Queries.AuditLogs;

public sealed record GetAuditLogListQuery(
    string? SourceModule,
    string? EventType,
    string? Action,
    string? ActionCategory,
    string? ResourceType,
    string? ResourceId,
    string? ActorUserId,
    long? ActorInternalId,
    string? Outcome,
    string? Severity,
    string? RiskLevel,
    string? CorrelationId,
    DateTime? FromUtc,
    DateTime? ToUtc,
    int Page,
    int PageSize,
    string? SortBy,
    string? SortDirection)
    : IRequest<Result<PagedQueryResult<AuditLogListItemResult>>>;