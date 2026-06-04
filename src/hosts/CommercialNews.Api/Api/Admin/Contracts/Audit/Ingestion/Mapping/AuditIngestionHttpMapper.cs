using Audit.Application.Models.Results.Ingestion;
using CommercialNews.Api.Api.Admin.Contracts.Audit.Ingestion.Responses;

namespace CommercialNews.Api.Api.Admin.Contracts.Audit.Ingestion.Mapping;

internal static class AuditIngestionHttpMapper
{
    public static AuditIngestionListItemHttpResponse ToListItem(
        AuditIngestionListItemResult item)
    {
        return new AuditIngestionListItemHttpResponse
        {
            PublicId = item.PublicId,
            MessageId = item.MessageId,
            EventType = item.EventType,
            AggregateType = item.AggregateType,
            AggregateId = item.AggregateId,
            AggregatePublicId = item.AggregatePublicId,
            AggregateVersion = item.AggregateVersion,
            CorrelationId = item.CorrelationId,
            SourcePriority = item.SourcePriority,
            SourceOccurredAtUtc = item.SourceOccurredAtUtc,
            SourcePublishedAtUtc = item.SourcePublishedAtUtc,
            ConsumerName = item.ConsumerName,
            Status = item.Status,
            AttemptCount = item.AttemptCount,
            FirstReceivedAtUtc = item.FirstReceivedAtUtc,
            LastAttemptAtUtc = item.LastAttemptAtUtc,
            ProcessedAtUtc = item.ProcessedAtUtc,
            DeadLetteredAtUtc = item.DeadLetteredAtUtc,
            LastErrorCode = item.LastErrorCode,
            LastErrorClass = item.LastErrorClass,
            CreatedAtUtc = item.CreatedAtUtc,
            UpdatedAtUtc = item.UpdatedAtUtc
        };
    }

    public static AuditIngestionDetailHttpResponse ToDetail(
        AuditIngestionDetailResult item)
    {
        return new AuditIngestionDetailHttpResponse
        {
            PublicId = item.PublicId,
            MessageId = item.MessageId,
            EventType = item.EventType,
            AggregateType = item.AggregateType,
            AggregateId = item.AggregateId,
            AggregatePublicId = item.AggregatePublicId,
            AggregateVersion = item.AggregateVersion,
            CorrelationId = item.CorrelationId,
            SourcePriority = item.SourcePriority,
            SourceOccurredAtUtc = item.SourceOccurredAtUtc,
            SourcePublishedAtUtc = item.SourcePublishedAtUtc,
            ConsumerName = item.ConsumerName,
            Status = item.Status,
            AttemptCount = item.AttemptCount,
            FirstReceivedAtUtc = item.FirstReceivedAtUtc,
            LastAttemptAtUtc = item.LastAttemptAtUtc,
            ProcessedAtUtc = item.ProcessedAtUtc,
            DeadLetteredAtUtc = item.DeadLetteredAtUtc,
            LastErrorCode = item.LastErrorCode,
            LastErrorMessage = item.LastErrorMessage,
            LastErrorClass = item.LastErrorClass,
            CreatedAtUtc = item.CreatedAtUtc,
            UpdatedAtUtc = item.UpdatedAtUtc
        };
    }
}
