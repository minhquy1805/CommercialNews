namespace Interaction.Domain.Constants;

public static class ReportAlertLevels
{
    public const string High = "High";
    public const string Critical = "Critical";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        High,
        Critical
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