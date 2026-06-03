using Audit.Application.Abstractions.Persistence;
using Audit.Application.Abstractions.Persistence.Queries;
using Audit.Application.Models.Queries.Dashboard;
using Audit.Application.Models.Results.Dashboard;
using Audit.Application.Services.Mapping;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using MediatR;

namespace Audit.Application.UseCases.Dashboard.GetRecentRiskEvents;

public sealed class GetRecentRiskEventsQueryHandler
    : IRequestHandler<GetRecentRiskEventsQuery, Result<IReadOnlyList<RecentRiskAuditEventResult>>>
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IAuditResultMapper _auditResultMapper;

    public GetRecentRiskEventsQueryHandler(
        IAuditLogRepository auditLogRepository,
        IAuditResultMapper auditResultMapper)
    {
        _auditLogRepository = auditLogRepository
            ?? throw new ArgumentNullException(nameof(auditLogRepository));

        _auditResultMapper = auditResultMapper
            ?? throw new ArgumentNullException(nameof(auditResultMapper));
    }

    public async Task<Result<IReadOnlyList<RecentRiskAuditEventResult>>> Handle(
        GetRecentRiskEventsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var searchQuery = new AuditRecentRiskEventsSearchQuery(
            FromUtc: request.FromUtc,
            ToUtc: request.ToUtc,
            SourceModule: request.SourceModule,
            RiskLevel: request.RiskLevel,
            Limit: request.Limit);

        var auditLogs = await _auditLogRepository.GetRecentRiskEventsAsync(
            searchQuery,
            cancellationToken);

        var result = auditLogs
            .Select(_auditResultMapper.ToRecentRiskEvent)
            .ToArray();

        return Result<IReadOnlyList<RecentRiskAuditEventResult>>.Success(
            result);
    }
}