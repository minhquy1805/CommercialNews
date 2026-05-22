using Reading.Domain.Exceptions;

namespace Reading.Domain.Entities;

public sealed class ArticleReadModelMedia
{
    private ArticleReadModelMedia()
    {
    }

    public long ArticleId { get; private set; }

    public long MediaId { get; private set; }

    public string Url { get; private set; } = string.Empty;

    public string? Alt { get; private set; }

    public string MediaType { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public bool IsPrimary { get; private set; }

    public long SourceVersion { get; private set; }

    public DateTime LastSyncedAtUtc { get; private set; }

    public static ArticleReadModelMedia Create(
        long articleId,
        long mediaId,
        string url,
        string? alt,
        string mediaType,
        int sortOrder,
        bool isPrimary,
        long sourceVersion,
        DateTime lastSyncedAtUtc)
    {
        ValidateArticleId(articleId);
        ValidateMediaId(mediaId);
        ValidateUrl(url);
        ValidateMediaType(mediaType);
        ValidateSortOrder(sortOrder);
        ValidateSourceVersion(sourceVersion);

        return new ArticleReadModelMedia
        {
            ArticleId = articleId,
            MediaId = mediaId,
            Url = url.Trim(),
            Alt = NormalizeNullable(alt),
            MediaType = mediaType.Trim(),
            SortOrder = sortOrder,
            IsPrimary = isPrimary,
            SourceVersion = sourceVersion,
            LastSyncedAtUtc = lastSyncedAtUtc
        };
    }

    public bool CanApply(long incomingSourceVersion)
    {
        return incomingSourceVersion > SourceVersion;
    }

    public bool Apply(
        string url,
        string? alt,
        string mediaType,
        int sortOrder,
        bool isPrimary,
        long sourceVersion,
        DateTime lastSyncedAtUtc)
    {
        ValidateUrl(url);
        ValidateMediaType(mediaType);
        ValidateSortOrder(sortOrder);
        ValidateSourceVersion(sourceVersion);

        if (!CanApply(sourceVersion))
        {
            return false;
        }

        Url = url.Trim();
        Alt = NormalizeNullable(alt);
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

    private static void ValidateUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ReadingDomainException(
                "READING.INVALID_MEDIA_URL",
                "Media URL is required.");
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

    private static void ValidateSourceVersion(long sourceVersion)
    {
        if (sourceVersion < 0)
        {
            throw new ReadingDomainException(
                "READING.INVALID_SOURCE_VERSION",
                "Source version must be non-negative.");
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}