namespace Media.Application.Ports.Services.Metadata;

public sealed class MediaFileMetadataResult
{
    public int? Width { get; init; }

    public int? Height { get; init; }

    public int? DurationSeconds { get; init; }

    public bool HasDimensions => Width.HasValue && Height.HasValue;

    public bool HasDuration => DurationSeconds.HasValue;

    public static MediaFileMetadataResult Empty { get; } = new();
}