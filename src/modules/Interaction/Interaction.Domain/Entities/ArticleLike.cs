using Interaction.Domain.Exceptions;

namespace Interaction.Domain.Entities;

public sealed class ArticleLike
{
    public long ArticleLikeId { get; private set; }

    public string PublicId { get; private set; } = string.Empty;
    public string ArticlePublicId { get; private set; } = string.Empty;
    public long UserId { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime LikedAtUtc { get; private set; }
    public DateTime? UnlikedAtUtc { get; private set; }

    public long Version { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private ArticleLike()
    {
    }

    /// <summary>
    /// Creates a new active like relationship.
    /// The application uses this shape when initiating a new like command,
    /// while the authoritative insert/reactivation decision is performed
    /// through the database procedure.
    /// </summary>
    public static ArticleLike CreateActive(
        string publicId,
        string articlePublicId,
        long userId,
        DateTime likedAtUtc)
    {
        ValidatePublicId(publicId);
        ValidateArticlePublicId(articlePublicId);
        ValidateUserId(userId);
        ValidateTimestamp(likedAtUtc, "LikedAtUtc");

        return new ArticleLike
        {
            PublicId = NormalizeRequired(publicId),
            ArticlePublicId = NormalizeRequired(articlePublicId),
            UserId = userId,
            IsActive = true,
            LikedAtUtc = likedAtUtc,
            UnlikedAtUtc = null,
            Version = 1,
            CreatedAtUtc = likedAtUtc,
            UpdatedAtUtc = null
        };
    }

    /// <summary>
    /// Rehydrates the relationship state returned by Interaction persistence.
    /// Like/unlike transitions are executed by authoritative database procedures
    /// because they must be idempotent under concurrent requests.
    /// </summary>
    public static ArticleLike Rehydrate(
        long articleLikeId,
        string publicId,
        string articlePublicId,
        long userId,
        bool isActive,
        DateTime likedAtUtc,
        DateTime? unlikedAtUtc,
        long version,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        ValidateArticleLikeId(articleLikeId);
        ValidatePublicId(publicId);
        ValidateArticlePublicId(articlePublicId);
        ValidateUserId(userId);
        ValidateVersion(version);
        ValidateState(
            isActive,
            likedAtUtc,
            unlikedAtUtc,
            createdAtUtc,
            updatedAtUtc);

        return new ArticleLike
        {
            ArticleLikeId = articleLikeId,
            PublicId = NormalizeRequired(publicId),
            ArticlePublicId = NormalizeRequired(articlePublicId),
            UserId = userId,
            IsActive = isActive,
            LikedAtUtc = likedAtUtc,
            UnlikedAtUtc = unlikedAtUtc,
            Version = version,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = updatedAtUtc
        };
    }

    private static void ValidateArticleLikeId(long articleLikeId)
    {
        if (articleLikeId <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_LIKE_INVALID_ID",
                "Article like id must be greater than zero.");
        }
    }

    private static void ValidatePublicId(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_LIKE_PUBLIC_ID_REQUIRED",
                "Article like public id is required.");
        }
    }

    private static void ValidateArticlePublicId(string articlePublicId)
    {
        if (string.IsNullOrWhiteSpace(articlePublicId))
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_LIKE_ARTICLE_PUBLIC_ID_REQUIRED",
                "Article public id is required.");
        }
    }

    private static void ValidateUserId(long userId)
    {
        if (userId <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_LIKE_INVALID_USER_ID",
                "User id must be greater than zero.");
        }
    }

    private static void ValidateVersion(long version)
    {
        if (version < 1)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_LIKE_INVALID_VERSION",
                "Article like version must be greater than or equal to one.");
        }
    }

    private static void ValidateState(
        bool isActive,
        DateTime likedAtUtc,
        DateTime? unlikedAtUtc,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        ValidateTimestamp(createdAtUtc, "CreatedAtUtc");
        ValidateTimestamp(likedAtUtc, "LikedAtUtc");

        if (likedAtUtc < createdAtUtc)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_LIKE_INVALID_LIKED_AT_UTC_ORDER",
                "LikedAtUtc must be greater than or equal to CreatedAtUtc.");
        }

        if (updatedAtUtc.HasValue && updatedAtUtc.Value < createdAtUtc)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_LIKE_INVALID_UPDATED_AT_UTC_ORDER",
                "UpdatedAtUtc must be greater than or equal to CreatedAtUtc.");
        }

        if (isActive && unlikedAtUtc.HasValue)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_LIKE_INVALID_ACTIVE_STATE",
                "Active article like must not have UnlikedAtUtc.");
        }

        if (!isActive && !unlikedAtUtc.HasValue)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_LIKE_INVALID_INACTIVE_STATE",
                "Inactive article like must have UnlikedAtUtc.");
        }

        if (unlikedAtUtc.HasValue && unlikedAtUtc.Value < likedAtUtc)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_LIKE_INVALID_UNLIKED_AT_UTC_ORDER",
                "UnlikedAtUtc must be greater than or equal to LikedAtUtc.");
        }
    }

    private static void ValidateTimestamp(DateTime value, string propertyName)
    {
        if (value == default)
        {
            throw new InteractionDomainException(
                $"INTERACTION.ARTICLE_LIKE_INVALID_{propertyName.ToUpperInvariant()}",
                $"{propertyName} must be a valid datetime.");
        }
    }

    private static string NormalizeRequired(string value)
    {
        return value.Trim();
    }
}