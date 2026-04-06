namespace Reading.Domain.Policies;

/// <summary>
/// Defines the deterministic rule set for related articles in Reading V1.
///
/// Responsibilities of this policy:
/// - document the priority order for related content
/// - make self-exclusion explicit
/// - keep the V1 deterministic strategy centralized
///
/// V1 strategy:
/// 1. same category
/// 2. shared tags
/// 3. newest published fallback
///
/// Why this exists:
/// - Reading V1 should be explainable and stable.
/// - We do not want hidden ranking heuristics at this stage.
/// </summary>
public static class RelatedArticlesPolicy
{
    /// <summary>
    /// Human-readable summary of the V1 related-article strategy.
    /// Useful for test names, logs, and documentation.
    /// </summary>
    public const string StrategyDescription =
        "Same category -> shared tags -> newest published fallback";

    /// <summary>
    /// The current article must never appear in its own related list.
    /// </summary>
    public static bool IsSelfArticle(long currentArticleId, long candidateArticleId)
    {
        return currentArticleId == candidateArticleId;
    }

    /// <summary>
    /// Returns true when the candidate matches the primary related rule:
    /// same category as the current article.
    /// </summary>
    public static bool MatchesPrimaryRule(long? currentCategoryId, long? candidateCategoryId)
    {
        return currentCategoryId.HasValue
               && candidateCategoryId.HasValue
               && currentCategoryId.Value == candidateCategoryId.Value;
    }
}