using Audit.Application.Abstractions.Persistence;
using Audit.Application.Models.Queries.AuditLogs;
using Audit.Application.Models.Results.AuditLogs;
using Audit.Application.Services.Mapping;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using MediatR;

namespace Audit.Application.UseCases.AuditLogs.GetActorAuditTimeline;

public sealed class GetActorAuditTimelineQueryHandler
    : IRequestHandler<GetActorAuditTimelineQuery, Result<PagedQueryResult<AuditLogListItemResult>>>
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IAuditResultMapper _auditResultMapper;

    public GetActorAuditTimelineQueryHandler(
        IAuditLogRepository auditLogRepository,
        IAuditResultMapper auditResultMapper)
    {
        _auditLogRepository = auditLogRepository
            ?? throw new ArgumentNullException(nameof(auditLogRepository));

        _auditResultMapper = auditResultMapper
            ?? throw new ArgumentNullException(nameof(auditResultMapper));
    }

    public async Task<Result<PagedQueryResult<AuditLogListItemResult>>> Handle(
        GetActorAuditTimelineQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var auditLogs = await _auditLogRepository.GetActorTimelineAsync(
            actorUserId: request.ActorUserId,
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