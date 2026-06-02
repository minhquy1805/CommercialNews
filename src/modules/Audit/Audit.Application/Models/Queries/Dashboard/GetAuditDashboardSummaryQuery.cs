using Audit.Application.Models.Results.Dashboard;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using MediatR;

namespace Audit.Application.Models.Queries.Dashboard;

public sealed record GetAuditDashboardSummaryQuery(
    DateTime? FromUtc,
    DateTime? ToUtc,
    string? SourceModule)
    : IRequest<Result<AuditDashboardSummaryResult>>;