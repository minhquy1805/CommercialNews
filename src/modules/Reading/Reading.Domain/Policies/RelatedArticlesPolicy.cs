namespace Reading.Domain.Policies;

/// <summary>
/// Defines the deterministic rule set for related articles in Reading V1.
///
/// V1 strategy:
/// 1. same category
/// 2. shared tags
/// 3. newest public fallback
/// </summary>
public static class RelatedArticlesPolicy
{
    public const string StrategyDescription =
        "Same category -> shared tags -> newest public fallback";

    public static bool IsSelfArticle(long currentArticleId, long candidateArticleId)
    {
        return currentArticleId == candidateArticleId;
    }

    public static bool MatchesPrimaryRule(long? currentCategoryId, long? candidateCategoryId)
    {
        return currentCategoryId.HasValue
               && candidateCategoryId.HasValue
               && currentCategoryId.Value == candidateCategoryId.Value;
    }

    public static bool HasSharedTag(
        IReadOnlySet<long> currentTagIds,
        IReadOnlySet<long> candidateTagIds)
    {
        if (currentTagIds.Count == 0 || candidateTagIds.Count == 0)
        {
            return false;
        }

        return currentTagIds.Overlaps(candidateTagIds);
    }
}