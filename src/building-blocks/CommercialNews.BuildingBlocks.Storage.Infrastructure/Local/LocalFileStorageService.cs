using System.Security.Cryptography;
using CommercialNews.BuildingBlocks.Storage.Abstractions;
using CommercialNews.BuildingBlocks.Storage.Constants;
using CommercialNews.BuildingBlocks.Storage.Models;
using Microsoft.Extensions.Options;

namespace CommercialNews.BuildingBlocks.Infrastructure.Storage.Local;

public sealed class LocalFileStorageService : IFileStorageService
{
    private const int BufferSize = 81920;

    private readonly LocalFileStorageOptions _options;

    public LocalFileStorageService(
        IOptions<LocalFileStorageOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<FileStorageUploadResult> UploadAsync(
        FileStorageUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

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

        string absoluteFolder = Path.Combine(
            _options.RootPath,
            relativeFolder.Replace('/', Path.DirectorySeparatorChar));

        Directory.CreateDirectory(absoluteFolder);

        string absoluteFilePath = Path.Combine(
            absoluteFolder,
            storedFileName);

        byte[] contentHash;

        await using (var outputStream = new FileStream(
                         absoluteFilePath,
                         FileMode.CreateNew,
                         FileAccess.Write,
                         FileShare.None,
                         BufferSize,
                         useAsync: true))
        {
            using var sha256 = SHA256.Create();

            await using var cryptoStream = new CryptoStream(
                outputStream,
                sha256,
                CryptoStreamMode.Write);

            await request.Content.CopyToAsync(
                cryptoStream,
                cancellationToken);

            await cryptoStream.FlushAsync(cancellationToken);
            cryptoStream.FlushFinalBlock();

            contentHash = sha256.Hash ?? Array.Empty<byte>();
        }

        string storagePath = $"{relativeFolder}/{storedFileName}";
        string publicUrl = BuildPublicUrl(storagePath);

        return new FileStorageUploadResult
        {
            StorageProvider = FileStorageProviders.Local,
            Url = publicUrl,
            StoragePath = storagePath,
            FileName = storedFileName,
            OriginalFileName = request.OriginalFileName,
            ContentType = request.ContentType,
            FileSizeBytes = request.Length,
            ContentHash = contentHash
        };
    }

    public Task DeleteAsync(
        FileStorageDeleteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!string.Equals(
                request.StorageProvider,
                FileStorageProviders.Local,
                StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(request.StoragePath))
        {
            return Task.CompletedTask;
        }

        string safeStoragePath = request.StoragePath
            .Replace("\\", "/", StringComparison.Ordinal)
            .TrimStart('/');

        if (safeStoragePath.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid storage path.");
        }

        string absoluteFilePath = Path.Combine(
            _options.RootPath,
            safeStoragePath.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(absoluteFilePath))
        {
            File.Delete(absoluteFilePath);
        }

        return Task.CompletedTask;
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