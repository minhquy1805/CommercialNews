namespace Interaction.Application.Outbox;

public static class InteractionIntegrationEventTypes
{
    public const string ArticleLiked =
        "interaction.article_liked";

    public const string ArticleUnliked =
        "interaction.article_unliked";

    public const string CommentCreated =
        "interaction.comment_created";

    public const string CommentHidden =
        "interaction.comment_hidden";

    public const string CommentRestored =
        "interaction.comment_restored";

    public const string CommentDeletedByAuthor =
        "interaction.comment_deleted_by_author";

    public const string CommentReported =
        "interaction.comment_reported";

    public const string CommentReportsDismissed =
        "interaction.comment_reports_dismissed";

    public const string CommentReportAlertTriggered =
        "interaction.comment_report_alert_triggered";

    public const string ArticleCountersProjectionPublished =
        "interaction.article_counters_projection_published";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ArticleLiked,
            ArticleUnliked,
            CommentCreated,
            CommentHidden,
            CommentRestored,
            CommentDeletedByAuthor,
            CommentReported,
            CommentReportsDismissed,
            CommentReportAlertTriggered,
            ArticleCountersProjectionPublished
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