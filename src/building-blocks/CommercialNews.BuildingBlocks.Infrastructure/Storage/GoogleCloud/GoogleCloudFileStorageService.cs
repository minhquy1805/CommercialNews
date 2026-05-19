using System.Security.Cryptography;
using CommercialNews.BuildingBlocks.Storage.Abstractions;
using CommercialNews.BuildingBlocks.Storage.Constants;
using CommercialNews.BuildingBlocks.Storage.Models;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
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

    public async Task<FileStorageUploadResult> UploadAsync(
        FileStorageUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateOptions();
        ValidateUploadRequest(request);

        string extension = GetSafeExtension(request.OriginalFileName);

        string storedFileNameWithoutExtension =
            string.IsNullOrWhiteSpace(request.PreferredFileNameWithoutExtension)
                ? Guid.NewGuid().ToString("N")
                : SanitizePathSegment(request.PreferredFileNameWithoutExtension);

        string storedFileName = $"{storedFileNameWithoutExtension}{extension}";

        string relativeFolder = BuildRelativeFolder(
            request.Purpose,
            request.Folder,
            DateTime.UtcNow);

        string storagePath = $"{relativeFolder}/{storedFileName}";

        byte[] fileBytes;
        byte[] contentHash;

        using (var memoryStream = new MemoryStream())
        {
            await request.Content.CopyToAsync(
                memoryStream,
                cancellationToken);

            fileBytes = memoryStream.ToArray();
        }

        using (var sha256 = SHA256.Create())
        {
            contentHash = sha256.ComputeHash(fileBytes);
        }

        StorageClient storageClient = await CreateStorageClientAsync(
            cancellationToken);

        using var uploadStream = new MemoryStream(fileBytes);

        await storageClient.UploadObjectAsync(
            bucket: _options.BucketName,
            objectName: storagePath,
            contentType: request.ContentType ?? "application/octet-stream",
            source: uploadStream,
            options: null,
            cancellationToken: cancellationToken);

        string publicUrl = BuildPublicUrl(storagePath);

        return new FileStorageUploadResult
        {
            StorageProvider = FileStorageProviders.GoogleCloud,
            Url = publicUrl,
            StoragePath = storagePath,
            FileName = storedFileName,
            OriginalFileName = request.OriginalFileName,
            ContentType = request.ContentType,
            FileSizeBytes = request.Length,
            ContentHash = contentHash
        };
    }

    public async Task DeleteAsync(
        FileStorageDeleteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!string.Equals(
                request.StorageProvider,
                FileStorageProviders.GoogleCloud,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(request.StoragePath))
        {
            return;
        }

        ValidateOptions();

        string storagePath = request.StoragePath
            .Replace("\\", "/", StringComparison.Ordinal)
            .TrimStart('/');

        if (storagePath.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid Google Cloud Storage object path.");
        }

        StorageClient storageClient = await CreateStorageClientAsync(
            cancellationToken);

        await storageClient.DeleteObjectAsync(
            bucket: _options.BucketName,
            objectName: storagePath,
            options: null,
            cancellationToken: cancellationToken);
    }

    private async Task<StorageClient> CreateStorageClientAsync(
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.CredentialsJsonPath))
        {
            return await StorageClient.CreateAsync();
        }

        ServiceAccountCredential serviceAccountCredential =
            await CredentialFactory.FromFileAsync<ServiceAccountCredential>(
                _options.CredentialsJsonPath,
                cancellationToken);

        GoogleCredential googleCredential =
            serviceAccountCredential.ToGoogleCredential();

        return await StorageClient.CreateAsync(googleCredential);
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.BucketName))
        {
            throw new InvalidOperationException(
                "Google Cloud Storage bucket name is required.");
        }

        if (_options.UsePublicUrl &&
            string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            throw new InvalidOperationException(
                "Google Cloud Storage public base URL is required.");
        }
    }

    private static void ValidateUploadRequest(
        FileStorageUploadRequest request)
    {
        if (request.Content == Stream.Null)
        {
            throw new InvalidOperationException("File content is required.");
        }

        if (!request.Content.CanRead)
        {
            throw new InvalidOperationException("File content stream must be readable.");
        }

        if (request.Length <= 0)
        {
            throw new InvalidOperationException("File length must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.OriginalFileName))
        {
            throw new InvalidOperationException("Original file name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Purpose))
        {
            throw new InvalidOperationException("File storage purpose is required.");
        }
    }

    private static string GetSafeExtension(
        string originalFileName)
    {
        string extension = Path.GetExtension(originalFileName);

        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        string normalized = extension.Trim().ToLowerInvariant();

        if (normalized.Contains("..", StringComparison.Ordinal) ||
            normalized.Contains('/', StringComparison.Ordinal) ||
            normalized.Contains('\\', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid file extension.");
        }

        return normalized;
    }

    private static string BuildRelativeFolder(
        string purpose,
        string? folder,
        DateTime utcNow)
    {
        string safePurpose = SanitizeRelativePath(purpose);

        string datePath = $"{utcNow:yyyy}/{utcNow:MM}/{utcNow:dd}";

        if (string.IsNullOrWhiteSpace(folder))
        {
            return $"{safePurpose}/{datePath}";
        }

        string safeFolder = SanitizeRelativePath(folder);

        return $"{safePurpose}/{safeFolder}/{datePath}";
    }

    private string BuildPublicUrl(
        string storagePath)
    {
        string baseUrl = _options.PublicBaseUrl.TrimEnd('/');

        string normalizedPath = storagePath
            .Replace("\\", "/", StringComparison.Ordinal)
            .TrimStart('/');

        return $"{baseUrl}/{normalizedPath}";
    }

    private static string SanitizeRelativePath(
        string value)
    {
        string[] segments = value
            .Replace("\\", "/", StringComparison.Ordinal)
            .Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(SanitizePathSegment)
            .Where(static segment => segment.Length > 0)
            .ToArray();

        if (segments.Length == 0)
        {
            throw new InvalidOperationException("Storage path contains no valid segment.");
        }

        return string.Join("/", segments);
    }

    private static string SanitizePathSegment(
        string value)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();

        string sanitized = new(
            value.Trim()
                .Select(character => invalidChars.Contains(character) ? '-' : character)
                .ToArray());

        sanitized = sanitized
            .Replace("..", "-", StringComparison.Ordinal)
            .Replace(" ", "-", StringComparison.Ordinal)
            .Trim('-', '.', '/', '\\');

        return sanitized;
    }
}