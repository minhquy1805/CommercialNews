namespace Reading.Domain.Policies;

/// <summary>
/// Defines safe degradation behavior for optional Reading enrichments.
///
/// Optional enrichments improve response completeness, but they must not define
/// whether an article is publicly readable.
/// </summary>
public static class ReadingDegradationPolicy
{
    public static bool UseZeroForMissingCounters => true;

    public static bool AllowMissingSeoMetadata => true;

    public static bool AllowMissingMedia => true;

    public static bool AllowMissingRelatedArticles => true;

    public static bool CanServeWithoutOptionalEnrichments()
    {
        return true;
    }
}