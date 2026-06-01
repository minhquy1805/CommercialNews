using System.Collections.Frozen;

namespace Audit.Domain.Constants.AuditLog;

public static class AuditActorTypes
{
    public const string User = "User";
    public const string Admin = "Admin";
    public const string Moderator = "Moderator";
    public const string System = "System";
    public const string Worker = "Worker";
    public const string Anonymous = "Anonymous";
    public const string External = "External";

    public static readonly FrozenSet<string> All = new[]
    {
        User,
        Admin,
        Moderator,
        System,
        Worker,
        Anonymous,
        External
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return All.Contains(value);
    }
}