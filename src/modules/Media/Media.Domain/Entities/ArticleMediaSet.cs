using Media.Domain.Exceptions;

namespace Media.Domain.Entities;

public sealed class ArticleMediaSet
{
    public long ArticleId { get; private set; }

    // Aggregate version for the article media attachment set.
    // Used by expectedVersion in reorder and set-primary operations.
    // DB default starts at 0; first truth mutation moves it to 1.
    public int Version { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public long? CreatedBy { get; private set; }

    public DateTime UpdatedAt { get; private set; }
    public long? UpdatedBy { get; private set; }

    private ArticleMediaSet()
    {
    }

    public static ArticleMediaSet Create(
        long articleId,
        DateTime nowUtc,
        long? actorUserId)
    {
        ValidateArticleId(articleId);

        return new ArticleMediaSet
        {
            ArticleId = articleId,
            Version = 0,
            CreatedAt = nowUtc,
            CreatedBy = actorUserId,
            UpdatedAt = nowUtc,
            UpdatedBy = actorUserId
        };
    }

    public static ArticleMediaSet Rehydrate(
        long articleId,
        int version,
        DateTime createdAt,
        long? createdBy,
        DateTime updatedAt,
        long? updatedBy)
    {
        ValidateArticleId(articleId);
        ValidateVersion(version);

        return new ArticleMediaSet
        {
            ArticleId = articleId,
            Version = version,
            CreatedAt = createdAt,
            CreatedBy = createdBy,
            UpdatedAt = updatedAt,
            UpdatedBy = updatedBy
        };
    }

    public void EnsureExpectedVersion(int? expectedVersion)
    {
        if (expectedVersion is null)
        {
            throw new MediaDomainException(
                "MEDIA.EXPECTED_VERSION_REQUIRED",
                "Expected version is required.");
        }

        if (expectedVersion.Value != Version)
        {
            throw new MediaDomainException(
                "MEDIA.VERSION_CONFLICT",
                "Article media set version does not match the expected version.");
        }
    }

    public void MarkMutated(
        DateTime nowUtc,
        long? actorUserId)
    {
        Version++;
        UpdatedAt = nowUtc;
        UpdatedBy = actorUserId;
    }

    public bool HasVersion(int expectedVersion)
    {
        return Version == expectedVersion;
    }

    private static void ValidateArticleId(long articleId)
    {
        if (articleId <= 0)
        {
            throw new MediaDomainException(
                "MEDIA.ARTICLE_MEDIA_SET_INVALID_ARTICLE_ID",
                "Article id must be greater than zero.");
        }
    }

    private static void ValidateVersion(int version)
    {
        if (version < 0)
        {
            throw new MediaDomainException(
                "MEDIA.ARTICLE_MEDIA_SET_INVALID_VERSION",
                "Article media set version must be greater than or equal to zero.");
        }
    }
}