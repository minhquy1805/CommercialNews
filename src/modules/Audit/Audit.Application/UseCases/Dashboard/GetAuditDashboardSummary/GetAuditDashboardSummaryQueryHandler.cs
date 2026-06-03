using Audit.Application.Abstractions.Persistence;
using Audit.Application.Abstractions.Persistence.Queries;
using Audit.Application.Abstractions.Persistence.Results;
using Audit.Application.Models.Queries.Dashboard;
using Audit.Application.Models.Results.Dashboard;
using Audit.Application.Services.Mapping;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using MediatR;

namespace Audit.Application.UseCases.Dashboard.GetAuditDashboardSummary;

public sealed class GetAuditDashboardSummaryQueryHandler
    : IRequestHandler<GetAuditDashboardSummaryQuery, Result<AuditDashboardSummaryResult>>
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IAuditIngestionRepository _auditIngestionRepository;
    private readonly IAuditResultMapper _auditResultMapper;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetAuditDashboardSummaryQueryHandler(
        IAuditLogRepository auditLogRepository,
        IAuditIngestionRepository auditIngestionRepository,
        IAuditResultMapper auditResultMapper,
        IDateTimeProvider dateTimeProvider)
    {
        _auditLogRepository = auditLogRepository
            ?? throw new ArgumentNullException(nameof(auditLogRepository));

        _auditIngestionRepository = auditIngestionRepository
            ?? throw new ArgumentNullException(nameof(auditIngestionRepository));

        _auditResultMapper = auditResultMapper
            ?? throw new ArgumentNullException(nameof(auditResultMapper));

        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<Result<AuditDashboardSummaryResult>> Handle(
        GetAuditDashboardSummaryQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var searchQuery = new AuditDashboardSummarySearchQuery(
            FromUtc: request.FromUtc,
            ToUtc: request.ToUtc,
            SourceModule: request.SourceModule);

        var auditEvents = await _auditLogRepository.CountAsync(
            new AuditLogSearchQuery(
                SourceModule: request.SourceModule,
                EventType: null,
                Action: null,
                ActionCategory: null,
                ResourceType: null,
                ResourceId: null,
                ActorUserId: null,
                ActorInternalId: null,
                Outcome: null,
                Severity: null,
                RiskLevel: null,
                CorrelationId: null,
                FromUtc: request.FromUtc,
                ToUtc: request.ToUtc,
                Page: 1,
                PageSize: 1,
                SortBy: null,
                SortDirection: null),
            cancellationToken);

        var highRiskEvents = await _auditLogRepository.CountHighRiskAsync(
            searchQuery,
            cancellationToken);

        var criticalEvents = await _auditLogRepository.CountCriticalAsync(
            searchQuery,
            cancellationToken);

        var failedIngestion = await _auditIngestionRepository.CountFailedAsync(
            searchQuery,
            cancellationToken);

        var duplicateIngestion = await _auditIngestionRepository.CountDuplicateAsync(
            searchQuery,
            cancellationToken);

        var countsByModule = await _auditLogRepository.CountByModuleAsync(
            searchQuery,
            cancellationToken);

        var countsBySeverity = await _auditLogRepository.CountBySeverityAsync(
            searchQuery,
            cancellationToken);

        var countsByRiskLevel = await _auditLogRepository.CountByRiskLevelAsync(
            searchQuery,
            cancellationToken);

        var oldestFailedAgeSeconds = await _auditIngestionRepository
            .GetOldestFailedIngestionAgeSecondsAsync(
                _dateTimeProvider.UtcNow,
                cancellationToken);

        var data = new AuditDashboardSummaryDataResult(
            FromUtc: request.FromUtc,
            ToUtc: request.ToUtc,
            AuditEvents: auditEvents,
            HighRiskEvents: highRiskEvents,
            CriticalEvents: criticalEvents,
            FailedIngestion: failedIngestion,
            DuplicateIngestion: duplicateIngestion,
            CountsByModule: countsByModule,
            CountsBySeverity: countsBySeverity,
            CountsByRiskLevel: countsByRiskLevel,
            GeneratedAtUtc: _dateTimeProvider.UtcNow,
            OldestFailedIngestionAgeSeconds: oldestFailedAgeSeconds);

        var result = _auditResultMapper.ToDashboardSummary(
            data);

        return Result<AuditDashboardSummaryResult>.Success(
            result);
    }
}