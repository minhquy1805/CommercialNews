namespace Audit.Domain.Enums;

public static class AuditOutcome
{
    public const string Success = "Success";
    public const string Failure = "Failure";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Success,
        Failure
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