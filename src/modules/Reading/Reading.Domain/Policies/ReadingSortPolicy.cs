using Reading.Domain.Enums;

namespace Reading.Domain.Policies;

/// <summary>
/// Defines how Reading handles sort input for public queries.
///
/// This policy does not own the sort vocabulary itself.
/// The allowed sort tokens live in Reading.Domain.Enums.ReadingSortValues.
///
/// Responsibilities of this policy:
/// - define the default sort
/// - validate whether an incoming sort is allowed
/// - normalize sort input before it enters the query pipeline
/// - expose helper methods for sort-specific behavior
///
/// Why this exists:
/// - Reading is a query facade, so sort semantics must be explicit and centralized.
/// - We want Application/Infrastructure to rely on one normalized rule set.
/// </summary>
public static class ReadingSortPolicy
{
    /// <summary>
    /// Default public sort for Reading V1.
    /// Published content should be served newest-first by default.
    /// </summary>
    public const string DefaultSort = ReadingSortValues.PublishedAtDescending;

    /// <summary>
    /// Returns true when the given sort value is allowed by Reading V1.
    ///
    /// Null/empty is treated as valid because it will fall back to DefaultSort.
    /// </summary>
    public static bool IsAllowed(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return true;
        }

        return ReadingSortValues.IsValid(sort.Trim());
    }

    /// <summary>
    /// Normalizes incoming sort input.
    ///
    /// If the client does not provide a sort, Reading uses the default sort.
    /// </summary>
    public static string Normalize(string? sort)
    {
        return string.IsNullOrWhiteSpace(sort)
            ? DefaultSort
            : sort.Trim();
    }

    /// <summary>
    /// Returns true when the chosen sort depends on popularity/counter-style data.
    ///
    /// This is useful because popularity is a derived/eventual enrichment,
    /// not publication truth.
    /// </summary>
    public static bool IsPopularitySort(string? sort)
    {
        string normalized = Normalize(sort);

        return string.Equals(normalized, ReadingSortValues.PopularityAscending, StringComparison.OrdinalIgnoreCase)
               || string.Equals(normalized, ReadingSortValues.PopularityDescending, StringComparison.OrdinalIgnoreCase);
    }
}