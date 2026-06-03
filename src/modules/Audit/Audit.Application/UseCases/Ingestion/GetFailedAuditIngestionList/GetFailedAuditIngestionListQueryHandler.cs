using Audit.Application.Abstractions.Persistence;
using Audit.Application.Abstractions.Persistence.Queries;
using Audit.Application.Models.Queries.Ingestion;
using Audit.Application.Models.Results.Ingestion;
using Audit.Application.Services.Mapping;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using MediatR;

namespace Audit.Application.UseCases.Ingestion.GetFailedAuditIngestionList;

public sealed class GetFailedAuditIngestionListQueryHandler
    : IRequestHandler<GetFailedAuditIngestionListQuery, Result<PagedQueryResult<AuditIngestionListItemResult>>>
{
    private readonly IAuditIngestionRepository _auditIngestionRepository;
    private readonly IAuditResultMapper _auditResultMapper;

    public GetFailedAuditIngestionListQueryHandler(
        IAuditIngestionRepository auditIngestionRepository,
        IAuditResultMapper auditResultMapper)
    {
        _auditIngestionRepository = auditIngestionRepository
            ?? throw new ArgumentNullException(nameof(auditIngestionRepository));

        _auditResultMapper = auditResultMapper
            ?? throw new ArgumentNullException(nameof(auditResultMapper));
    }

    public async Task<Result<PagedQueryResult<AuditIngestionListItemResult>>> Handle(
        GetFailedAuditIngestionListQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var searchQuery = new AuditFailedIngestionSearchQuery(
            EventType: request.EventType,
            AggregateType: request.AggregateType,
            AggregateId: request.AggregateId,
            AggregatePublicId: request.AggregatePublicId,
            CorrelationId: request.CorrelationId,
            ConsumerName: request.ConsumerName,
            LastErrorClass: request.LastErrorClass,
            FromUtc: request.FromUtc,
            ToUtc: request.ToUtc,
            Page: request.Page,
            PageSize: request.PageSize,
            SortBy: request.SortBy,
            SortDirection: request.SortDirection);

        var result = await _auditIngestionRepository.SearchFailedAsync(
            searchQuery,
            cancellationToken);

        var response = new PagedQueryResult<AuditIngestionListItemResult>
        {
            Items = result.Items
                .Select(_auditResultMapper.ToAuditIngestionListItem)
                .ToArray(),

            Page = result.Page,
            PageSize = result.PageSize,
            TotalItems = result.TotalItems
        };

        return Result<PagedQueryResult<AuditIngestionListItemResult>>.Success(
            response);
    }
}