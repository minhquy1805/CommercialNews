namespace Media.Application.Contracts.MediaAsset.Responses;

public sealed class UpdateMediaAssetResponse
{
    public long MediaId { get; init; }
    public string PublicId { get; init; } = string.Empty;

    public string? AltText { get; init; }
    public string? MetadataJson { get; init; }

    public DateTime UpdatedAt { get; init; }
    public long? UpdatedBy { get; init; }

    public int Version { get; init; }
}