namespace Audit.Application.Consumers.Content.Payloads;

public sealed class ArticleUpdatedAuditPayload
{
    public long ArticleId { get; init; }

    public string ArticlePublicId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public long CategoryId { get; init; }

    public long ActorUserId { get; init; }

    public long RevisionId { get; init; }

    public string? ChangeSummary { get; init; }

    public IReadOnlyCollection<long> TagIds { get; init; } = Array.Empty<long>();

    public long Version { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}