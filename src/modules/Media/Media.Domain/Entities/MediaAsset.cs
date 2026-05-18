using Media.Domain.Constants;
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

    public int Version { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public long? CreatedBy { get; private set; }

    public DateTime UpdatedAt { get; private set; }
    public long? UpdatedBy { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public long? DeletedBy { get; private set; }
    public DateTime? RestoreUntil { get; private set; }

    public DateTime? RestoredAt { get; private set; }
    public long? RestoredBy { get; private set; }

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
        ValidateStoragePath(storagePath);
        ValidateFileName(fileName);
        ValidateMimeType(mimeType);
        ValidateFileSizeBytes(fileSizeBytes);
        ValidateDimension(width, nameof(width));
        ValidateDimension(height, nameof(height));
        ValidateDurationSeconds(durationSeconds);
        ValidateAltText(altText);

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

            Version = 1,

            CreatedAt = nowUtc,
            CreatedBy = actorUserId,
            UpdatedAt = nowUtc,
            UpdatedBy = actorUserId,

            IsDeleted = false,
            DeletedAt = null,
            DeletedBy = null,
            RestoreUntil = null,
            RestoredAt = null,
            RestoredBy = null
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
        int version,
        DateTime createdAt,
        long? createdBy,
        DateTime updatedAt,
        long? updatedBy,
        bool isDeleted,
        DateTime? deletedAt,
        long? deletedBy,
        DateTime? restoreUntil,
        DateTime? restoredAt,
        long? restoredBy)
    {
        if (mediaId <= 0)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_INVALID_MEDIA_ID",
                "Media asset id must be greater than zero.");
        }

        ValidatePublicId(publicId);
        ValidateStorageProvider(storageProvider);
        ValidateUrl(url);
        ValidateMediaType(mediaType);
        ValidateStoragePath(storagePath);
        ValidateFileName(fileName);
        ValidateMimeType(mimeType);
        ValidateFileSizeBytes(fileSizeBytes);
        ValidateDimension(width, nameof(width));
        ValidateDimension(height, nameof(height));
        ValidateDurationSeconds(durationSeconds);
        ValidateAltText(altText);
        ValidateVersion(version);
        ValidateDeleteState(isDeleted, deletedAt, deletedBy);
        ValidateRestoreState(restoredAt, restoredBy);

        if (isDeleted &&
            restoreUntil.HasValue &&
            deletedAt.HasValue &&
            restoreUntil.Value < deletedAt.Value)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_RESTORE_UNTIL_INVALID",
                "RestoreUntil must be greater than or equal to DeletedAt.");
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

            Version = version,

            CreatedAt = createdAt,
            CreatedBy = createdBy,
            UpdatedAt = updatedAt,
            UpdatedBy = updatedBy,

            IsDeleted = isDeleted,
            DeletedAt = deletedAt,
            DeletedBy = deletedBy,
            RestoreUntil = restoreUntil,
            RestoredAt = restoredAt,
            RestoredBy = restoredBy
        };
    }

    public void UpdateSafeMetadata(
        string? altText,
        string? metadataJson,
        DateTime nowUtc,
        long? actorUserId)
    {
        EnsureNotDeleted();

        ValidateAltText(altText);

        var normalizedAltText = NormalizeOptional(altText);
        var normalizedMetadataJson = NormalizeOptional(metadataJson);

        if (AltText == normalizedAltText &&
            MetadataJson == normalizedMetadataJson)
        {
            return;
        }

        AltText = normalizedAltText;
        MetadataJson = normalizedMetadataJson;

        Touch(nowUtc, actorUserId);
    }

    public void SoftDelete(
        DateTime nowUtc,
        long? actorUserId,
        DateTime? restoreUntil)
    {
        if (IsDeleted)
        {
            return;
        }

        if (restoreUntil.HasValue && restoreUntil.Value < nowUtc)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_RESTORE_UNTIL_INVALID",
                "RestoreUntil must be greater than or equal to the current time.");
        }

        IsDeleted = true;
        DeletedAt = nowUtc;
        DeletedBy = actorUserId;
        RestoreUntil = restoreUntil;

        RestoredAt = null;
        RestoredBy = null;

        Touch(nowUtc, actorUserId);
    }

    public void Restore(
        DateTime nowUtc,
        long? actorUserId)
    {
        if (!IsDeleted)
        {
            return;
        }

        if (RestoreUntil.HasValue && RestoreUntil.Value < nowUtc)
        {
            throw new MediaDomainException(
                "MEDIA.RESTORE_WINDOW_EXPIRED",
                "Restore window has expired.");
        }

        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        RestoreUntil = null;

        RestoredAt = nowUtc;
        RestoredBy = actorUserId;

        Touch(nowUtc, actorUserId);
    }

    private void Touch(
        DateTime nowUtc,
        long? actorUserId)
    {
        UpdatedAt = nowUtc;
        UpdatedBy = actorUserId;
        Version++;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_DELETED",
                "Deleted media asset cannot be changed.");
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

    private static void ValidateStoragePath(string? storagePath)
    {
        if (!string.IsNullOrWhiteSpace(storagePath) && storagePath.Trim().Length > 800)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_STORAGE_PATH_TOO_LONG",
                "Storage path must not exceed 800 characters.");
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

    private static void ValidateVersion(int version)
    {
        if (version <= 0)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_INVALID_VERSION",
                "Media asset version must be greater than zero.");
        }
    }

    private static void ValidateDeleteState(
        bool isDeleted,
        DateTime? deletedAt,
        long? deletedBy)
    {
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

        if (!isDeleted && deletedBy is not null)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_DELETED_BY_INVALID",
                "Active media asset must not have DeletedBy.");
        }
    }

    private static void ValidateRestoreState(
        DateTime? restoredAt,
        long? restoredBy)
    {
        if (restoredAt is null && restoredBy is not null)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_ASSET_RESTORED_AT_REQUIRED",
                "Restored media asset must have RestoredAt when RestoredBy exists.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}