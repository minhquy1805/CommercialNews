namespace Interaction.Domain.Entities;

using Interaction.Domain.Exceptions;

public sealed class ArticleLike
{
    public long ArticleLikeId { get; private set; }

    public long ArticleId { get; private set; }
    public long UserId { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime LikedAt { get; private set; }
    public DateTime? UnlikedAt { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private ArticleLike()
    {
    }

    public static ArticleLike Create(
        long articleId,
        long userId,
        DateTime nowUtc)
    {
        ValidateArticleId(articleId);
        ValidateUserId(userId);

        return new ArticleLike
        {
            ArticleId = articleId,
            UserId = userId,
            IsActive = true,
            LikedAt = nowUtc,
            UnlikedAt = null,
            CreatedAt = nowUtc,
            UpdatedAt = null
        };
    }

    public static ArticleLike Rehydrate(
        long articleLikeId,
        long articleId,
        long userId,
        bool isActive,
        DateTime likedAt,
        DateTime? unlikedAt,
        DateTime createdAt,
        DateTime? updatedAt)
    {
        if (articleLikeId <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_LIKE_INVALID_ID",
                "Article like id must be greater than zero.");
        }

        ValidateArticleId(articleId);
        ValidateUserId(userId);
        ValidateState(isActive, likedAt, unlikedAt, createdAt, updatedAt);

        return new ArticleLike
        {
            ArticleLikeId = articleLikeId,
            ArticleId = articleId,
            UserId = userId,
            IsActive = isActive,
            LikedAt = likedAt,
            UnlikedAt = unlikedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void Activate(DateTime nowUtc)
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        LikedAt = nowUtc;
        UnlikedAt = null;
        UpdatedAt = nowUtc;
    }

    public void Deactivate(DateTime nowUtc)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        UnlikedAt = nowUtc;
        UpdatedAt = nowUtc;
    }

    private static void ValidateArticleId(long articleId)
    {
        if (articleId <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_LIKE_INVALID_ARTICLE_ID",
                "Article id must be greater than zero.");
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

    private static void ValidateState(
        bool isActive,
        DateTime likedAt,
        DateTime? unlikedAt,
        DateTime createdAt,
        DateTime? updatedAt)
    {
        if (createdAt == default)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_LIKE_INVALID_CREATED_AT",
                "CreatedAt must be a valid UTC datetime.");
        }

        if (likedAt == default)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_LIKE_INVALID_LIKED_AT",
                "LikedAt must be a valid UTC datetime.");
        }

        if (likedAt < createdAt)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_LIKE_INVALID_LIKED_AT_ORDER",
                "LikedAt must be greater than or equal to CreatedAt.");
        }

        if (updatedAt.HasValue && updatedAt.Value < createdAt)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_LIKE_INVALID_UPDATED_AT_ORDER",
                "UpdatedAt must be greater than or equal to CreatedAt.");
        }

        if (isActive && unlikedAt.HasValue)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_LIKE_INVALID_ACTIVE_STATE",
                "Active article like must not have UnlikedAt.");
        }

        if (!isActive && !unlikedAt.HasValue)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_LIKE_INVALID_INACTIVE_STATE",
                "Inactive article like must have UnlikedAt.");
        }

        if (unlikedAt.HasValue && unlikedAt.Value < likedAt)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_LIKE_INVALID_UNLIKED_AT_ORDER",
                "UnlikedAt must be greater than or equal to LikedAt.");
        }
    }
}