using Reading.Domain.Exceptions;

namespace Reading.Domain.Entities;

public sealed class ArticleReadModelMedia
{
    private const int PublicIdLength = 26;
    private const int MaxUrlLength = 1000;
    private const int MaxAltLength = 300;
    private const int MaxCaptionLength = 300;
    private const int MaxMediaTypeLength = 50;

    private ArticleReadModelMedia()
    {
    }

    public long ArticleId { get; private set; }

    public long MediaId { get; private set; }

    public string MediaPublicId { get; private set; } = string.Empty;

    public string Url { get; private set; } = string.Empty;

    public string? Alt { get; private set; }

    public string? Caption { get; private set; }

    public string MediaType { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public bool IsPrimary { get; private set; }

    /// <summary>
    /// Version of the Media article attachment set.
    /// This is not the Content article version and not the MediaAsset version.
    /// </summary>
    public long SourceVersion { get; private set; }

    public DateTime LastSyncedAtUtc { get; private set; }

    public static ArticleReadModelMedia Create(
        long articleId,
        long mediaId,
        string mediaPublicId,
        string url,
        string? alt,
        string? caption,
        string mediaType,
        int sortOrder,
        bool isPrimary,
        long sourceVersion,
        DateTime lastSyncedAtUtc)
    {
        ValidateArticleId(articleId);
        ValidateMediaId(mediaId);
        ValidateMediaPublicId(mediaPublicId);
        ValidateUrl(url);
        ValidateAlt(alt);
        ValidateCaption(caption);
        ValidateMediaType(mediaType);
        ValidateSortOrder(sortOrder);
        ValidateIncomingSourceVersion(sourceVersion);
        ValidateLastSyncedAtUtc(lastSyncedAtUtc);

        return new ArticleReadModelMedia
        {
            ArticleId = articleId,
            MediaId = mediaId,
            MediaPublicId = mediaPublicId.Trim(),
            Url = url.Trim(),
            Alt = NormalizeNullable(alt),
            Caption = NormalizeNullable(caption),
            MediaType = mediaType.Trim(),
            SortOrder = sortOrder,
            IsPrimary = isPrimary,
            SourceVersion = sourceVersion,
            LastSyncedAtUtc = lastSyncedAtUtc
        };
    }

    public bool CanApply(long incomingSourceVersion)
    {
        ValidateIncomingSourceVersion(incomingSourceVersion);

        return incomingSourceVersion > SourceVersion;
    }

    public bool Apply(
        string mediaPublicId,
        string url,
        string? alt,
        string? caption,
        string mediaType,
        int sortOrder,
        bool isPrimary,
        long sourceVersion,
        DateTime lastSyncedAtUtc)
    {
        ValidateMediaPublicId(mediaPublicId);
        ValidateUrl(url);
        ValidateAlt(alt);
        ValidateCaption(caption);
        ValidateMediaType(mediaType);
        ValidateSortOrder(sortOrder);
        ValidateIncomingSourceVersion(sourceVersion);
        ValidateLastSyncedAtUtc(lastSyncedAtUtc);

        if (!CanApply(sourceVersion))
        {
            return false;
        }

        MediaPublicId = mediaPublicId.Trim();
        Url = url.Trim();
        Alt = NormalizeNullable(alt);
        Caption = NormalizeNullable(caption);
        MediaType = mediaType.Trim();
        SortOrder = sortOrder;
        IsPrimary = isPrimary;
        SourceVersion = sourceVersion;
        LastSyncedAtUtc = lastSyncedAtUtc;

        return true;
    }

    private static void ValidateArticleId(long articleId)
    {
        if (articleId <= 0)
        {
            throw new ReadingDomainException(
                "READING.INVALID_ARTICLE_ID",
                "Article id must be greater than zero.");
        }
    }

    private static void ValidateMediaId(long mediaId)
    {
        if (mediaId <= 0)
        {
            throw new ReadingDomainException(
                "READING.INVALID_MEDIA_ID",
                "Media id must be greater than zero.");
        }
    }

    private static void ValidateMediaPublicId(string mediaPublicId)
    {
        if (string.IsNullOrWhiteSpace(mediaPublicId)
            || mediaPublicId.Trim().Length != PublicIdLength)
        {
            throw new ReadingDomainException(
                "READING.INVALID_MEDIA_PUBLIC_ID",
                "Media public id must be a 26-character value.");
        }
    }

    private static void ValidateUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ReadingDomainException(
                "READING.INVALID_MEDIA_URL",
                "Media URL is required.");
        }

        if (url.Trim().Length > MaxUrlLength)
        {
            throw new ReadingDomainException(
                "READING.MEDIA_URL_TOO_LONG",
                $"Media URL must not exceed {MaxUrlLength} characters.");
        }
    }

    private static void ValidateAlt(string? alt)
    {
        if (string.IsNullOrWhiteSpace(alt))
        {
            return;
        }

        if (alt.Trim().Length > MaxAltLength)
        {
            throw new ReadingDomainException(
                "READING.MEDIA_ALT_TOO_LONG",
                $"Media alt text must not exceed {MaxAltLength} characters.");
        }
    }

    private static void ValidateCaption(string? caption)
    {
        if (string.IsNullOrWhiteSpace(caption))
        {
            return;
        }

        if (caption.Trim().Length > MaxCaptionLength)
        {
            throw new ReadingDomainException(
                "READING.MEDIA_CAPTION_TOO_LONG",
                $"Media caption must not exceed {MaxCaptionLength} characters.");
        }
    }

    private static void ValidateMediaType(string mediaType)
    {
        if (string.IsNullOrWhiteSpace(mediaType))
        {
            throw new ReadingDomainException(
                "READING.INVALID_MEDIA_TYPE",
                "Media type is required.");
        }

        if (mediaType.Trim().Length > MaxMediaTypeLength)
        {
            throw new ReadingDomainException(
                "READING.MEDIA_TYPE_TOO_LONG",
                $"Media type must not exceed {MaxMediaTypeLength} characters.");
        }
    }

    private static void ValidateSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new ReadingDomainException(
                "READING.INVALID_SORT_ORDER",
                "Sort order must be non-negative.");
        }
    }

    private static void ValidateIncomingSourceVersion(long sourceVersion)
    {
        if (sourceVersion <= 0)
        {
            throw new ReadingDomainException(
                "READING.INVALID_SOURCE_VERSION",
                "Source version must be greater than zero.");
        }
    }

    private static void ValidateLastSyncedAtUtc(DateTime lastSyncedAtUtc)
    {
        if (lastSyncedAtUtc == default)
        {
            throw new ReadingDomainException(
                "READING.INVALID_LAST_SYNCED_AT_UTC",
                "Last synced timestamp is required.");
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
