using Audit.Application.Models.Results.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using MediatR;

namespace Audit.Application.Models.Queries.Ingestions;

public sealed record GetFailedAuditIngestionsQuery(
    string? EventType,
    string? AggregateType,
    string? AggregateId,
    string? AggregatePublicId,
    string? CorrelationId,
    string? ConsumerName,
    string? LastErrorClass,
    DateTime? FromUtc,
    DateTime? ToUtc,
    int Page,
    int PageSize,
    string? SortBy,
    string? SortDirection)
    : IRequest<Result<PagedQueryResult<AuditIngestionListItemResult>>>;