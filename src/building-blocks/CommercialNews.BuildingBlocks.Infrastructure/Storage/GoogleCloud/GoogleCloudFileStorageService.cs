using CommercialNews.BuildingBlocks.Storage.Abstractions;
using CommercialNews.BuildingBlocks.Storage.Models;
using Microsoft.Extensions.Options;

namespace CommercialNews.BuildingBlocks.Infrastructure.Storage.GoogleCloud;

public sealed class GoogleCloudFileStorageService : IFileStorageService
{
    private readonly GoogleCloudStorageOptions _options;

    public GoogleCloudFileStorageService(
        IOptions<GoogleCloudStorageOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public Task<FileStorageUploadResult> UploadAsync(
        FileStorageUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Google Cloud Storage is not implemented yet. Use Local provider for development.");
    }

    public Task DeleteAsync(
        FileStorageDeleteRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Google Cloud Storage is not implemented yet. Use Local provider for development.");
    }
}