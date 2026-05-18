namespace Audit.Application.Consumers.Media.Payloads;

public sealed class MediaAssetUpdatedAuditPayload
{
    public long MediaId { get; init; }

    public string MediaPublicId { get; init; } = string.Empty;

    public string? AltText { get; init; }

    public string? MetadataJson { get; init; }

    public long ActorUserId { get; init; }

    public long Version { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}