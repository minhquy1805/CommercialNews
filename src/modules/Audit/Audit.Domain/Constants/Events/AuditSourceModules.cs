using System.Collections.Frozen;

namespace Audit.Domain.Constants.Events;

public static class AuditSourceModules
{
    // Current V1 consumed sources.
    public const string Authorization = "Authorization";
    public const string Identity = "Identity";
    public const string Content = "Content";
    public const string Media = "Media";
    public const string Interaction = "Interaction";

    // Future audit coverage sources.
    public const string Seo = "SEO";
    public const string Notifications = "Notifications";

    // Future internal/system audit sources.
    public const string Audit = "Audit";
    public const string System = "System";

    public static readonly FrozenSet<string> All = new[]
    {
        Authorization,
        Identity,
        Content,
        Media,
        Interaction,
        Seo,
        Notifications,
        Audit,
        System
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static readonly FrozenSet<string> CurrentV1Baseline = new[]
    {
        Authorization,
        Identity,
        Content,
        Media,
        Interaction
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static readonly FrozenSet<string> FutureExtensionSources = new[]
    {
        Seo,
        Notifications,
        Audit,
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

        return FutureExtensionSources.Contains(value);
    }
}