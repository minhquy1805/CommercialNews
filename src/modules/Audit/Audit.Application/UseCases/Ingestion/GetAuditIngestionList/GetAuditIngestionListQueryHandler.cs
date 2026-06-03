using Audit.Application.Abstractions.Persistence;
using Audit.Application.Abstractions.Persistence.Queries;
using Audit.Application.Models.Queries.Ingestion;
using Audit.Application.Models.Results.Ingestion;
using Audit.Application.Services.Mapping;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using MediatR;

namespace Audit.Application.UseCases.Ingestion.GetAuditIngestionList;

public sealed class GetAuditIngestionListQueryHandler
    : IRequestHandler<GetAuditIngestionListQuery, Result<PagedQueryResult<AuditIngestionListItemResult>>>
{
    private readonly IAuditIngestionRepository _auditIngestionRepository;
    private readonly IAuditResultMapper _auditResultMapper;

    public GetAuditIngestionListQueryHandler(
        IAuditIngestionRepository auditIngestionRepository,
        IAuditResultMapper auditResultMapper)
    {
        _auditIngestionRepository = auditIngestionRepository
            ?? throw new ArgumentNullException(nameof(auditIngestionRepository));

        _auditResultMapper = auditResultMapper
            ?? throw new ArgumentNullException(nameof(auditResultMapper));
    }

    public async Task<Result<PagedQueryResult<AuditIngestionListItemResult>>> Handle(
        GetAuditIngestionListQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var searchQuery = new AuditIngestionSearchQuery(
            Status: request.Status,
            MessageId: request.MessageId,
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

        var result = await _auditIngestionRepository.SearchAsync(
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
