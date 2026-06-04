using Audit.Application.Models.Results.Metadata;
using Audit.Domain.Constants.Events;

namespace Audit.Application.UseCases.Metadata;

internal static class AuditMetadataCatalog
{
    public static readonly IReadOnlyList<AuditModuleResult> CurrentModules =
        new[]
        {
            new AuditModuleResult(
                SourceModule: AuditSourceModules.Authorization,
                Description: "Authorization roles, permissions, and user-role changes."),

            new AuditModuleResult(
                SourceModule: AuditSourceModules.Identity,
                Description: "Identity account lifecycle and security changes."),

            new AuditModuleResult(
                SourceModule: AuditSourceModules.Content,
                Description: "Content article lifecycle changes."),

            new AuditModuleResult(
                SourceModule: AuditSourceModules.Media,
                Description: "Media asset and article-media changes."),

            new AuditModuleResult(
                SourceModule: AuditSourceModules.Interaction,
                Description: "Interaction moderation changes.")
        };

    public static bool TryGetCurrentModuleEventTypes(
        string sourceModule,
        out string normalizedSourceModule,
        out IReadOnlyCollection<string> eventTypes)
    {
        normalizedSourceModule = sourceModule.Trim();
        eventTypes = Array.Empty<string>();

        if (string.Equals(normalizedSourceModule, AuditSourceModules.Authorization, StringComparison.OrdinalIgnoreCase))
        {
            normalizedSourceModule = AuditSourceModules.Authorization;
            eventTypes = AuditEventTypes.AuthorizationEvents;
            return true;
        }

        if (string.Equals(normalizedSourceModule, AuditSourceModules.Identity, StringComparison.OrdinalIgnoreCase))
        {
            normalizedSourceModule = AuditSourceModules.Identity;
            eventTypes = AuditEventTypes.IdentityEvents;
            return true;
        }

        if (string.Equals(normalizedSourceModule, AuditSourceModules.Content, StringComparison.OrdinalIgnoreCase))
        {
            normalizedSourceModule = AuditSourceModules.Content;
            eventTypes = AuditEventTypes.ContentEvents;
            return true;
        }

        if (string.Equals(normalizedSourceModule, AuditSourceModules.Media, StringComparison.OrdinalIgnoreCase))
        {
            normalizedSourceModule = AuditSourceModules.Media;
            eventTypes = AuditEventTypes.MediaEvents;
            return true;
        }

        if (string.Equals(normalizedSourceModule, AuditSourceModules.Interaction, StringComparison.OrdinalIgnoreCase))
        {
            normalizedSourceModule = AuditSourceModules.Interaction;
            eventTypes = AuditEventTypes.InteractionEvents;
            return true;
        }

        return false;
    }
}
