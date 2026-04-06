namespace Reading.Domain.Policies;

/// <summary>
/// Defines safe degradation behavior for optional Reading enrichments.
///
/// Responsibilities of this policy:
/// - state what Reading may omit or default safely
/// - keep "safe omission beats stale invention" explicit
/// - document which enrichments are optional for correctness
///
/// Reading V1 correctness rule:
/// - public visibility must be truth-backed
/// - optional enrichments may lag, be missing, or be omitted
///
/// Optional enrichments include:
/// - counters
/// - SEO metadata fragments
/// - media enrichments
/// </summary>
public static class ReadingFallbackPolicy
{
    /// <summary>
    /// Missing counters may safely fall back to zero in V1,
    /// as long as the caller can mark the response as partial if needed.
    /// </summary>
    public static bool UseZeroForMissingCounters => true;

    /// <summary>
    /// Missing SEO metadata must not fail the entire public response.
    /// Reading may omit these fields or use safe defaults.
    /// </summary>
    public static bool AllowMissingSeoMetadata => true;

    /// <summary>
    /// Missing media must not fail the entire public response.
    /// Reading may return null media or a placeholder at higher layers.
    /// </summary>
    public static bool AllowMissingMedia => true;

    /// <summary>
    /// Returns true when Reading may still serve the core truth-backed response
    /// even if optional enrichments are unavailable.
    /// </summary>
    public static bool CanServeWithoutOptionalEnrichments()
    {
        return true;
    }
}