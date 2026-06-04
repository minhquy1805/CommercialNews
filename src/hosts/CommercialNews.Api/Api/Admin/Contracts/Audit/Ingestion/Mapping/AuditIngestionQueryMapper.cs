using Audit.Application.Models.Queries.Ingestion;
using CommercialNews.Api.Api.Admin.Contracts.Audit.Common;
using CommercialNews.Api.Api.Admin.Contracts.Audit.Ingestion.Requests;

namespace CommercialNews.Api.Api.Admin.Contracts.Audit.Ingestion.Mapping;

internal static class AuditIngestionQueryMapper
{
    public static GetAuditIngestionListQuery ToQuery(
        GetAuditIngestionListHttpRequest request)
    {
        var sort = AuditHttpSortParser.Parse(
            request.Sort,
            AuditHttpSortFields.Ingestion);

        return new GetAuditIngestionListQuery(
            request.Status,
            request.MessageId,
            request.EventType,
            request.AggregateType,
            request.AggregateId,
            request.AggregatePublicId,
            request.CorrelationId,
            request.ConsumerName,
            request.LastErrorClass,
            request.FromUtc,
            request.ToUtc,
            request.Page,
            request.PageSize,
            sort.SortBy,
            sort.SortDirection);
    }

    public static GetFailedAuditIngestionListQuery ToQuery(
        GetFailedAuditIngestionListHttpRequest request)
    {
        var sort = AuditHttpSortParser.Parse(
            request.Sort,
            AuditHttpSortFields.Ingestion);

        return new GetFailedAuditIngestionListQuery(
            request.EventType,
            request.AggregateType,
            request.AggregateId,
            request.AggregatePublicId,
            request.CorrelationId,
            request.ConsumerName,
            request.LastErrorClass,
            request.FromUtc,
            request.ToUtc,
            request.Page,
            request.PageSize,
            sort.SortBy,
            sort.SortDirection);
    }
}
