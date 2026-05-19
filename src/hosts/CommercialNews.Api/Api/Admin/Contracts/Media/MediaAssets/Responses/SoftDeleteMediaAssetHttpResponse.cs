namespace CommercialNews.Api.Api.Admin.Contracts.Media.MediaAssets.Responses;

public sealed class SoftDeleteMediaAssetHttpResponse
{
    public long MediaId { get; init; }

    public string PublicId { get; init; } = string.Empty;

    public bool IsDeleted { get; init; }

    public DateTime DeletedAt { get; init; }

    public long? DeletedBy { get; init; }

    public DateTime? RestoreUntil { get; init; }

    public int AffectedRows { get; init; }

    public int PrimaryClearedCount { get; init; }

    public int Version { get; init; }
}