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
    public const string Unknown = "Unknown";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Transient,
        Permanent,
        Ambiguous,
        Policy,
        Template,
        Provider,
        Validation,
        Unknown
    };

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return All.Contains(value.Trim());
    }

    public static bool IsGenerallyRetryable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();

        return string.Equals(normalized, Transient, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, Ambiguous, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, Unknown, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsGenerallyTerminal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();

        return string.Equals(normalized, Permanent, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, Policy, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, Template, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, Validation, StringComparison.OrdinalIgnoreCase);
    }
}