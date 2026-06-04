using Audit.Application.Abstractions.Serialization;
using Audit.Application.Services.Normalization;
using Audit.Domain.Constants.AuditLog;
using Audit.Domain.Constants.Events;
using Audit.Domain.Policies.Evidence;
using Audit.Domain.ValueObjects.Evidence;
using Audit.Infrastructure.Normalization.Authorization.EventPayloads;
using Audit.Infrastructure.Normalization.Common;

namespace Audit.Infrastructure.Normalization.Authorization;

internal sealed class AuthorizationAuditEventNormalizer : AuditNormalizerBase
{
    public override string SourceModule => AuditSourceModules.Authorization;

    public AuthorizationAuditEventNormalizer(
        IAuditJsonSerializer jsonSerializer,
        IAuditActionClassificationPolicy actionClassificationPolicy,
        IAuditRiskClassificationPolicy riskClassificationPolicy)
        : base(
            jsonSerializer,
            actionClassificationPolicy,
            riskClassificationPolicy)
    {
    }

    public override bool CanHandle(
        string eventType)
    {
        return AuditEventTypes.IsAuthorizationEvent(eventType);
    }

    protected override AuditNormalizedEvent NormalizeCore(
        AuditEventNormalizationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var eventType = NormalizeRequired(
            context.SourceEvent.EventType,
            nameof(context.SourceEvent.EventType));

        if (IsEventType(eventType, AuditEventTypes.AuthorizationUserRoleAssigned))
        {
            return NormalizeUserRoleAssigned(context);
        }

        if (IsEventType(eventType, AuditEventTypes.AuthorizationUserRoleRevoked))
        {
            return NormalizeUserRoleRevoked(context);
        }

        if (IsEventType(eventType, AuditEventTypes.AuthorizationRolePermissionGranted))
        {
            return NormalizeRolePermissionGranted(context);
        }

        if (IsEventType(eventType, AuditEventTypes.AuthorizationRolePermissionRevoked))
        {
            return NormalizeRolePermissionRevoked(context);
        }

        if (IsEventType(eventType, AuditEventTypes.AuthorizationRoleCreated))
        {
            return NormalizeRoleCreated(context);
        }

        if (IsEventType(eventType, AuditEventTypes.AuthorizationRoleUpdated))
        {
            return NormalizeRoleUpdated(context);
        }

        if (IsEventType(eventType, AuditEventTypes.AuthorizationRoleActivated))
        {
            return NormalizeRoleActivated(context);
        }

        if (IsEventType(eventType, AuditEventTypes.AuthorizationRoleDeactivated))
        {
            return NormalizeRoleDeactivated(context);
        }

        if (IsEventType(eventType, AuditEventTypes.AuthorizationPermissionCreated))
        {
            return NormalizePermissionCreated(context);
        }

        if (IsEventType(eventType, AuditEventTypes.AuthorizationPermissionUpdated))
        {
            return NormalizePermissionUpdated(context);
        }

        if (IsEventType(eventType, AuditEventTypes.AuthorizationPermissionActivated))
        {
            return NormalizePermissionActivated(context);
        }

        if (IsEventType(eventType, AuditEventTypes.AuthorizationPermissionDeactivated))
        {
            return NormalizePermissionDeactivated(context);
        }

        throw new InvalidOperationException(
            $"Unsupported authorization audit event type '{context.SourceEvent.EventType}'.");
    }

    private AuditNormalizedEvent NormalizeUserRoleAssigned(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<UserRoleAssignedAuditPayload>(context);

        return CreateNormalizedEvent(
            context,
            BuildAdminOrSystemActor(actorInternalId: payload.AssignedByUserId),
            BuildUserRoleResource(
                userInternalId: payload.UserId,
                roleInternalId: payload.RoleId,
                roleName: payload.RoleName,
                roleDisplayName: payload.RoleDisplayName),
            new
            {
                userInternalId = payload.UserId,
                roleInternalId = payload.RoleId,
                rolePublicId = NormalizeRequired(payload.RolePublicId, nameof(payload.RolePublicId)),
                roleName = NormalizeRequired(payload.RoleName, nameof(payload.RoleName)),
                roleDisplayName = NormalizeOptional(payload.RoleDisplayName),
                roleIsSystem = payload.RoleIsSystem,
                assignedByInternalId = payload.AssignedByUserId,
                assignedAtUtc = payload.AssignedAtUtc
            },
            "User role was assigned.");
    }

    private AuditNormalizedEvent NormalizeUserRoleRevoked(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<UserRoleRevokedAuditPayload>(context);

        return CreateNormalizedEvent(
            context,
            BuildAdminOrSystemActor(actorInternalId: payload.RevokedByUserId),
            BuildUserRoleResource(
                userInternalId: payload.UserId,
                roleInternalId: payload.RoleId,
                roleName: payload.RoleName,
                roleDisplayName: payload.RoleDisplayName),
            new
            {
                userInternalId = payload.UserId,
                roleInternalId = payload.RoleId,
                rolePublicId = NormalizeRequired(payload.RolePublicId, nameof(payload.RolePublicId)),
                roleName = NormalizeRequired(payload.RoleName, nameof(payload.RoleName)),
                roleDisplayName = NormalizeOptional(payload.RoleDisplayName),
                roleIsSystem = payload.RoleIsSystem,
                revokedByInternalId = payload.RevokedByUserId,
                revokedAtUtc = payload.RevokedAtUtc
            },
            "User role was revoked.");
    }

    private AuditNormalizedEvent NormalizeRolePermissionGranted(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<RolePermissionGrantedAuditPayload>(context);

        return CreateNormalizedEvent(
            context,
            BuildAdminOrSystemActor(actorInternalId: payload.GrantedByUserId),
            BuildRolePermissionResource(
                payload.RolePublicId,
                payload.PermissionPublicId,
                payload.RoleName,
                payload.RoleDisplayName,
                payload.PermissionKey),
            BuildRolePermissionPayload(
                payload.RolePublicId,
                payload.RoleName,
                payload.RoleDisplayName,
                payload.RoleIsSystem,
                payload.PermissionPublicId,
                payload.PermissionKey,
                payload.PermissionModule,
                payload.PermissionAction,
                payload.PermissionIsSystem,
                actorInternalId: payload.GrantedByUserId,
                occurredAtUtc: payload.GrantedAtUtc),
            "Role permission was granted.");
    }

    private AuditNormalizedEvent NormalizeRolePermissionRevoked(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<RolePermissionRevokedAuditPayload>(context);

        return CreateNormalizedEvent(
            context,
            BuildAdminOrSystemActor(actorInternalId: payload.RevokedByUserId),
            BuildRolePermissionResource(
                payload.RolePublicId,
                payload.PermissionPublicId,
                payload.RoleName,
                payload.RoleDisplayName,
                payload.PermissionKey),
            BuildRolePermissionPayload(
                payload.RolePublicId,
                payload.RoleName,
                payload.RoleDisplayName,
                payload.RoleIsSystem,
                payload.PermissionPublicId,
                payload.PermissionKey,
                payload.PermissionModule,
                payload.PermissionAction,
                payload.PermissionIsSystem,
                actorInternalId: payload.RevokedByUserId,
                occurredAtUtc: payload.RevokedAtUtc),
            "Role permission was revoked.");
    }

    private AuditNormalizedEvent NormalizeRoleCreated(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<RoleCreatedAuditPayload>(context);

        return CreateRoleNormalizedEvent(
            context,
            BuildAdminOrSystemActor(actorInternalId: payload.CreatedByUserId),
            payload.RolePublicId,
            payload.RoleName,
            payload.RoleDisplayName,
            payload.RoleIsSystem,
            payload.RoleIsActive,
            actorInternalId: payload.CreatedByUserId,
            occurredAtUtc: payload.CreatedAtUtc,
            summary: "Role was created.");
    }

    private AuditNormalizedEvent NormalizeRoleUpdated(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<RoleUpdatedAuditPayload>(context);

        return CreateRoleNormalizedEvent(
            context,
            BuildAdminOrSystemActor(actorInternalId: payload.UpdatedByUserId),
            payload.RolePublicId,
            payload.RoleName,
            payload.RoleDisplayName,
            payload.RoleIsSystem,
            payload.RoleIsActive,
            actorInternalId: payload.UpdatedByUserId,
            occurredAtUtc: payload.UpdatedAtUtc,
            summary: "Role was updated.");
    }

    private AuditNormalizedEvent NormalizeRoleActivated(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<RoleActivatedAuditPayload>(context);

        return CreateRoleNormalizedEvent(
            context,
            BuildAdminOrSystemActor(actorInternalId: payload.ActivatedByUserId),
            payload.RolePublicId,
            payload.RoleName,
            payload.RoleDisplayName,
            payload.RoleIsSystem,
            roleIsActive: true,
            actorInternalId: payload.ActivatedByUserId,
            occurredAtUtc: payload.ActivatedAtUtc,
            summary: "Role was activated.");
    }

    private AuditNormalizedEvent NormalizeRoleDeactivated(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<RoleDeactivatedAuditPayload>(context);

        return CreateRoleNormalizedEvent(
            context,
            BuildAdminOrSystemActor(actorInternalId: payload.DeactivatedByUserId),
            payload.RolePublicId,
            payload.RoleName,
            payload.RoleDisplayName,
            payload.RoleIsSystem,
            roleIsActive: false,
            actorInternalId: payload.DeactivatedByUserId,
            occurredAtUtc: payload.DeactivatedAtUtc,
            summary: "Role was deactivated.");
    }

    private AuditNormalizedEvent NormalizePermissionCreated(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<PermissionCreatedAuditPayload>(context);

        return CreatePermissionNormalizedEvent(
            context,
            BuildAdminOrSystemActor(actorInternalId: payload.CreatedByUserId),
            payload.PermissionPublicId,
            payload.PermissionKey,
            payload.PermissionModule,
            payload.PermissionAction,
            payload.PermissionIsSystem,
            payload.PermissionIsActive,
            actorInternalId: payload.CreatedByUserId,
            occurredAtUtc: payload.CreatedAtUtc,
            summary: "Permission was created.");
    }

    private AuditNormalizedEvent NormalizePermissionUpdated(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<PermissionUpdatedAuditPayload>(context);

        return CreatePermissionNormalizedEvent(
            context,
            BuildAdminOrSystemActor(actorInternalId: payload.UpdatedByUserId),
            payload.PermissionPublicId,
            payload.PermissionKey,
            payload.PermissionModule,
            payload.PermissionAction,
            payload.PermissionIsSystem,
            payload.PermissionIsActive,
            actorInternalId: payload.UpdatedByUserId,
            occurredAtUtc: payload.UpdatedAtUtc,
            summary: "Permission was updated.");
    }

    private AuditNormalizedEvent NormalizePermissionActivated(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<PermissionActivatedAuditPayload>(context);

        return CreatePermissionNormalizedEvent(
            context,
            BuildAdminOrSystemActor(actorInternalId: payload.ActivatedByUserId),
            payload.PermissionPublicId,
            payload.PermissionKey,
            payload.PermissionModule,
            payload.PermissionAction,
            payload.PermissionIsSystem,
            permissionIsActive: true,
            actorInternalId: payload.ActivatedByUserId,
            occurredAtUtc: payload.ActivatedAtUtc,
            summary: "Permission was activated.");
    }

    private AuditNormalizedEvent NormalizePermissionDeactivated(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<PermissionDeactivatedAuditPayload>(context);

        return CreatePermissionNormalizedEvent(
            context,
            BuildAdminOrSystemActor(actorInternalId: payload.DeactivatedByUserId),
            payload.PermissionPublicId,
            payload.PermissionKey,
            payload.PermissionModule,
            payload.PermissionAction,
            payload.PermissionIsSystem,
            permissionIsActive: false,
            actorInternalId: payload.DeactivatedByUserId,
            occurredAtUtc: payload.DeactivatedAtUtc,
            summary: "Permission was deactivated.");
    }

    private AuditNormalizedEvent CreateRoleNormalizedEvent(
        AuditEventNormalizationContext context,
        AuditActor actor,
        string rolePublicId,
        string roleName,
        string? roleDisplayName,
        bool roleIsSystem,
        bool roleIsActive,
        long? actorInternalId,
        DateTime occurredAtUtc,
        string summary)
    {
        return CreateNormalizedEvent(
            context,
            actor,
            AuditResource.Create(
                AuditResourceTypes.Role,
                NormalizeRequired(rolePublicId, nameof(rolePublicId)),
                ResolveDisplayName(roleDisplayName, roleName)),
            new
            {
                rolePublicId = NormalizeRequired(rolePublicId, nameof(rolePublicId)),
                roleName = NormalizeRequired(roleName, nameof(roleName)),
                roleDisplayName = NormalizeOptional(roleDisplayName),
                roleIsSystem,
                roleIsActive,
                actorInternalId,
                occurredAtUtc
            },
            summary);
    }

    private AuditNormalizedEvent CreatePermissionNormalizedEvent(
        AuditEventNormalizationContext context,
        AuditActor actor,
        string permissionPublicId,
        string permissionKey,
        string? permissionModule,
        string? permissionAction,
        bool permissionIsSystem,
        bool permissionIsActive,
        long? actorInternalId,
        DateTime occurredAtUtc,
        string summary)
    {
        return CreateNormalizedEvent(
            context,
            actor,
            AuditResource.Create(
                AuditResourceTypes.Permission,
                NormalizeRequired(permissionPublicId, nameof(permissionPublicId)),
                NormalizeRequired(permissionKey, nameof(permissionKey))),
            new
            {
                permissionPublicId = NormalizeRequired(permissionPublicId, nameof(permissionPublicId)),
                permissionKey = NormalizeRequired(permissionKey, nameof(permissionKey)),
                permissionModule = NormalizeOptional(permissionModule),
                permissionAction = NormalizeOptional(permissionAction),
                permissionIsSystem,
                permissionIsActive,
                actorInternalId,
                occurredAtUtc
            },
            summary);
    }

    private AuditNormalizedEvent CreateNormalizedEvent<TPayload>(
        AuditEventNormalizationContext context,
        AuditActor actor,
        AuditResource resource,
        TPayload normalizedPayload,
        string summary)
    {
        var actionClassification = ClassifyAction(context.SourceEvent.EventType);
        var riskClassification = ClassifyRisk(context.SourceEvent.EventType, actionClassification);

        return new AuditNormalizedEvent(
            Actor: actor,
            Resource: resource,
            ActionClassification: actionClassification,
            RiskClassification: riskClassification,
            RequestContext: AuditRequestContext.Empty(),
            JsonPayload: AuditJsonPayload.Create(
                metadataJson: null,
                headersJson: context.HeadersJson,
                sanitizedPayloadJson: Serialize(normalizedPayload),
                beforeJson: null,
                afterJson: null,
                changesJson: null),
            Summary: summary,
            Reason: null);
    }

    private static object BuildRolePermissionPayload(
        string rolePublicId,
        string roleName,
        string? roleDisplayName,
        bool roleIsSystem,
        string permissionPublicId,
        string permissionKey,
        string? permissionModule,
        string? permissionAction,
        bool permissionIsSystem,
        long? actorInternalId,
        DateTime occurredAtUtc)
    {
        return new
        {
            rolePublicId = NormalizeRequired(rolePublicId, nameof(rolePublicId)),
            roleName = NormalizeRequired(roleName, nameof(roleName)),
            roleDisplayName = NormalizeOptional(roleDisplayName),
            roleIsSystem,
            permissionPublicId = NormalizeRequired(permissionPublicId, nameof(permissionPublicId)),
            permissionKey = NormalizeRequired(permissionKey, nameof(permissionKey)),
            permissionModule = NormalizeOptional(permissionModule),
            permissionAction = NormalizeOptional(permissionAction),
            permissionIsSystem,
            actorInternalId,
            occurredAtUtc
        };
    }

    private static AuditActor BuildAdminOrSystemActor(
        long? actorInternalId)
    {
        if (actorInternalId is null)
        {
            return AuditActor.System(AuditSourceModules.Authorization);
        }

        return AuditActor.Create(
            actorInternalId: actorInternalId,
            actorUserId: null,
            actorEmail: null,
            actorDisplayName: null,
            actorType: AuditActorTypes.Admin);
    }

    private static AuditResource BuildUserRoleResource(
        long userInternalId,
        long roleInternalId,
        string roleName,
        string? roleDisplayName)
    {
        return AuditResource.Create(
            AuditResourceTypes.UserRole,
            $"{userInternalId}:{roleInternalId}",
            ResolveDisplayName(roleDisplayName, roleName));
    }

    private static AuditResource BuildRolePermissionResource(
        string rolePublicId,
        string permissionPublicId,
        string roleName,
        string? roleDisplayName,
        string permissionKey)
    {
        var normalizedRolePublicId = NormalizeRequired(
            rolePublicId,
            nameof(rolePublicId));

        var normalizedPermissionPublicId = NormalizeRequired(
            permissionPublicId,
            nameof(permissionPublicId));

        return AuditResource.Create(
            AuditResourceTypes.RolePermission,
            $"{normalizedRolePublicId}:{normalizedPermissionPublicId}",
            $"{ResolveDisplayName(roleDisplayName, roleName)} / {NormalizeRequired(permissionKey, nameof(permissionKey))}");
    }

    private static string ResolveDisplayName(
        string? displayName,
        string fallback)
    {
        return NormalizeOptional(displayName) ??
            NormalizeRequired(fallback, nameof(fallback));
    }

    private static bool IsEventType(
        string eventType,
        string expectedEventType)
    {
        return string.Equals(eventType, expectedEventType, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeRequired(
        string? value,
        string parameterName)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException(
                $"Authorization audit payload field '{parameterName}' is required.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(
        string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }
}
