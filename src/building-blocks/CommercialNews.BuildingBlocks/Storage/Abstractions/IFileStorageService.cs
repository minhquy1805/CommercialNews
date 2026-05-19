using CommercialNews.BuildingBlocks.Storage.Models;

namespace CommercialNews.BuildingBlocks.Storage.Abstractions;

public interface IFileStorageService
{
    Task<FileStorageUploadResult> UploadAsync(
        FileStorageUploadRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        FileStorageDeleteRequest request,
        CancellationToken cancellationToken = default);
}