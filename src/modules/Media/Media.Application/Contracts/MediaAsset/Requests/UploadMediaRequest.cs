namespace Media.Application.Contracts.MediaAsset.Requests;

public sealed class UploadMediaRequest
{
    public Stream Content { get; init; } = Stream.Null;

    public string OriginalFileName { get; init; } = string.Empty;

    public string? ContentType { get; init; }

    public long Length { get; init; }

    public string MediaType { get; init; } = string.Empty;

    public string? AltText { get; init; }

    public string? MetadataJson { get; init; }

    public string? Folder { get; init; }

    public string? PreferredFileNameWithoutExtension { get; init; }
}