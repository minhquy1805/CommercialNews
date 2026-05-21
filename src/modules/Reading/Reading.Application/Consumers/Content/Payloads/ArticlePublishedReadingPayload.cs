namespace Reading.Application.Consumers.Content.Payloads;

public sealed class ArticlePublishedReadingPayload
{
    public long ArticleId { get; init; }

    public string ArticlePublicId { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public long? CategoryId { get; init; }

    public string? CategoryName { get; init; }

    public long? AuthorUserId { get; init; }

    public string? AuthorDisplayName { get; init; }

    public string Status { get; init; } = string.Empty;

    public bool IsPublic { get; init; }

    public DateTime? PublishedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public long Version { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}