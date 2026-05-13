namespace CommercialNews.Api.Api.Admin.Contracts.Content.Articles.Responses;

public sealed class ArticleLifecycleEventItemResponse
{
    public long EventId { get; init; }

    public long ArticleId { get; init; }

    public long ArticleVersion { get; init; }

    public string ActionType { get; init; } = string.Empty;

    public string? FromStatus { get; init; }

    public string? ToStatus { get; init; }

    public string? Reason { get; init; }

    public long ActorUserId { get; init; }

    public DateTime OccurredAt { get; init; }

    public string? CorrelationId { get; init; }

    public string? MetadataJson { get; init; }
}