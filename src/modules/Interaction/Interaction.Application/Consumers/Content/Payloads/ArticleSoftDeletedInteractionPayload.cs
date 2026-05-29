namespace Interaction.Application.Consumers.Content.Payloads;

public sealed class ArticleSoftDeletedInteractionPayload
{
    public long ArticleId { get; init; }

    public string ArticlePublicId { get; init; } = string.Empty;

    public string FromStatus { get; init; } = string.Empty;

    public string ToStatus { get; init; } = string.Empty;

    public bool IsDeleted { get; init; }

    public long ActorUserId { get; init; }

    public long Version { get; init; }

    public DateTime DeletedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}