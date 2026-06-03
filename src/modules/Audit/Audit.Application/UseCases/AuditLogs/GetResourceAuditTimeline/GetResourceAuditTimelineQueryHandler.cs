using Audit.Application.Abstractions.Persistence;
using Audit.Application.Models.Queries.AuditLogs;
using Audit.Application.Models.Results.AuditLogs;
using Audit.Application.Services.Mapping;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using MediatR;

namespace Audit.Application.UseCases.AuditLogs.GetResourceAuditTimeline;

public sealed class GetResourceAuditTimelineQueryHandler
    : IRequestHandler<GetResourceAuditTimelineQuery, Result<PagedQueryResult<AuditLogListItemResult>>>
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IAuditResultMapper _auditResultMapper;

    public GetResourceAuditTimelineQueryHandler(
        IAuditLogRepository auditLogRepository,
        IAuditResultMapper auditResultMapper)
    {
        _auditLogRepository = auditLogRepository
            ?? throw new ArgumentNullException(nameof(auditLogRepository));

        _auditResultMapper = auditResultMapper
            ?? throw new ArgumentNullException(nameof(auditResultMapper));
    }

    public async Task<Result<PagedQueryResult<AuditLogListItemResult>>> Handle(
        GetResourceAuditTimelineQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var auditLogs = await _auditLogRepository.GetResourceTimelineAsync(
            resourceType: request.ResourceType,
            resourceId: request.ResourceId,
            fromUtc: request.FromUtc,
            toUtc: request.ToUtc,
            page: request.Page,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);

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