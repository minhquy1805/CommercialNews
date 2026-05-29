namespace Interaction.Application.Consumers.Content.Payloads;

public sealed class ArticleArchivedInteractionPayload
{
    public long ArticleId { get; init; }

    public string ArticlePublicId { get; init; } = string.Empty;

    public string FromStatus { get; init; } = string.Empty;

    public string ToStatus { get; init; } = string.Empty;

    public long ActorUserId { get; init; }

    public long Version { get; init; }

    public DateTime ArchivedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}