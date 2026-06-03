using Audit.Application.Abstractions.Persistence;
using Audit.Application.Abstractions.Persistence.Queries;
using Audit.Application.Models.Queries.AuditLogs;
using Audit.Application.Models.Results.AuditLogs;
using Audit.Application.Services.Mapping;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using MediatR;

namespace Audit.Application.UseCases.AuditLogs.GetAuditLogList;

public sealed class GetAuditLogListQueryHandler
    : IRequestHandler<GetAuditLogListQuery, Result<PagedQueryResult<AuditLogListItemResult>>>
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IAuditResultMapper _auditResultMapper;

    public GetAuditLogListQueryHandler(
        IAuditLogRepository auditLogRepository,
        IAuditResultMapper auditResultMapper)
    {
        _auditLogRepository = auditLogRepository
            ?? throw new ArgumentNullException(nameof(auditLogRepository));

        _auditResultMapper = auditResultMapper
            ?? throw new ArgumentNullException(nameof(auditResultMapper));
    }

    public async Task<Result<PagedQueryResult<AuditLogListItemResult>>> Handle(
        GetAuditLogListQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var searchQuery = new AuditLogSearchQuery(
            MessageId: request.MessageId,
            SourceModule: request.SourceModule,
            EventType: request.EventType,
            Action: request.Action,
            ActionCategory: request.ActionCategory,
            ResourceType: request.ResourceType,
            ResourceId: request.ResourceId,
            ActorUserId: request.ActorUserId,
            ActorInternalId: request.ActorInternalId,
            Outcome: request.Outcome,
            Severity: request.Severity,
            RiskLevel: request.RiskLevel,
            CorrelationId: request.CorrelationId,
            FromUtc: request.FromUtc,
            ToUtc: request.ToUtc,
            Page: request.Page,
            PageSize: request.PageSize,
            SortBy: request.SortBy,
            SortDirection: request.SortDirection);

        var auditLogs = await _auditLogRepository.SearchAsync(
            searchQuery,
            cancellationToken);

        var response = new PagedQueryResult<AuditLogListItemResult>
        {
            Items = auditLogs.Items
                .Select(_auditResultMapper.ToAuditLogListItem)
                .ToArray(),

            Page = auditLogs.Page,
            PageSize = auditLogs.PageSize,
            TotalItems = auditLogs.TotalItems
        };

        return Result<PagedQueryResult<AuditLogListItemResult>>.Success(
            response);
    }
}