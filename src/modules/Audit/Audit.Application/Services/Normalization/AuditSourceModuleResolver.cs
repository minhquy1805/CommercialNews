using Audit.Domain.Constants.Events;

namespace Audit.Application.Services.Normalization;

public static class AuditSourceModuleResolver
{
    public static string? Resolve(string? eventType)
    {
        if (AuditEventTypes.IsAuthorizationEvent(eventType))
        {
            return AuditSourceModules.Authorization;
        }

        if (AuditEventTypes.IsIdentityEvent(eventType))
        {
            return AuditSourceModules.Identity;
        }

        if (AuditEventTypes.IsContentEvent(eventType))
        {
            return AuditSourceModules.Content;
        }

        if (AuditEventTypes.IsMediaEvent(eventType))
        {
            return AuditSourceModules.Media;
        }

        if (AuditEventTypes.IsInteractionEvent(eventType))
        {
            return AuditSourceModules.Interaction;
        }

        return null;
    }
}