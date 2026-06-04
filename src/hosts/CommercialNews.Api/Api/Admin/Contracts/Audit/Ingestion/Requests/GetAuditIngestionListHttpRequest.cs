namespace CommercialNews.Api.Api.Admin.Contracts.Audit.Ingestion.Requests;

public sealed class GetAuditIngestionListHttpRequest
{
    public string? Status { get; init; }

    public string? MessageId { get; init; }

    public string? EventType { get; init; }

    public string? AggregateType { get; init; }

    public string? AggregateId { get; init; }

    public string? AggregatePublicId { get; init; }

    public string? CorrelationId { get; init; }

    public string? ConsumerName { get; init; }

    public string? LastErrorClass { get; init; }

    public DateTime? FromUtc { get; init; }

    public DateTime? ToUtc { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Sort { get; init; }
}
