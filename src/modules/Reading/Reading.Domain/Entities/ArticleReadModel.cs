using Reading.Domain.Constants;
using Reading.Domain.Exceptions;
using Reading.Domain.Policies;

namespace Reading.Domain.Entities;

public sealed class ArticleReadModel
{
    private const int PublicIdLength = 26;

    private const int MaxSlugLength = 200;
    private const int MaxTitleLength = 300;
    private const int MaxSummaryLength = 1000;
    private const int MaxCategoryNameLength = 200;
    private const int MaxAuthorDisplayNameLength = 200;

    private const int MaxCoverMediaUrlLength = 1000;
    private const int MaxCoverAltLength = 300;

    private const int MaxCanonicalUrlLength = 500;
    private const int MaxMetaTitleLength = 300;
    private const int MaxMetaDescriptionLength = 500;

    private const int MaxOgTitleLength = 300;
    private const int MaxOgDescriptionLength = 500;
    private const int MaxOgImageUrlLength = 800;

    private const int MaxTwitterTitleLength = 300;
    private const int MaxTwitterDescriptionLength = 500;
    private const int MaxTwitterImageUrlLength = 800;

    private const int MaxRobotsLength = 100;

    private ArticleReadModel()
    {
    }

    public long ArticleId { get; private set; }

    public string ArticlePublicId { get; private set; } = string.Empty;

    public string? Slug { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Summary { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public long? CategoryId { get; private set; }

    public string? CategoryName { get; private set; }

    public long? AuthorUserId { get; private set; }

    public string? AuthorDisplayName { get; private set; }

    public long? CoverMediaId { get; private set; }

    public string? CoverMediaUrl { get; private set; }

    public string? CoverAlt { get; private set; }

    public string? CanonicalUrl { get; private set; }

    public string? MetaTitle { get; private set; }

    public string? MetaDescription { get; private set; }

    public string? OgTitle { get; private set; }

    public string? OgDescription { get; private set; }

    public string? OgImageUrl { get; private set; }

    public string? TwitterTitle { get; private set; }

    public string? TwitterDescription { get; private set; }

    public string? TwitterImageUrl { get; private set; }

    public string? Robots { get; private set; }

    public bool SeoIsManualOverride { get; private set; }

    public bool SeoRouteIsActive { get; private set; }

    public bool SeoIsIndexable { get; private set; }

    public string Status { get; private set; } = SourceArticleStatuses.Draft;

    public bool IsPublic { get; private set; }

    public DateTime? PublishedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public string? SearchText { get; private set; }

    /// <summary>
    /// Version of the Content article projection only.
    /// This must not be overwritten by SEO, Media, or Interaction enrichment.
    /// </summary>
    public long SourceVersion { get; private set; }

    /// <summary>
    /// Message id of the latest applied Content article event only.
    /// </summary>
    public string? LastEventMessageId { get; private set; }

    /// <summary>
    /// Occurred timestamp of the latest applied Content article event only.
    /// </summary>
    public DateTime? LastSourceOccurredAtUtc { get; private set; }

    /// <summary>
    /// Synchronization timestamp of the Content article projection only.
    /// </summary>
    public DateTime LastSyncedAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public bool CanServePublicly()
    {
        return ReadingVisibilityPolicy.CanServePublicly(
            Status,
            IsPublic,
            PublishedAtUtc);
    }

    public bool CanApply(long incomingSourceVersion)
    {
        ValidateIncomingSourceVersion(incomingSourceVersion);

        return incomingSourceVersion > SourceVersion;
    }

    public static ArticleReadModel CreateFromContent(
        long articleId,
        string articlePublicId,
        string title,
        string summary,
        string body,
        long? categoryId,
        string? categoryName,
        long? authorUserId,
        string? authorDisplayName,
        long? coverMediaId,
        string sourceStatus,
        bool requestedPublic,
        DateTime? publishedAtUtc,
        DateTime updatedAtUtc,
        long sourceVersion,
        string? messageId,
        DateTime? sourceOccurredAtUtc,
        DateTime syncedAtUtc)
    {
        ValidateIdentity(articleId, articlePublicId);
        ValidateContent(title, summary, body);
        ValidateCategory(categoryId, categoryName);
        ValidateAuthor(authorUserId, authorDisplayName);
        ValidateCoverMediaId(coverMediaId);
        ValidateIncomingSourceVersion(sourceVersion);
        ValidateMessageId(messageId);
        ValidateRequiredTimestamp(updatedAtUtc, "READING.INVALID_UPDATED_AT_UTC", "Updated timestamp is required.");
        ValidateRequiredTimestamp(syncedAtUtc, "READING.INVALID_LAST_SYNCED_AT_UTC", "Last synced timestamp is required.");

        string normalizedStatus = NormalizeStatus(sourceStatus);
        DateTime? effectivePublishedAtUtc = NormalizePublishedAt(
            normalizedStatus,
            publishedAtUtc);

        bool normalizedPublic = ReadingVisibilityPolicy.NormalizePublicFlag(
            normalizedStatus,
            requestedPublic,
            effectivePublishedAtUtc);

        return new ArticleReadModel
        {
            ArticleId = articleId,
            ArticlePublicId = articlePublicId.Trim(),

            Title = title.Trim(),
            Summary = summary.Trim(),
            Body = body,

            CategoryId = categoryId,
            CategoryName = NormalizeNullable(categoryName),

            AuthorUserId = authorUserId,
            AuthorDisplayName = NormalizeNullable(authorDisplayName),

            CoverMediaId = coverMediaId,
            CoverMediaUrl = null,
            CoverAlt = null,

            Status = normalizedStatus,
            IsPublic = normalizedPublic,
            PublishedAtUtc = effectivePublishedAtUtc,
            UpdatedAtUtc = updatedAtUtc,

            SearchText = BuildSearchText(
                title,
                summary,
                body,
                categoryName,
                authorDisplayName),

            SourceVersion = sourceVersion,
            LastEventMessageId = NormalizeMessageId(messageId),
            LastSourceOccurredAtUtc = sourceOccurredAtUtc,
            LastSyncedAtUtc = syncedAtUtc,
            CreatedAtUtc = syncedAtUtc
        };
    }

    public bool ApplyContent(
        string articlePublicId,
        string title,
        string summary,
        string body,
        long? categoryId,
        string? categoryName,
        long? authorUserId,
        string? authorDisplayName,
        string sourceStatus,
        bool requestedPublic,
        DateTime? publishedAtUtc,
        DateTime updatedAtUtc,
        long sourceVersion,
        string? messageId,
        DateTime? sourceOccurredAtUtc,
        DateTime syncedAtUtc)
    {
        ValidateIdentity(ArticleId, articlePublicId);
        ValidateContent(title, summary, body);
        ValidateCategory(categoryId, categoryName);
        ValidateAuthor(authorUserId, authorDisplayName);
        ValidateIncomingSourceVersion(sourceVersion);
        ValidateMessageId(messageId);
        ValidateRequiredTimestamp(updatedAtUtc, "READING.INVALID_UPDATED_AT_UTC", "Updated timestamp is required.");
        ValidateRequiredTimestamp(syncedAtUtc, "READING.INVALID_LAST_SYNCED_AT_UTC", "Last synced timestamp is required.");

        if (!CanApply(sourceVersion))
        {
            return false;
        }

        string normalizedStatus = NormalizeStatus(sourceStatus);

        DateTime? effectivePublishedAtUtc = ResolvePublishedAtForContentApply(
            normalizedStatus,
            publishedAtUtc);

        bool normalizedPublic = ReadingVisibilityPolicy.NormalizePublicFlag(
            normalizedStatus,
            requestedPublic,
            effectivePublishedAtUtc);

        ArticlePublicId = articlePublicId.Trim();

        Title = title.Trim();
        Summary = summary.Trim();
        Body = body;

        CategoryId = categoryId;
        CategoryName = NormalizeNullable(categoryName);

        AuthorUserId = authorUserId;
        AuthorDisplayName = NormalizeNullable(authorDisplayName);

        Status = normalizedStatus;
        IsPublic = normalizedPublic;
        PublishedAtUtc = effectivePublishedAtUtc;
        UpdatedAtUtc = updatedAtUtc;

        SearchText = BuildSearchText(
            title,
            summary,
            body,
            categoryName,
            authorDisplayName);

        SourceVersion = sourceVersion;
        LastEventMessageId = NormalizeMessageId(messageId);
        LastSourceOccurredAtUtc = sourceOccurredAtUtc;
        LastSyncedAtUtc = syncedAtUtc;

        return true;
    }

    public bool MarkNotPublic(
        string sourceStatus,
        long sourceVersion,
        string? messageId,
        DateTime? sourceOccurredAtUtc,
        DateTime syncedAtUtc)
    {
        ValidateIncomingSourceVersion(sourceVersion);
        ValidateMessageId(messageId);
        ValidateRequiredTimestamp(
            syncedAtUtc,
            "READING.INVALID_LAST_SYNCED_AT_UTC",
            "Last synced timestamp is required.");

        if (!CanApply(sourceVersion))
        {
            return false;
        }

        Status = NormalizeStatus(sourceStatus);
        IsPublic = false;

        if (!SourceArticleStatuses.IsPublished(Status))
        {
            PublishedAtUtc = null;
        }

        SourceVersion = sourceVersion;
        LastEventMessageId = NormalizeMessageId(messageId);
        LastSourceOccurredAtUtc = sourceOccurredAtUtc;
        LastSyncedAtUtc = syncedAtUtc;

        return true;
    }

    /// <summary>
    /// Applies denormalized SEO route fields only.
    /// SEO route checkpoint is owned by ArticleSeoRouteProjection.
    /// </summary>
    public void ApplySeoRoute(
        string? slug,
        string? canonicalUrl,
        bool seoRouteIsActive,
        bool seoIsIndexable)
    {
        ValidateOptionalLength(
            slug,
            MaxSlugLength,
            "READING.SLUG_TOO_LONG",
            "Slug");

        ValidateOptionalLength(
            canonicalUrl,
            MaxCanonicalUrlLength,
            "READING.CANONICAL_URL_TOO_LONG",
            "Canonical URL");

        Slug = NormalizeNullable(slug);
        CanonicalUrl = NormalizeNullable(canonicalUrl);

        SeoRouteIsActive = seoRouteIsActive;
        SeoIsIndexable = seoRouteIsActive && seoIsIndexable;
    }

    /// <summary>
    /// Applies denormalized SEO metadata fields only.
    /// SEO metadata checkpoint is owned by ArticleSeoMetadataProjection.
    /// </summary>
    public void ApplySeoMetadata(
        string? metaTitle,
        string? metaDescription,
        string? ogTitle,
        string? ogDescription,
        string? ogImageUrl,
        string? twitterTitle,
        string? twitterDescription,
        string? twitterImageUrl,
        string? robots,
        bool seoIsManualOverride)
    {
        ValidateOptionalLength(
            metaTitle,
            MaxMetaTitleLength,
            "READING.META_TITLE_TOO_LONG",
            "Meta title");

        ValidateOptionalLength(
            metaDescription,
            MaxMetaDescriptionLength,
            "READING.META_DESCRIPTION_TOO_LONG",
            "Meta description");

        ValidateOptionalLength(
            ogTitle,
            MaxOgTitleLength,
            "READING.OG_TITLE_TOO_LONG",
            "Open Graph title");

        ValidateOptionalLength(
            ogDescription,
            MaxOgDescriptionLength,
            "READING.OG_DESCRIPTION_TOO_LONG",
            "Open Graph description");

        ValidateOptionalLength(
            ogImageUrl,
            MaxOgImageUrlLength,
            "READING.OG_IMAGE_URL_TOO_LONG",
            "Open Graph image URL");

        ValidateOptionalLength(
            twitterTitle,
            MaxTwitterTitleLength,
            "READING.TWITTER_TITLE_TOO_LONG",
            "Twitter title");

        ValidateOptionalLength(
            twitterDescription,
            MaxTwitterDescriptionLength,
            "READING.TWITTER_DESCRIPTION_TOO_LONG",
            "Twitter description");

        ValidateOptionalLength(
            twitterImageUrl,
            MaxTwitterImageUrlLength,
            "READING.TWITTER_IMAGE_URL_TOO_LONG",
            "Twitter image URL");

        ValidateOptionalLength(
            robots,
            MaxRobotsLength,
            "READING.ROBOTS_TOO_LONG",
            "Robots value");

        MetaTitle = NormalizeNullable(metaTitle);
        MetaDescription = NormalizeNullable(metaDescription);

        OgTitle = NormalizeNullable(ogTitle);
        OgDescription = NormalizeNullable(ogDescription);
        OgImageUrl = NormalizeNullable(ogImageUrl);

        TwitterTitle = NormalizeNullable(twitterTitle);
        TwitterDescription = NormalizeNullable(twitterDescription);
        TwitterImageUrl = NormalizeNullable(twitterImageUrl);

        Robots = NormalizeNullable(robots);
        SeoIsManualOverride = seoIsManualOverride;
    }

    /// <summary>
    /// Applies denormalized cover fields only.
    /// Media checkpoint is owned by ArticleMediaProjectionState.
    /// </summary>
    public void ApplyCoverMedia(
        long? coverMediaId,
        string? coverMediaUrl,
        string? coverAlt)
    {
        ValidateCoverMediaId(coverMediaId);

        ValidateOptionalLength(
            coverMediaUrl,
            MaxCoverMediaUrlLength,
            "READING.COVER_MEDIA_URL_TOO_LONG",
            "Cover media URL");

        ValidateOptionalLength(
            coverAlt,
            MaxCoverAltLength,
            "READING.COVER_ALT_TOO_LONG",
            "Cover alt text");

        CoverMediaId = coverMediaId;
        CoverMediaUrl = NormalizeNullable(coverMediaUrl);
        CoverAlt = NormalizeNullable(coverAlt);
    }

    private DateTime? ResolvePublishedAtForContentApply(
        string normalizedStatus,
        DateTime? publishedAtUtc)
    {
        if (!SourceArticleStatuses.IsPublished(normalizedStatus))
        {
            return null;
        }

        return publishedAtUtc ?? PublishedAtUtc;
    }

    private static DateTime? NormalizePublishedAt(
        string normalizedStatus,
        DateTime? publishedAtUtc)
    {
        return SourceArticleStatuses.IsPublished(normalizedStatus)
            ? publishedAtUtc
            : null;
    }

    private static string NormalizeStatus(string sourceStatus)
    {
        string? normalized = SourceArticleStatuses.NormalizeOrNull(sourceStatus);

        if (normalized is null)
        {
            throw new ReadingDomainException(
                "READING.INVALID_SOURCE_STATUS",
                "Source article status is invalid.");
        }

        return normalized;
    }

    private static void ValidateIdentity(
        long articleId,
        string articlePublicId)
    {
        if (articleId <= 0)
        {
            throw new ReadingDomainException(
                "READING.INVALID_ARTICLE_ID",
                "Article id must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(articlePublicId)
            || articlePublicId.Trim().Length != PublicIdLength)
        {
            throw new ReadingDomainException(
                "READING.INVALID_ARTICLE_PUBLIC_ID",
                "Article public id must be a 26-character value.");
        }
    }

    private static void ValidateContent(
        string title,
        string summary,
        string body)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ReadingDomainException(
                "READING.INVALID_TITLE",
                "Title is required.");
        }

        if (title.Trim().Length > MaxTitleLength)
        {
            throw new ReadingDomainException(
                "READING.TITLE_TOO_LONG",
                $"Title must not exceed {MaxTitleLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ReadingDomainException(
                "READING.INVALID_SUMMARY",
                "Summary is required.");
        }

        if (summary.Trim().Length > MaxSummaryLength)
        {
            throw new ReadingDomainException(
                "READING.SUMMARY_TOO_LONG",
                $"Summary must not exceed {MaxSummaryLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ReadingDomainException(
                "READING.INVALID_BODY",
                "Body is required.");
        }
    }

    private static void ValidateCategory(
        long? categoryId,
        string? categoryName)
    {
        if (categoryId.HasValue && categoryId.Value <= 0)
        {
            throw new ReadingDomainException(
                "READING.INVALID_CATEGORY_ID",
                "Category id must be greater than zero when provided.");
        }

        ValidateOptionalLength(
            categoryName,
            MaxCategoryNameLength,
            "READING.CATEGORY_NAME_TOO_LONG",
            "Category name");
    }

    private static void ValidateAuthor(
        long? authorUserId,
        string? authorDisplayName)
    {
        if (authorUserId.HasValue && authorUserId.Value <= 0)
        {
            throw new ReadingDomainException(
                "READING.INVALID_AUTHOR_USER_ID",
                "Author user id must be greater than zero when provided.");
        }

        ValidateOptionalLength(
            authorDisplayName,
            MaxAuthorDisplayNameLength,
            "READING.AUTHOR_DISPLAY_NAME_TOO_LONG",
            "Author display name");
    }

    private static void ValidateCoverMediaId(long? coverMediaId)
    {
        if (coverMediaId.HasValue && coverMediaId.Value <= 0)
        {
            throw new ReadingDomainException(
                "READING.INVALID_COVER_MEDIA_ID",
                "Cover media id must be greater than zero when provided.");
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

    private static void ValidateMessageId(string? messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            return;
        }

        if (messageId.Trim().Length != PublicIdLength)
        {
            throw new ReadingDomainException(
                "READING.INVALID_MESSAGE_ID",
                "Message id must be a 26-character value.");
        }
    }

    private static void ValidateRequiredTimestamp(
        DateTime value,
        string errorCode,
        string errorMessage)
    {
        if (value == default)
        {
            throw new ReadingDomainException(
                errorCode,
                errorMessage);
        }
    }

    private static void ValidateOptionalLength(
        string? value,
        int maxLength,
        string errorCode,
        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (value.Trim().Length > maxLength)
        {
            throw new ReadingDomainException(
                errorCode,
                $"{fieldName} must not exceed {maxLength} characters.");
        }
    }

    private static string? NormalizeMessageId(string? messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            return null;
        }

        ValidateMessageId(messageId);

        return messageId.Trim();
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string BuildSearchText(
        string title,
        string summary,
        string body,
        string? categoryName,
        string? authorDisplayName)
    {
        return string.Join(
            ' ',
            new[]
            {
                title.Trim(),
                summary.Trim(),
                body,
                NormalizeNullable(categoryName),
                NormalizeNullable(authorDisplayName)
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}
