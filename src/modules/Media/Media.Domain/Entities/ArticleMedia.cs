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

    // Row-level version.
    // Attachment-set concurrency is handled by ArticleMediaSet.Version.
    public int Version { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public long? CreatedBy { get; private set; }

    public DateTime UpdatedAt { get; private set; }
    public long? UpdatedBy { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public long? DeletedBy { get; private set; }

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
            Version = 1,

            CreatedAt = nowUtc,
            CreatedBy = actorUserId,
            UpdatedAt = nowUtc,
            UpdatedBy = actorUserId,

            IsDeleted = false,
            DeletedAt = null,
            DeletedBy = null
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
        int version,
        DateTime createdAt,
        long? createdBy,
        DateTime updatedAt,
        long? updatedBy,
        bool isDeleted,
        DateTime? deletedAt,
        long? deletedBy)
    {
        if (articleMediaId <= 0)
        {
            throw new MediaDomainException(
                "MEDIA.ARTICLE_MEDIA_INVALID_ARTICLE_MEDIA_ID",
                "Article media id must be greater than zero.");
        }

        ValidateArticleId(articleId);
        ValidateMediaId(mediaId);
        ValidateSortOrder(sortOrder);
        ValidateAltTextOverride(altTextOverride);
        ValidateCaption(caption);
        ValidateVersion(version);
        ValidateDeleteState(isDeleted, deletedAt, deletedBy, isPrimary);

        return new ArticleMedia
        {
            ArticleMediaId = articleMediaId,
            ArticleId = articleId,
            MediaId = mediaId,
            SortOrder = sortOrder,
            IsPrimary = isPrimary,
            AltTextOverride = NormalizeOptional(altTextOverride),
            Caption = NormalizeOptional(caption),
            Version = version,

            CreatedAt = createdAt,
            CreatedBy = createdBy,
            UpdatedAt = updatedAt,
            UpdatedBy = updatedBy,

            IsDeleted = isDeleted,
            DeletedAt = deletedAt,
            DeletedBy = deletedBy
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

        var normalizedAltTextOverride = NormalizeOptional(altTextOverride);
        var normalizedCaption = NormalizeOptional(caption);

        if (AltTextOverride == normalizedAltTextOverride &&
            Caption == normalizedCaption)
        {
            return;
        }

        AltTextOverride = normalizedAltTextOverride;
        Caption = normalizedCaption;
        Touch(nowUtc, actorUserId);
    }

    public void ChangeSortOrder(
        int sortOrder,
        DateTime nowUtc,
        long? actorUserId)
    {
        EnsureNotDeleted();
        ValidateSortOrder(sortOrder);

        if (SortOrder == sortOrder)
        {
            return;
        }

        SortOrder = sortOrder;
        Touch(nowUtc, actorUserId);
    }

    public void MarkAsPrimary(
        DateTime nowUtc,
        long? actorUserId)
    {
        EnsureNotDeleted();

        if (IsPrimary)
        {
            return;
        }

        IsPrimary = true;
        Touch(nowUtc, actorUserId);
    }

    public void UnmarkAsPrimary(
        DateTime nowUtc,
        long? actorUserId)
    {
        EnsureNotDeleted();

        if (!IsPrimary)
        {
            return;
        }

        IsPrimary = false;
        Touch(nowUtc, actorUserId);
    }

    public void SoftDelete(
        DateTime nowUtc,
        long? actorUserId)
    {
        if (IsDeleted)
        {
            return;
        }

        IsPrimary = false;
        IsDeleted = true;
        DeletedAt = nowUtc;
        DeletedBy = actorUserId;

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

        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;

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

    private static void ValidateVersion(int version)
    {
        if (version <= 0)
        {
            throw new MediaDomainException(
                "MEDIA.ARTICLE_MEDIA_INVALID_VERSION",
                "Article media version must be greater than zero.");
        }
    }

    private static void ValidateDeleteState(
        bool isDeleted,
        DateTime? deletedAt,
        long? deletedBy,
        bool isPrimary)
    {
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

        if (!isDeleted && deletedBy is not null)
        {
            throw new MediaDomainException(
                "MEDIA.ARTICLE_MEDIA_DELETED_BY_INVALID",
                "Active article media must not have DeletedBy.");
        }

        if (isDeleted && isPrimary)
        {
            throw new MediaDomainException(
                "MEDIA.PRIMARY_CONSTRAINT_VIOLATION",
                "Deleted article media cannot remain primary.");
        }
    }

    private static void ValidateAltTextOverride(string? altTextOverride)
    {
        if (!string.IsNullOrWhiteSpace(altTextOverride) &&
            altTextOverride.Trim().Length > 300)
        {
            throw new MediaDomainException(
                "MEDIA.ARTICLE_MEDIA_ALT_TEXT_OVERRIDE_TOO_LONG",
                "Alt text override must not exceed 300 characters.");
        }
    }

    private static void ValidateCaption(string? caption)
    {
        if (!string.IsNullOrWhiteSpace(caption) &&
            caption.Trim().Length > 300)
        {
            throw new MediaDomainException(
                "MEDIA.ARTICLE_MEDIA_CAPTION_TOO_LONG",
                "Caption must not exceed 300 characters.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}