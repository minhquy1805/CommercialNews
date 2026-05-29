namespace Interaction.Application.Outbox;

public static class InteractionAggregateTypes
{
    public const string ArticleLike = "ArticleLike";
    public const string Comment = "Comment";
    public const string CommentReport = "CommentReport";
    public const string CommentModerationCase = "CommentModerationCase";
    public const string ArticleInteractionStats = "ArticleInteractionStats";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ArticleLike,
            Comment,
            CommentReport,
            CommentModerationCase,
            ArticleInteractionStats
        };

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return All.Contains(value);
    }
}