using Media.Domain.Enums;
using Media.Domain.Exceptions;

namespace Media.Domain.Entities;

public sealed class MediaAsset
{
    public long MediaId { get; private set; }
    public string PublicId { get; private set; } = string.Empty;

    public string StorageProvider { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public string? StoragePath { get; private set; }
    public string? FileName { get; private set; }

    public string MediaType { get; private set; } = string.Empty;
    public string? MimeType { get; private set; }

    public long? FileSizeBytes { get; private set; }
    public int? Width { get; private set; }
    public int? Height { get; private set; }
    public int? DurationSeconds { get; private set; }

    public string? AltText { get; private set; }
    public string? MetadataJson { get; private set; }
    public byte[]? ContentHash { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public long? CreatedByUserId { get; private set; }
    public long? UpdatedByUserId { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public long? DeletedByUserId { get; private set; }
    public DateTime? RestoreUntil { get; private set; }

    public int Version { get; private set; }

    private MediaAsset()
    {
    }

    public static MediaAsset Create(
        string publicId,
        string storageProvider,
        string url,
        string mediaType,
        string? storagePath,
        string? fileName,
        string? mimeType,
        long? fileSizeBytes,
        int? width,
        int? height,
        int? durationSeconds,
        string? altText,
        string? metadataJson,
        byte[]? contentHash,
        DateTime nowUtc,
        long? actorUserId)
    {
        ValidatePublicId(publicId);
        ValidateStorageProvider(storageProvider);
        ValidateUrl(url);
        ValidateMediaType(mediaType);
        ValidateFileSizeBytes(fileSizeBytes);
        ValidateDimension(width, nameof(width));
        ValidateDimension(height, nameof(height));
        ValidateDurationSeconds(durationSeconds);
        ValidateAltText(altText);
        ValidateFileName(fileName);
        ValidateMimeType(mimeType);

        return new MediaAsset
        {
            PublicId = publicId.Trim(),
            StorageProvider = storageProvider.Trim(),
            Url = url.Trim(),
            StoragePath = NormalizeOptional(storagePath),
            FileName = NormalizeOptional(fileName),
            MediaType = mediaType.Trim(),
            MimeType = NormalizeOptional(mimeType),
            FileSizeBytes = fileSizeBytes,
            Width = width,
            Height = height,
            DurationSeconds = durationSeconds,
            AltText = NormalizeOptional(altText),
            MetadataJson = NormalizeOptional(metadataJson),
            ContentHash = contentHash,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc,
            CreatedByUserId = actorUserId,
            UpdatedByUserId = actorUserId,
            IsDeleted = false,
            Version = 1
        };
    }

    public static MediaAsset Rehydrate(
        long mediaId,
        string publicId,
        string storageProvider,
        string url,
        string? storagePath,
        string? fileName,
        string mediaType,
        string? mimeType,
        long? fileSizeBytes,
        int? width,
        int? height,
        int? durationSeconds,
        string? altText,
        string? metadataJson,
        byte[]? contentHash,
        DateTime createdAt,
        DateTime updatedAt,
        long? createdByUserId,
        long? updatedByUserId,
        bool isDeleted,
        DateTime? deletedAt,
        long? deletedByUserId,
        DateTime? restoreUntil,
        int version)
    {
        if (mediaId <= 0)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_INVALID_MEDIA_ID",
                "Media asset id must be greater than zero.");
        }

        if (version <= 0)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_INVALID_VERSION",
                "Media asset version must be greater than zero.");
        }

        ValidatePublicId(publicId);
        ValidateStorageProvider(storageProvider);
        ValidateUrl(url);
        ValidateMediaType(mediaType);
        ValidateFileSizeBytes(fileSizeBytes);
        ValidateDimension(width, nameof(width));
        ValidateDimension(height, nameof(height));
        ValidateDurationSeconds(durationSeconds);
        ValidateAltText(altText);
        ValidateFileName(fileName);
        ValidateMimeType(mimeType);

        if (isDeleted && deletedAt is null)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_DELETED_AT_REQUIRED",
                "Deleted media asset must have DeletedAt.");
        }

        if (!isDeleted && deletedAt is not null)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_DELETED_AT_INVALID",
                "Active media asset must not have DeletedAt.");
        }

        return new MediaAsset
        {
            MediaId = mediaId,
            PublicId = publicId.Trim(),
            StorageProvider = storageProvider.Trim(),
            Url = url.Trim(),
            StoragePath = NormalizeOptional(storagePath),
            FileName = NormalizeOptional(fileName),
            MediaType = mediaType.Trim(),
            MimeType = NormalizeOptional(mimeType),
            FileSizeBytes = fileSizeBytes,
            Width = width,
            Height = height,
            DurationSeconds = durationSeconds,
            AltText = NormalizeOptional(altText),
            MetadataJson = NormalizeOptional(metadataJson),
            ContentHash = contentHash,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            CreatedByUserId = createdByUserId,
            UpdatedByUserId = updatedByUserId,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt,
            DeletedByUserId = deletedByUserId,
            RestoreUntil = restoreUntil,
            Version = version
        };
    }

    public void UpdateMetadata(
        string storageProvider,
        string url,
        string? storagePath,
        string? fileName,
        string mediaType,
        string? mimeType,
        long? fileSizeBytes,
        int? width,
        int? height,
        int? durationSeconds,
        string? altText,
        string? metadataJson,
        byte[]? contentHash,
        DateTime nowUtc,
        long? actorUserId)
    {
        EnsureNotDeleted();

        ValidateStorageProvider(storageProvider);
        ValidateUrl(url);
        ValidateMediaType(mediaType);
        ValidateFileSizeBytes(fileSizeBytes);
        ValidateDimension(width, nameof(width));
        ValidateDimension(height, nameof(height));
        ValidateDurationSeconds(durationSeconds);
        ValidateAltText(altText);
        ValidateFileName(fileName);
        ValidateMimeType(mimeType);

        StorageProvider = storageProvider.Trim();
        Url = url.Trim();
        StoragePath = NormalizeOptional(storagePath);
        FileName = NormalizeOptional(fileName);
        MediaType = mediaType.Trim();
        MimeType = NormalizeOptional(mimeType);
        FileSizeBytes = fileSizeBytes;
        Width = width;
        Height = height;
        DurationSeconds = durationSeconds;
        AltText = NormalizeOptional(altText);
        MetadataJson = NormalizeOptional(metadataJson);
        ContentHash = contentHash;
        UpdatedAt = nowUtc;
        UpdatedByUserId = actorUserId;
        Version++;
    }

    public void SoftDelete(DateTime nowUtc, long? actorUserId, DateTime? restoreUntil)
    {
        if (IsDeleted)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_ALREADY_DELETED",
                "Media asset is already deleted.");
        }

        if (restoreUntil.HasValue && restoreUntil.Value < nowUtc)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_RESTORE_UNTIL_INVALID",
                "RestoreUntil must be greater than or equal to the current time.");
        }

        IsDeleted = true;
        DeletedAt = nowUtc;
        DeletedByUserId = actorUserId;
        RestoreUntil = restoreUntil;
        UpdatedAt = nowUtc;
        UpdatedByUserId = actorUserId;
        Version++;
    }

    public void Restore(DateTime nowUtc, long? actorUserId)
    {
        if (!IsDeleted)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_NOT_DELETED",
                "Media asset is not deleted.");
        }

        if (RestoreUntil.HasValue && RestoreUntil.Value < nowUtc)
        {
            throw new MediaDomainException(
                "MEDIA.RESTORE_WINDOW_EXPIRED",
                "Restore window has expired.");
        }

        IsDeleted = false;
        DeletedAt = null;
        DeletedByUserId = null;
        RestoreUntil = null;
        UpdatedAt = nowUtc;
        UpdatedByUserId = actorUserId;
        Version++;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_ALREADY_DELETED",
                "Media asset is already deleted.");
        }
    }

    private static void ValidatePublicId(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_PUBLIC_ID_REQUIRED",
                "Media asset public id is required.");
        }

        if (publicId.Trim().Length != 26)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_PUBLIC_ID_INVALID",
                "Media asset public id must be exactly 26 characters.");
        }
    }

    private static void ValidateStorageProvider(string storageProvider)
    {
        if (string.IsNullOrWhiteSpace(storageProvider))
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_STORAGE_PROVIDER_REQUIRED",
                "Storage provider is required.");
        }

        if (storageProvider.Trim().Length > 30)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_STORAGE_PROVIDER_TOO_LONG",
                "Storage provider must not exceed 30 characters.");
        }
    }

    private static void ValidateUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_URL_REQUIRED",
                "Media asset URL is required.");
        }

        if (url.Trim().Length > 800)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_URL_TOO_LONG",
                "Media asset URL must not exceed 800 characters.");
        }
    }

    private static void ValidateMediaType(string mediaType)
    {
        if (!MediaTypes.IsValid(mediaType))
        {
            throw new MediaDomainException(
                "MEDIA.TYPE_NOT_ALLOWED",
                "Media type is not allowed.");
        }
    }

    private static void ValidateFileSizeBytes(long? fileSizeBytes)
    {
        if (fileSizeBytes.HasValue && fileSizeBytes.Value < 0)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_FILE_SIZE_INVALID",
                "File size must be greater than or equal to zero.");
        }
    }

    private static void ValidateDimension(int? value, string fieldName)
    {
        if (value.HasValue && value.Value < 0)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_DIMENSION_INVALID",
                $"{fieldName} must be greater than or equal to zero.");
        }
    }

    private static void ValidateDurationSeconds(int? durationSeconds)
    {
        if (durationSeconds.HasValue && durationSeconds.Value < 0)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_DURATION_INVALID",
                "DurationSeconds must be greater than or equal to zero.");
        }
    }

    private static void ValidateAltText(string? altText)
    {
        if (!string.IsNullOrWhiteSpace(altText) && altText.Trim().Length > 300)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_ALT_TEXT_TOO_LONG",
                "Alt text must not exceed 300 characters.");
        }
    }

    private static void ValidateFileName(string? fileName)
    {
        if (!string.IsNullOrWhiteSpace(fileName) && fileName.Trim().Length > 255)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_FILE_NAME_TOO_LONG",
                "File name must not exceed 255 characters.");
        }
    }

    private static void ValidateMimeType(string? mimeType)
    {
        if (!string.IsNullOrWhiteSpace(mimeType) && mimeType.Trim().Length > 100)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_MIME_TYPE_TOO_LONG",
                "Mime type must not exceed 100 characters.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}