using Audit.Application.Models.Results.Dashboard;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using MediatR;

namespace Audit.Application.Models.Queries.Dashboard;

public sealed record GetRecentRiskEventsQuery(
    DateTime? FromUtc,
    DateTime? ToUtc,
    string? SourceModule,
    string? RiskLevel,
    int Limit)
    : IRequest<Result<IReadOnlyList<RecentRiskAuditEventResult>>>;