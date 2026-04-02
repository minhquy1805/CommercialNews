using Media.Domain.Exceptions;

namespace Media.Domain.Entities;

public sealed class MediaVariant
{
    public long VariantId { get; private set; }
    public long MediaId { get; private set; }

    public string VariantType { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;

    public int? Width { get; private set; }
    public int? Height { get; private set; }
    public long? FileSizeBytes { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private MediaVariant()
    {
    }

    public static MediaVariant Create(
        long mediaId,
        string variantType,
        string url,
        int? width,
        int? height,
        long? fileSizeBytes,
        DateTime nowUtc)
    {
        ValidateMediaId(mediaId);
        ValidateVariantType(variantType);
        ValidateUrl(url);
        ValidateDimension(width, nameof(width));
        ValidateDimension(height, nameof(height));
        ValidateFileSizeBytes(fileSizeBytes);

        return new MediaVariant
        {
            MediaId = mediaId,
            VariantType = variantType.Trim(),
            Url = url.Trim(),
            Width = width,
            Height = height,
            FileSizeBytes = fileSizeBytes,
            CreatedAt = nowUtc
        };
    }

    public static MediaVariant Rehydrate(
        long variantId,
        long mediaId,
        string variantType,
        string url,
        int? width,
        int? height,
        long? fileSizeBytes,
        DateTime createdAt)
    {
        if (variantId <= 0)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_VARIANT_INVALID_VARIANT_ID",
                "Media variant id must be greater than zero.");
        }

        ValidateMediaId(mediaId);
        ValidateVariantType(variantType);
        ValidateUrl(url);
        ValidateDimension(width, nameof(width));
        ValidateDimension(height, nameof(height));
        ValidateFileSizeBytes(fileSizeBytes);

        return new MediaVariant
        {
            VariantId = variantId,
            MediaId = mediaId,
            VariantType = variantType.Trim(),
            Url = url.Trim(),
            Width = width,
            Height = height,
            FileSizeBytes = fileSizeBytes,
            CreatedAt = createdAt
        };
    }

    private static void ValidateMediaId(long mediaId)
    {
        if (mediaId <= 0)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_VARIANT_INVALID_MEDIA_ID",
                "Media id must be greater than zero.");
        }
    }

    private static void ValidateVariantType(string variantType)
    {
        if (string.IsNullOrWhiteSpace(variantType))
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_VARIANT_TYPE_REQUIRED",
                "Variant type is required.");
        }

        if (variantType.Trim().Length > 30)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_VARIANT_TYPE_TOO_LONG",
                "Variant type must not exceed 30 characters.");
        }
    }

    private static void ValidateUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_VARIANT_URL_REQUIRED",
                "Variant URL is required.");
        }

        if (url.Trim().Length > 800)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_VARIANT_URL_TOO_LONG",
                "Variant URL must not exceed 800 characters.");
        }
    }

    private static void ValidateDimension(int? value, string fieldName)
    {
        if (value.HasValue && value.Value < 0)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_VARIANT_DIMENSION_INVALID",
                $"{fieldName} must be greater than or equal to zero.");
        }
    }

    private static void ValidateFileSizeBytes(long? fileSizeBytes)
    {
        if (fileSizeBytes.HasValue && fileSizeBytes.Value < 0)
        {
            throw new MediaDomainException(
                "MEDIA.MEDIA_VARIANT_FILE_SIZE_INVALID",
                "File size must be greater than or equal to zero.");
        }
    }
}