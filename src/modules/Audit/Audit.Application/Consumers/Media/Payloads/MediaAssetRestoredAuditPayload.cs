namespace Audit.Application.Consumers.Media.Payloads;

public sealed class MediaAssetRestoredAuditPayload
{
    public long MediaId { get; init; }

    public string MediaPublicId { get; init; } = string.Empty;

    public bool IsDeleted { get; init; }

    public long ActorUserId { get; init; }

    public long Version { get; init; }

    public DateTime RestoredAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}