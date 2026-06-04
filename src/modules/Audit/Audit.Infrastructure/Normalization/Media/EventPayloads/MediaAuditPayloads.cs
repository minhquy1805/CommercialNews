namespace Audit.Infrastructure.Normalization.Media.EventPayloads;

internal sealed class MediaAssetRegisteredAuditPayload
{
    public long MediaId { get; init; }
    public string MediaPublicId { get; init; } = string.Empty;
    public string StorageProvider { get; init; } = string.Empty;
    public string? FileName { get; init; }
    public string MediaType { get; init; } = string.Empty;
    public string? MimeType { get; init; }
    public long? FileSizeBytes { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public int? DurationSeconds { get; init; }
    public string? AltText { get; init; }
    public long ActorUserId { get; init; }
    public long Version { get; init; }
    public DateTime RegisteredAtUtc { get; init; }
}

internal sealed class MediaAssetUpdatedAuditPayload
{
    public long MediaId { get; init; }
    public string MediaPublicId { get; init; } = string.Empty;
    public string? AltText { get; init; }
    public long ActorUserId { get; init; }
    public long Version { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}

internal sealed class MediaAssetSoftDeletedAuditPayload
{
    public long MediaId { get; init; }
    public string MediaPublicId { get; init; } = string.Empty;
    public bool IsDeleted { get; init; }
    public DateTime? RestoreUntil { get; init; }
    public int PrimaryClearedCount { get; init; }
    public long ActorUserId { get; init; }
    public long Version { get; init; }
    public DateTime DeletedAtUtc { get; init; }
}

internal sealed class MediaAssetRestoredAuditPayload
{
    public long MediaId { get; init; }
    public string MediaPublicId { get; init; } = string.Empty;
    public bool IsDeleted { get; init; }
    public long ActorUserId { get; init; }
    public long Version { get; init; }
    public DateTime RestoredAtUtc { get; init; }
}

internal sealed class ArticleMediaAttachedAuditPayload
{
    public long ArticleId { get; init; }
    public long MediaId { get; init; }
    public string MediaPublicId { get; init; } = string.Empty;
    public long? ArticleMediaId { get; init; }
    public string MediaType { get; init; } = string.Empty;
    public string? AltText { get; init; }
    public string? AltTextOverride { get; init; }
    public string? EffectiveAltText { get; init; }
    public string? Caption { get; init; }
    public int SortOrder { get; init; }
    public bool IsPrimary { get; init; }
    public bool PrimaryChanged { get; init; }
    public long ActorUserId { get; init; }
    public long AttachmentSetVersion { get; init; }
    public DateTime AttachedAtUtc { get; init; }
}

internal sealed class ArticleMediaDetachedAuditPayload
{
    public long ArticleId { get; init; }
    public long MediaId { get; init; }
    public bool PrimaryCleared { get; init; }
    public long ActorUserId { get; init; }
    public long AttachmentSetVersion { get; init; }
    public DateTime DetachedAtUtc { get; init; }
}

internal sealed class ArticleMediaReorderedAuditPayload
{
    public long ArticleId { get; init; }
    public IReadOnlyCollection<ArticleMediaReorderedAuditItem> Items { get; init; } =
        Array.Empty<ArticleMediaReorderedAuditItem>();
    public long ActorUserId { get; init; }
    public long AttachmentSetVersion { get; init; }
    public DateTime ReorderedAtUtc { get; init; }
}

internal sealed class ArticleMediaReorderedAuditItem
{
    public long MediaId { get; init; }
    public int SortOrder { get; init; }
}

internal sealed class ArticlePrimaryMediaSetAuditPayload
{
    public long ArticleId { get; init; }
    public long MediaId { get; init; }
    public string MediaPublicId { get; init; } = string.Empty;
    public string MediaType { get; init; } = string.Empty;
    public string? AltText { get; init; }
    public string? AltTextOverride { get; init; }
    public string? EffectiveAltText { get; init; }
    public string? Caption { get; init; }
    public int SortOrder { get; init; }
    public long ActorUserId { get; init; }
    public long AttachmentSetVersion { get; init; }
    public DateTime PrimarySetAtUtc { get; init; }
}
