namespace Reading.Domain.Policies;

/// <summary>
/// Defines public visibility rules for Reading.
///
/// Responsibilities of this policy:
/// - determine whether content may be served publicly
/// - keep the "truth-safe visibility first" rule explicit
/// - make deny/allow intent easy to read at call sites
///
/// Important:
/// - Reading does not own article lifecycle truth.
/// - Content owns publication state.
/// - Reading only evaluates public serving rules based on truth-backed values.
///
/// V1 public rule:
/// - content must be Published
/// - content must not be deleted
/// </summary>
public static class ReadingVisibilityPolicy
{
    /// <summary>
    /// The Content status value that allows public serving.
    ///
    /// This is kept local to Reading to avoid introducing cross-module domain coupling
    /// at this stage of the architecture.
    /// </summary>
    public const string PublishedStatus = "Published";

    /// <summary>
    /// Returns true only when the content is safe to serve publicly.
    /// </summary>
    public static bool CanServePublicly(string? status, bool isDeleted)
    {
        return !isDeleted
               && string.Equals(status, PublishedStatus, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns true when Reading must deny public serving.
    /// Useful for explicit guard clauses.
    /// </summary>
    public static bool MustDenyPublic(string? status, bool isDeleted)
    {
        return !CanServePublicly(status, isDeleted);
    }
}