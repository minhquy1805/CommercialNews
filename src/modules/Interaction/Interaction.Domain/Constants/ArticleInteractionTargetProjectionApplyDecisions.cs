namespace Interaction.Domain.Constants;

public static class ArticleInteractionTargetProjectionApplyDecisions
{
    public const string Applied = "Applied";
    public const string StaleIgnored = "StaleIgnored";

    // Reserved for future gap-detection and resync workflow.
    public const string ResyncRequired = "ResyncRequired";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Applied,
        StaleIgnored,
        ResyncRequired
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