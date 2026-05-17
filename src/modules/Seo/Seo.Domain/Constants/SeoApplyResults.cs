namespace Seo.Domain.Constants;

public static class SeoApplyResults
{
    public const string Applied = "Applied";
    public const string StaleIgnored = "StaleIgnored";
    public const string NoRouteToActivate = "NoRouteToActivate";
    public const string NoRouteToDeactivate = "NoRouteToDeactivate";
    public const string NotApplied = "NotApplied";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Applied,
        StaleIgnored,
        NoRouteToActivate,
        NoRouteToDeactivate,
        NotApplied
    };

    public static bool IsValid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && All.Contains(value.Trim());
    }
}
