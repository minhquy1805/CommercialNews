namespace Audit.Application.Consumers.Content.Payloads;

public sealed class ArticleCreatedAuditPayload
{
    public long ArticleId { get; init; }

    public string ArticlePublicId { get; init; } = string.Empty;

    public long CategoryId { get; init; }

    public long AuthorUserId { get; init; }

    public long CreatedByUserId { get; init; }

    public string Status { get; init; } = string.Empty;

    public IReadOnlyCollection<long> TagIds { get; init; } = Array.Empty<long>();

    public long Version { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}