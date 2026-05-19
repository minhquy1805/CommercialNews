namespace CommercialNews.Api.Api.Admin.Contracts.Media.MediaAssets.Responses;

public sealed class RestoreMediaAssetHttpResponse
{
    public long MediaId { get; init; }

    public string PublicId { get; init; } = string.Empty;

    public bool IsRestored { get; init; }

    public bool IsDeleted { get; init; }

    public DateTime RestoredAt { get; init; }

    public long? RestoredBy { get; init; }

    public int AffectedRows { get; init; }

    public int Version { get; init; }
}