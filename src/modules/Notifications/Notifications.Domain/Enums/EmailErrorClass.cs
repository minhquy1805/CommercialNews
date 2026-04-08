namespace Notifications.Domain.Enums;

public static class EmailErrorClass
{
    public const string Transient = "Transient";
    public const string Permanent = "Permanent";
    public const string Ambiguous = "Ambiguous";
    public const string Policy = "Policy";
    public const string Template = "Template";
    public const string Provider = "Provider";
    public const string Validation = "Validation";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Transient,
        Permanent,
        Ambiguous,
        Policy,
        Template,
        Provider,
        Validation
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