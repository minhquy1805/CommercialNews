using Reading.Domain.Constants;
using Reading.Domain.Exceptions;
using Reading.Domain.Policies;

namespace Reading.Domain.Entities;

public sealed class ArticleReadModel
{
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

    public string Status { get; private set; } = SourceArticleStatuses.Draft;

    public bool IsPublic { get; private set; }

    public DateTime? PublishedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public string? SearchText { get; private set; }

    public long ViewCount { get; private set; }

    public long LikeCount { get; private set; }

    public long CommentCount { get; private set; }

    public double? PopularityScore { get; private set; }

    public long SourceVersion { get; private set; }

    public string? LastEventMessageId { get; private set; }

    public DateTime? LastSourceOccurredAtUtc { get; private set; }

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
        ValidateSourceVersion(sourceVersion);

        string normalizedStatus = NormalizeStatus(sourceStatus);

        bool normalizedPublic = ReadingVisibilityPolicy.NormalizePublicFlag(
            normalizedStatus,
            requestedPublic,
            publishedAtUtc);

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

            Status = normalizedStatus,
            IsPublic = normalizedPublic,

            PublishedAtUtc = publishedAtUtc,
            UpdatedAtUtc = updatedAtUtc,

            SearchText = BuildSearchText(
                title,
                summary,
                body,
                categoryName,
                authorDisplayName),

            ViewCount = 0,
            LikeCount = 0,
            CommentCount = 0,
            PopularityScore = null,

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
        ValidateSourceVersion(sourceVersion);

        if (!CanApply(sourceVersion))
        {
            return false;
        }

        string normalizedStatus = NormalizeStatus(sourceStatus);

        bool normalizedPublic = ReadingVisibilityPolicy.NormalizePublicFlag(
            normalizedStatus,
            requestedPublic,
            publishedAtUtc);

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

        PublishedAtUtc = publishedAtUtc;
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
        ValidateSourceVersion(sourceVersion);

        if (!CanApply(sourceVersion))
        {
            return false;
        }

        Status = NormalizeStatus(sourceStatus);
        IsPublic = false;

        SourceVersion = sourceVersion;
        LastEventMessageId = NormalizeMessageId(messageId);
        LastSourceOccurredAtUtc = sourceOccurredAtUtc;
        LastSyncedAtUtc = syncedAtUtc;

        return true;
    }

    public void ApplySeo(
        string? slug,
        string? canonicalUrl,
        string? metaTitle,
        string? metaDescription,
        string? messageId,
        DateTime? sourceOccurredAtUtc,
        DateTime syncedAtUtc)
    {
        Slug = NormalizeNullable(slug);
        CanonicalUrl = NormalizeNullable(canonicalUrl);
        MetaTitle = NormalizeNullable(metaTitle);
        MetaDescription = NormalizeNullable(metaDescription);

        LastEventMessageId = NormalizeMessageId(messageId);
        LastSourceOccurredAtUtc = sourceOccurredAtUtc;
        LastSyncedAtUtc = syncedAtUtc;
    }

    public void ApplyCoverMedia(
        long? coverMediaId,
        string? coverMediaUrl,
        string? coverAlt,
        string? messageId,
        DateTime? sourceOccurredAtUtc,
        DateTime syncedAtUtc)
    {
        CoverMediaId = coverMediaId;
        CoverMediaUrl = NormalizeNullable(coverMediaUrl);
        CoverAlt = NormalizeNullable(coverAlt);

        LastEventMessageId = NormalizeMessageId(messageId);
        LastSourceOccurredAtUtc = sourceOccurredAtUtc;
        LastSyncedAtUtc = syncedAtUtc;
    }

    public void ApplyCounters(
        long viewCount,
        long likeCount,
        long commentCount,
        double? popularityScore,
        string? messageId,
        DateTime? sourceOccurredAtUtc,
        DateTime syncedAtUtc)
    {
        if (viewCount < 0 || likeCount < 0 || commentCount < 0)
        {
            throw new ReadingDomainException(
                "READING.INVALID_COUNTERS",
                "Counters must be non-negative.");
        }

        ViewCount = viewCount;
        LikeCount = likeCount;
        CommentCount = commentCount;
        PopularityScore = popularityScore;

        LastEventMessageId = NormalizeMessageId(messageId);
        LastSourceOccurredAtUtc = sourceOccurredAtUtc;
        LastSyncedAtUtc = syncedAtUtc;
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

    private static void ValidateIdentity(long articleId, string articlePublicId)
    {
        if (articleId <= 0)
        {
            throw new ReadingDomainException(
                "READING.INVALID_ARTICLE_ID",
                "Article id must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(articlePublicId) || articlePublicId.Trim().Length != 26)
        {
            throw new ReadingDomainException(
                "READING.INVALID_ARTICLE_PUBLIC_ID",
                "Article public id must be a 26-character value.");
        }
    }

    private static void ValidateContent(string title, string summary, string body)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ReadingDomainException(
                "READING.INVALID_TITLE",
                "Title is required.");
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ReadingDomainException(
                "READING.INVALID_SUMMARY",
                "Summary is required.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ReadingDomainException(
                "READING.INVALID_BODY",
                "Body is required.");
        }
    }

    private static void ValidateSourceVersion(long sourceVersion)
    {
        if (sourceVersion <= 0)
        {
            throw new ReadingDomainException(
                "READING.INVALID_SOURCE_VERSION",
                "Source version must be greater than zero.");
        }
    }

    private static string? NormalizeMessageId(string? messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            return null;
        }

        string trimmed = messageId.Trim();

        if (trimmed.Length != 26)
        {
            throw new ReadingDomainException(
                "READING.INVALID_MESSAGE_ID",
                "Message id must be a 26-character value.");
        }

        return trimmed;
    }

    private static string? NormalizeNullable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
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
                title,
                summary,
                body,
                categoryName,
                authorDisplayName
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}