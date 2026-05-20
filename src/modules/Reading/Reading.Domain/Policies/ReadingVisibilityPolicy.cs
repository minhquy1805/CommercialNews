using Reading.Domain.Constants;

namespace Reading.Domain.Policies;

/// <summary>
/// Defines public visibility rules for Reading projections.
///
/// Reading does not own article lifecycle truth.
/// Reading evaluates public serving rules from source-derived projection values.
/// </summary>
public static class ReadingVisibilityPolicy
{
    public static bool CanServePublicly(
        string? sourceStatus,
        bool isPublic,
        DateTime? publishedAtUtc)
    {
        return isPublic
               && SourceArticleStatuses.IsPublished(sourceStatus)
               && publishedAtUtc is not null;
    }

    public static bool MustDenyPublic(
        string? sourceStatus,
        bool isPublic,
        DateTime? publishedAtUtc)
    {
        return !CanServePublicly(sourceStatus, isPublic, publishedAtUtc);
    }

    public static bool NormalizePublicFlag(
        string? sourceStatus,
        bool requestedPublic,
        DateTime? publishedAtUtc)
    {
        if (!SourceArticleStatuses.IsPublished(sourceStatus))
        {
            return false;
        }

        if (publishedAtUtc is null)
        {
            return false;
        }

        return requestedPublic;
    }
}