using System.Collections.Frozen;

namespace Audit.Domain.Constants.Events;

public static class AuditActionCategories
{
    public const string AuthorizationGovernance = "AuthorizationGovernance";
    public const string IdentitySecurity = "IdentitySecurity";
    public const string ContentLifecycle = "ContentLifecycle";
    public const string MediaGovernance = "MediaGovernance";
    public const string InteractionModeration = "InteractionModeration";

    // Future extension.
    public const string SeoGovernance = "SeoGovernance";
    public const string NotificationGovernance = "NotificationGovernance";
    public const string AuditGovernance = "AuditGovernance";
    public const string System = "System";

    public static readonly FrozenSet<string> All = new[]
    {
        AuthorizationGovernance,
        IdentitySecurity,
        ContentLifecycle,
        MediaGovernance,
        InteractionModeration,
        SeoGovernance,
        NotificationGovernance,
        AuditGovernance,
        System
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static readonly FrozenSet<string> CurrentV1Baseline = new[]
    {
        AuthorizationGovernance,
        IdentitySecurity,
        ContentLifecycle,
        MediaGovernance,
        InteractionModeration
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static readonly FrozenSet<string> FutureExtensionCategories = new[]
    {
        SeoGovernance,
        NotificationGovernance,
        AuditGovernance,
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