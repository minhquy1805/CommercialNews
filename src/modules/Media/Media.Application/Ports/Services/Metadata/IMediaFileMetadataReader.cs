namespace Media.Application.Ports.Services.Metadata;

public interface IMediaFileMetadataReader
{
    Task<MediaFileMetadataResult> ReadAsync(
        Stream content,
        string? contentType,
        string mediaType,
        CancellationToken cancellationToken = default);
}