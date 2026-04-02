using Media.Domain.Exceptions;

namespace Media.Domain.Entities;

public sealed class ArticleMedia
{
    public long ArticleMediaId { get; private set; }

    public long ArticleId { get; private set; }
    public long MediaId { get; private set; }

    public int SortOrder { get; private set; }
    public bool IsPrimary { get; private set; }

    public string? AltTextOverride { get; private set; }
    public string? Caption { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public long? CreatedByUserId { get; private set; }
    public long? UpdatedByUserId { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public long? DeletedByUserId { get; private set; }

    public int Version { get; private set; }

    private ArticleMedia()
    {
    }

    public static ArticleMedia Create(
        long articleId,
        long mediaId,
        int sortOrder,
        bool isPrimary,
        string? altTextOverride,
        string? caption,
        DateTime nowUtc,
        long? actorUserId)
    {
        ValidateArticleId(articleId);
        ValidateMediaId(mediaId);
        ValidateSortOrder(sortOrder);
        ValidateAltTextOverride(altTextOverride);
        ValidateCaption(caption);

        return new ArticleMedia
        {
            ArticleId = articleId,
            MediaId = mediaId,
            SortOrder = sortOrder,
            IsPrimary = isPrimary,
            AltTextOverride = NormalizeOptional(altTextOverride),
            Caption = NormalizeOptional(caption),
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc,
            CreatedByUserId = actorUserId,
            UpdatedByUserId = actorUserId,
            IsDeleted = false,
            Version = 1
        };
    }

    public static ArticleMedia Rehydrate(
        long articleMediaId,
        long articleId,
        long mediaId,
        int sortOrder,
        bool isPrimary,
        string? altTextOverride,
        string? caption,
        DateTime createdAt,
        DateTime updatedAt,
        long? createdByUserId,
        long? updatedByUserId,
        bool isDeleted,
        DateTime? deletedAt,
        long? deletedByUserId,
        int version)
    {
        if (articleMediaId <= 0)
        {
            throw new MediaDomainException(
                "MEDIA.ARTICLE_MEDIA_INVALID_ARTICLE_MEDIA_ID",
                "Article media id must be greater than zero.");
        }

        if (version <= 0)
        {
            throw new MediaDomainException(
                "MEDIA.ARTICLE_MEDIA_INVALID_VERSION",
                "Article media version must be greater than zero.");
        }

        ValidateArticleId(articleId);
        ValidateMediaId(mediaId);
        ValidateSortOrder(sortOrder);
        ValidateAltTextOverride(altTextOverride);
        ValidateCaption(caption);

        if (isDeleted && deletedAt is null)
        {
            throw new MediaDomainException(
                "MEDIA.ARTICLE_MEDIA_DELETED_AT_REQUIRED",
                "Deleted article media must have DeletedAt.");
        }

        if (!isDeleted && deletedAt is not null)
        {
            throw new MediaDomainException(
                "MEDIA.ARTICLE_MEDIA_DELETED_AT_INVALID",
                "Active article media must not have DeletedAt.");
        }

        if (isDeleted && isPrimary)
        {
            throw new MediaDomainException(
                "MEDIA.PRIMARY_CONSTRAINT_VIOLATION",
                "Deleted article media cannot remain primary.");
        }

        return new ArticleMedia
        {
            ArticleMediaId = articleMediaId,
            ArticleId = articleId,
            MediaId = mediaId,
            SortOrder = sortOrder,
            IsPrimary = isPrimary,
            AltTextOverride = NormalizeOptional(altTextOverride),
            Caption = NormalizeOptional(caption),
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            CreatedByUserId = createdByUserId,
            UpdatedByUserId = updatedByUserId,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt,
            DeletedByUserId = deletedByUserId,
            Version = version
        };
    }

    public void UpdatePresentation(
        string? altTextOverride,
        string? caption,
        DateTime nowUtc,
        long? actorUserId)
    {
        EnsureNotDeleted();

        ValidateAltTextOverride(altTextOverride);
        ValidateCaption(caption);

        AltTextOverride = NormalizeOptional(altTextOverride);
        Caption = NormalizeOptional(caption);
        UpdatedAt = nowUtc;
        UpdatedByUserId = actorUserId;
        Version++;
    }

    public void ChangeSortOrder(int sortOrder, DateTime nowUtc, long? actorUserId)
    {
        EnsureNotDeleted();

        ValidateSortOrder(sortOrder);

        SortOrder = sortOrder;
        UpdatedAt = nowUtc;
        UpdatedByUserId = actorUserId;
        Version++;
    }

    public void MarkAsPrimary(DateTime nowUtc, long? actorUserId)
    {
        EnsureNotDeleted();

        if (IsPrimary)
        {
            throw new MediaDomainException(
                "MEDIA.ARTICLE_MEDIA_ALREADY_PRIMARY",
                "Article media is already primary.");
        }

        IsPrimary = true;
        UpdatedAt = nowUtc;
        UpdatedByUserId = actorUserId;
        Version++;
    }

    public void UnmarkAsPrimary(DateTime nowUtc, long? actorUserId)
    {
        EnsureNotDeleted();

        if (!IsPrimary)
        {
            return;
        }

        IsPrimary = false;
        UpdatedAt = nowUtc;
        UpdatedByUserId = actorUserId;
        Version++;
    }

    public void SoftDelete(DateTime nowUtc, long? actorUserId)
    {
        if (IsDeleted)
        {
            throw new MediaDomainException(
                "MEDIA.ARTICLE_MEDIA_ALREADY_DELETED",
                "Article media is already deleted.");
        }

        IsDeleted = true;
        IsPrimary = false;
        DeletedAt = nowUtc;
        DeletedByUserId = actorUserId;
        UpdatedAt = nowUtc;
        UpdatedByUserId = actorUserId;
        Version++;
    }

    public void Restore(DateTime nowUtc, long? actorUserId)
    {
        if (!IsDeleted)
        {
            throw new MediaDomainException(
                "MEDIA.ARTICLE_MEDIA_NOT_DELETED",
                "Article media is not deleted.");
        }

        IsDeleted = false;
        DeletedAt = null;
        DeletedByUserId = null;
        UpdatedAt = nowUtc;
        UpdatedByUserId = actorUserId;
        Version++;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new MediaDomainException(
                "MEDIA.ARTICLE_MEDIA_ALREADY_DELETED",
                "Article media is already deleted.");
        }
    }

    private static void ValidateArticleId(long articleId)
    {
        if (articleId <= 0)
        {
            throw new MediaDomainException(
                "MEDIA.ARTICLE_MEDIA_INVALID_ARTICLE_ID",
                "Article id must be greater than zero.");
        }
    }

    private static void ValidateMediaId(long mediaId)
    {
        if (mediaId <= 0)
        {
            throw new MediaDomainException(
                "MEDIA.ARTICLE_MEDIA_INVALID_MEDIA_ID",
                "Media id must be greater than zero.");
        }
    }

    private static void ValidateSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new MediaDomainException(
                "MEDIA.ARTICLE_MEDIA_SORT_ORDER_INVALID",
                "Sort order must be greater than or equal to zero.");
        }
    }

    private static void ValidateAltTextOverride(string? altTextOverride)
    {
        if (!string.IsNullOrWhiteSpace(altTextOverride) && altTextOverride.Trim().Length > 300)
        {
            throw new MediaDomainException(
                "MEDIA.ARTICLE_MEDIA_ALT_TEXT_OVERRIDE_TOO_LONG",
                "Alt text override must not exceed 300 characters.");
        }
    }

    private static void ValidateCaption(string? caption)
    {
        if (!string.IsNullOrWhiteSpace(caption) && caption.Trim().Length > 300)
        {
            throw new MediaDomainException(
                "MEDIA.ARTICLE_MEDIA_CAPTION_TOO_LONG",
                "Caption must not exceed 300 characters.");
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