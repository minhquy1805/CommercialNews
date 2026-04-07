namespace Interaction.Domain.Entities;

using Interaction.Domain.Exceptions;

public sealed class ArticleInteractionStats
{
    public long ArticleId { get; private set; }

    public long ViewsTotal { get; private set; }
    public long LikesTotal { get; private set; }
    public long CommentsTotal { get; private set; }

    public decimal PopularityScore { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? LastAggregatedAt { get; private set; }

    private ArticleInteractionStats()
    {
    }

    public static ArticleInteractionStats Create(
        long articleId,
        long viewsTotal,
        long likesTotal,
        long commentsTotal,
        decimal popularityScore,
        DateTime nowUtc)
    {
        ValidateArticleId(articleId);
        ValidateViewsTotal(viewsTotal);
        ValidateLikesTotal(likesTotal);
        ValidateCommentsTotal(commentsTotal);
        ValidatePopularityScore(popularityScore);

        return new ArticleInteractionStats
        {
            ArticleId = articleId,
            ViewsTotal = viewsTotal,
            LikesTotal = likesTotal,
            CommentsTotal = commentsTotal,
            PopularityScore = popularityScore,
            CreatedAt = nowUtc,
            UpdatedAt = null,
            LastAggregatedAt = null
        };
    }

    public static ArticleInteractionStats Rehydrate(
        long articleId,
        long viewsTotal,
        long likesTotal,
        long commentsTotal,
        decimal popularityScore,
        DateTime createdAt,
        DateTime? updatedAt,
        DateTime? lastAggregatedAt)
    {
        ValidateArticleId(articleId);
        ValidateViewsTotal(viewsTotal);
        ValidateLikesTotal(likesTotal);
        ValidateCommentsTotal(commentsTotal);
        ValidatePopularityScore(popularityScore);

        if (createdAt == default)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_STATS_INVALID_CREATED_AT",
                "CreatedAt must be a valid UTC datetime.");
        }

        if (updatedAt.HasValue && updatedAt.Value < createdAt)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_STATS_INVALID_UPDATED_AT_ORDER",
                "UpdatedAt must be greater than or equal to CreatedAt.");
        }

        if (lastAggregatedAt.HasValue && lastAggregatedAt.Value < createdAt)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_STATS_INVALID_LAST_AGGREGATED_AT_ORDER",
                "LastAggregatedAt must be greater than or equal to CreatedAt.");
        }

        return new ArticleInteractionStats
        {
            ArticleId = articleId,
            ViewsTotal = viewsTotal,
            LikesTotal = likesTotal,
            CommentsTotal = commentsTotal,
            PopularityScore = popularityScore,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            LastAggregatedAt = lastAggregatedAt
        };
    }

    public void UpdateTotals(
        long viewsTotal,
        long likesTotal,
        long commentsTotal,
        decimal popularityScore,
        DateTime nowUtc)
    {
        ValidateViewsTotal(viewsTotal);
        ValidateLikesTotal(likesTotal);
        ValidateCommentsTotal(commentsTotal);
        ValidatePopularityScore(popularityScore);

        ViewsTotal = viewsTotal;
        LikesTotal = likesTotal;
        CommentsTotal = commentsTotal;
        PopularityScore = popularityScore;
        UpdatedAt = nowUtc;
        LastAggregatedAt = nowUtc;
    }

    private static void ValidateArticleId(long articleId)
    {
        if (articleId <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_STATS_INVALID_ARTICLE_ID",
                "Article id must be greater than zero.");
        }
    }

    private static void ValidateViewsTotal(long viewsTotal)
    {
        if (viewsTotal < 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_STATS_INVALID_VIEWS_TOTAL",
                "ViewsTotal must be greater than or equal to zero.");
        }
    }

    private static void ValidateLikesTotal(long likesTotal)
    {
        if (likesTotal < 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_STATS_INVALID_LIKES_TOTAL",
                "LikesTotal must be greater than or equal to zero.");
        }
    }

    private static void ValidateCommentsTotal(long commentsTotal)
    {
        if (commentsTotal < 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_STATS_INVALID_COMMENTS_TOTAL",
                "CommentsTotal must be greater than or equal to zero.");
        }
    }

    private static void ValidatePopularityScore(decimal popularityScore)
    {
        if (popularityScore < 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_STATS_INVALID_POPULARITY_SCORE",
                "PopularityScore must be greater than or equal to zero.");
        }
    }
}