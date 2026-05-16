namespace Seo.Domain.Constants;

public static class SeoScopes
{
    public const string Public = "public";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Public
    };

    public static bool IsValid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && All.Contains(value.Trim());
    }
}