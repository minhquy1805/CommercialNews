using System.Collections.Frozen;

namespace Audit.Domain.Constants.Events;

public static class AuditActionCategories
{
    public const string Authentication = "Authentication";
    public const string Authorization = "Authorization";
    public const string IdentitySecurity = "IdentitySecurity";
    public const string ContentLifecycle = "ContentLifecycle";
    public const string Moderation = "Moderation";
    public const string MediaGovernance = "MediaGovernance";

    // Future extension.
    public const string SeoGovernance = "SeoGovernance";
    public const string NotificationDelivery = "NotificationDelivery";
    public const string AuditIngestion = "AuditIngestion";
    public const string System = "System";

    public static readonly FrozenSet<string> All = new[]
    {
        Authentication,
        Authorization,
        IdentitySecurity,
        ContentLifecycle,
        Moderation,
        MediaGovernance,
        SeoGovernance,
        NotificationDelivery,
        AuditIngestion,
        System
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static readonly FrozenSet<string> CurrentV1Baseline = new[]
    {
        Authentication,
        Authorization,
        IdentitySecurity,
        ContentLifecycle,
        MediaGovernance,
        Moderation
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static readonly FrozenSet<string> FutureExtensionCategories = new[]
    {
        SeoGovernance,
        NotificationDelivery,
        AuditIngestion,
        System
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return All.Contains(value);
    }

    public static bool IsCurrentV1Baseline(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return CurrentV1Baseline.Contains(value);
    }

    public static bool IsFutureExtension(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return FutureExtensionCategories.Contains(value);
    }
}
