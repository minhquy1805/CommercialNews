using Audit.Application.Models.Results.AuditLogs;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using MediatR;

namespace Audit.Application.Models.Queries.AuditLogs;

public sealed record GetActorAuditTimelineQuery(
    string ActorUserId,
    DateTime? FromUtc,
    DateTime? ToUtc,
    int Page,
    int PageSize)
    : IRequest<Result<PagedQueryResult<AuditLogListItemResult>>>;