using Audit.Application.Abstractions.Serialization;
using Audit.Application.Services.Normalization;
using Audit.Domain.Constants.AuditLog;
using Audit.Domain.Constants.Events;
using Audit.Domain.Policies.Evidence;
using Audit.Domain.ValueObjects.Evidence;
using Audit.Infrastructure.Normalization.Common;
using Audit.Infrastructure.Normalization.Identity.EventPayloads;

namespace Audit.Infrastructure.Normalization.Identity;

internal sealed class IdentityAuditEventNormalizer : AuditNormalizerBase
{
    public override string SourceModule => AuditSourceModules.Identity;

    public IdentityAuditEventNormalizer(
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
        return AuditEventTypes.IsIdentityEvent(eventType);
    }

    protected override AuditNormalizedEvent NormalizeCore(
        AuditEventNormalizationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var eventType = NormalizeRequired(
            context.SourceEvent.EventType,
            nameof(context.SourceEvent.EventType));

        if (IsEventType(eventType, AuditEventTypes.IdentityEmailVerified))
        {
            return NormalizeEmailVerified(context);
        }

        if (IsEventType(eventType, AuditEventTypes.IdentityPasswordChanged))
        {
            return NormalizePasswordChanged(context);
        }

        if (IsEventType(eventType, AuditEventTypes.IdentityEmailMarkedVerified))
        {
            return NormalizeEmailMarkedVerified(context);
        }

        if (IsEventType(eventType, AuditEventTypes.IdentityUserActivated))
        {
            return NormalizeUserActivated(context);
        }

        if (IsEventType(eventType, AuditEventTypes.IdentityUserDisabled))
        {
            return NormalizeUserDisabled(context);
        }

        if (IsEventType(eventType, AuditEventTypes.IdentityUserLocked))
        {
            return NormalizeUserLocked(context);
        }

        if (IsEventType(eventType, AuditEventTypes.IdentityUserSessionsRevoked))
        {
            return NormalizeUserSessionsRevoked(context);
        }

        if (IsEventType(eventType, AuditEventTypes.IdentityUserUnlocked))
        {
            return NormalizeUserUnlocked(context);
        }

        throw new InvalidOperationException(
            $"Unsupported identity audit event type '{context.SourceEvent.EventType}'.");
    }

    private AuditNormalizedEvent NormalizeEmailVerified(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<EmailVerifiedAuditPayload>(context);

        var actor = BuildSelfUserActor(
            userInternalId: payload.UserId,
            userPublicId: payload.UserPublicId,
            email: payload.Email,
            fullName: payload.FullName);

        var resource = BuildTargetUserResource(
            userPublicId: payload.UserPublicId,
            email: payload.Email,
            fullName: payload.FullName);

        return CreateNormalizedEvent(
            context: context,
            actor: actor,
            resource: resource,
            jsonPayload: BuildIdentityJsonPayload(
                context,
                new
                {
                    userPublicId = NormalizeRequired(
                        payload.UserPublicId,
                        nameof(payload.UserPublicId)),
                    emailDomain = ResolveEmailDomain(payload.Email),
                    verifiedAtUtc = payload.VerifiedAtUtc
                }),
            summary: "User email was verified.",
            reason: null);
    }

    private AuditNormalizedEvent NormalizePasswordChanged(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<PasswordChangedAuditPayload>(context);

        var actor = BuildSelfUserActor(
            userInternalId: payload.UserId,
            userPublicId: payload.UserPublicId,
            email: payload.Email,
            fullName: payload.FullName);

        var resource = BuildTargetUserResource(
            userPublicId: payload.UserPublicId,
            email: payload.Email,
            fullName: payload.FullName);

        return CreateNormalizedEvent(
            context: context,
            actor: actor,
            resource: resource,
            jsonPayload: BuildIdentityJsonPayload(
                context,
                new
                {
                    userPublicId = NormalizeRequired(
                        payload.UserPublicId,
                        nameof(payload.UserPublicId)),
                    emailDomain = ResolveEmailDomain(payload.Email),
                    reason = NormalizeOptional(payload.Reason),
                    changedAtUtc = payload.ChangedAtUtc
                }),
            summary: "User password was changed.",
            reason: payload.Reason);
    }

    private AuditNormalizedEvent NormalizeEmailMarkedVerified(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<EmailMarkedVerifiedAuditPayload>(context);

        var actor = BuildAdminActor(
            actorInternalId: payload.ActorUserId);

        var resource = BuildTargetUserResource(
            userPublicId: payload.TargetUserPublicId,
            email: payload.TargetEmail,
            fullName: payload.TargetFullName);

        return CreateNormalizedEvent(
            context: context,
            actor: actor,
            resource: resource,
            jsonPayload: BuildIdentityJsonPayload(
                context,
                new
                {
                    targetUserPublicId = NormalizeRequired(
                        payload.TargetUserPublicId,
                        nameof(payload.TargetUserPublicId)),
                    targetEmailDomain = ResolveEmailDomain(payload.TargetEmail),
                    reason = NormalizeOptional(payload.Reason),
                    wasAlreadyVerified = payload.WasAlreadyVerified,
                    previousStatus = NormalizeOptional(payload.PreviousStatus),
                    newStatus = NormalizeOptional(payload.NewStatus),
                    markedVerifiedAtUtc = payload.MarkedVerifiedAtUtc
                }),
            summary: "User email was marked as verified.",
            reason: payload.Reason);
    }

    private AuditNormalizedEvent NormalizeUserActivated(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<UserActivatedAuditPayload>(context);

        var actor = BuildAdminActor(
            actorInternalId: payload.ActorUserId);

        var resource = BuildTargetUserResource(
            userPublicId: payload.TargetUserPublicId,
            email: payload.TargetEmail,
            fullName: payload.TargetFullName);

        return CreateNormalizedEvent(
            context: context,
            actor: actor,
            resource: resource,
            jsonPayload: BuildIdentityJsonPayload(
                context,
                new
                {
                    targetUserPublicId = NormalizeRequired(
                        payload.TargetUserPublicId,
                        nameof(payload.TargetUserPublicId)),
                    targetEmailDomain = ResolveEmailDomain(payload.TargetEmail),
                    reason = NormalizeOptional(payload.Reason),
                    previousStatus = NormalizeOptional(payload.PreviousStatus),
                    newStatus = NormalizeOptional(payload.NewStatus),
                    activatedAtUtc = payload.ActivatedAtUtc
                }),
            summary: "User account was activated.",
            reason: payload.Reason);
    }

    private AuditNormalizedEvent NormalizeUserDisabled(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<UserDisabledAuditPayload>(context);

        var actor = BuildAdminActor(
            actorInternalId: payload.ActorUserId);

        var resource = BuildTargetUserResource(
            userPublicId: payload.TargetUserPublicId,
            email: payload.TargetEmail,
            fullName: payload.TargetFullName);

        return CreateNormalizedEvent(
            context: context,
            actor: actor,
            resource: resource,
            jsonPayload: BuildIdentityJsonPayload(
                context,
                new
                {
                    targetUserPublicId = NormalizeRequired(
                        payload.TargetUserPublicId,
                        nameof(payload.TargetUserPublicId)),
                    targetEmailDomain = ResolveEmailDomain(payload.TargetEmail),
                    reason = NormalizeOptional(payload.Reason),
                    previousStatus = NormalizeOptional(payload.PreviousStatus),
                    newStatus = NormalizeOptional(payload.NewStatus),
                    sessionsRevoked = payload.SessionsRevoked,
                    revokedSessionCount = payload.RevokedSessionCount,
                    disabledAtUtc = payload.DisabledAtUtc
                }),
            summary: "User account was disabled.",
            reason: payload.Reason);
    }

    private AuditNormalizedEvent NormalizeUserLocked(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<UserLockedAuditPayload>(context);

        var actor = BuildAdminActor(
            actorInternalId: payload.ActorUserId);

        var resource = BuildTargetUserResource(
            userPublicId: payload.TargetUserPublicId,
            email: payload.TargetEmail,
            fullName: payload.TargetFullName);

        return CreateNormalizedEvent(
            context: context,
            actor: actor,
            resource: resource,
            jsonPayload: BuildIdentityJsonPayload(
                context,
                new
                {
                    targetUserPublicId = NormalizeRequired(
                        payload.TargetUserPublicId,
                        nameof(payload.TargetUserPublicId)),
                    targetEmailDomain = ResolveEmailDomain(payload.TargetEmail),
                    reason = NormalizeOptional(payload.Reason),
                    previousStatus = NormalizeOptional(payload.PreviousStatus),
                    newStatus = NormalizeOptional(payload.NewStatus),
                    lockedUntilUtc = payload.LockedUntilUtc,
                    sessionsRevoked = payload.SessionsRevoked,
                    revokedSessionCount = payload.RevokedSessionCount,
                    lockedAtUtc = payload.LockedAtUtc
                }),
            summary: "User account was locked.",
            reason: payload.Reason);
    }

    private AuditNormalizedEvent NormalizeUserSessionsRevoked(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<UserSessionsRevokedAuditPayload>(context);

        var actor = BuildAdminActor(
            actorInternalId: payload.ActorUserId);

        var resource = BuildTargetUserResource(
            userPublicId: payload.TargetUserPublicId,
            email: payload.TargetEmail,
            fullName: payload.TargetFullName);

        return CreateNormalizedEvent(
            context: context,
            actor: actor,
            resource: resource,
            jsonPayload: BuildIdentityJsonPayload(
                context,
                new
                {
                    targetUserPublicId = NormalizeRequired(
                        payload.TargetUserPublicId,
                        nameof(payload.TargetUserPublicId)),
                    targetEmailDomain = ResolveEmailDomain(payload.TargetEmail),
                    reason = NormalizeOptional(payload.Reason),
                    revokedSessionCount = payload.RevokedSessionCount,
                    revokedAtUtc = payload.RevokedAtUtc
                }),
            summary: "User sessions were revoked.",
            reason: payload.Reason);
    }

    private AuditNormalizedEvent NormalizeUserUnlocked(
        AuditEventNormalizationContext context)
    {
        var payload = RequirePayload<UserUnlockedAuditPayload>(context);

        var actor = BuildAdminActor(
            actorInternalId: payload.ActorUserId);

        var resource = BuildTargetUserResource(
            userPublicId: payload.TargetUserPublicId,
            email: payload.TargetEmail,
            fullName: payload.TargetFullName);

        return CreateNormalizedEvent(
            context: context,
            actor: actor,
            resource: resource,
            jsonPayload: BuildIdentityJsonPayload(
                context,
                new
                {
                    targetUserPublicId = NormalizeRequired(
                        payload.TargetUserPublicId,
                        nameof(payload.TargetUserPublicId)),
                    targetEmailDomain = ResolveEmailDomain(payload.TargetEmail),
                    reason = NormalizeOptional(payload.Reason),
                    previousStatus = NormalizeOptional(payload.PreviousStatus),
                    newStatus = NormalizeOptional(payload.NewStatus),
                    unlockedAtUtc = payload.UnlockedAtUtc
                }),
            summary: "User account was unlocked.",
            reason: payload.Reason);
    }

    private AuditNormalizedEvent CreateNormalizedEvent(
        AuditEventNormalizationContext context,
        AuditActor actor,
        AuditResource resource,
        AuditJsonPayload jsonPayload,
        string summary,
        string? reason)
    {
        var actionClassification = ClassifyAction(
            context.SourceEvent.EventType);

        var riskClassification = ClassifyRisk(
            context.SourceEvent.EventType,
            actionClassification);

        return new AuditNormalizedEvent(
            Actor: actor,
            Resource: resource,
            ActionClassification: actionClassification,
            RiskClassification: riskClassification,
            RequestContext: AuditRequestContext.Empty(),
            JsonPayload: jsonPayload,
            Summary: summary,
            Reason: NormalizeOptional(reason));
    }

    private AuditJsonPayload BuildIdentityJsonPayload<TPayload>(
        AuditEventNormalizationContext context,
        TPayload normalizedPayload)
    {
        return AuditJsonPayload.Create(
            metadataJson: null,
            headersJson: context.HeadersJson,
            sanitizedPayloadJson: Serialize(normalizedPayload),
            beforeJson: null,
            afterJson: null,
            changesJson: null);
    }

    private static AuditActor BuildSelfUserActor(
        long userInternalId,
        string userPublicId,
        string email,
        string? fullName)
    {
        return AuditActor.Create(
            actorUserId: NormalizeRequired(userPublicId, nameof(userPublicId)),
            actorInternalId: userInternalId,
            actorEmail: NormalizeOptional(email),
            actorDisplayName: ResolveDisplayName(
                fullName,
                email),
            actorType: AuditActorTypes.User);
    }

    private static AuditActor BuildAdminActor(
        long actorInternalId)
    {
        return AuditActor.Create(
            actorUserId: null,
            actorInternalId: actorInternalId,
            actorEmail: null,
            actorDisplayName: null,
            actorType: AuditActorTypes.Admin);
    }

    private static AuditResource BuildTargetUserResource(
        string userPublicId,
        string email,
        string? fullName)
    {
        return AuditResource.Create(
            resourceType: AuditResourceTypes.UserAccount,
            resourceId: NormalizeRequired(userPublicId, nameof(userPublicId)),
            resourceDisplayName: ResolveDisplayName(
                fullName,
                email));
    }

    private static string? ResolveDisplayName(
        string? fullName,
        string? email)
    {
        var normalizedFullName = NormalizeOptional(fullName);

        if (normalizedFullName is not null)
        {
            return normalizedFullName;
        }

        return NormalizeOptional(email);
    }

    private static string NormalizeRequired(
        string? value,
        string parameterName)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException(
                $"Identity audit payload field '{parameterName}' is required.");
        }

        return normalized;
    }

    private static bool IsEventType(
        string eventType,
        string expectedEventType)
    {
        return string.Equals(
            eventType,
            expectedEventType,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveEmailDomain(
        string? email)
    {
        var normalizedEmail = NormalizeOptional(email);

        if (normalizedEmail is null)
        {
            return null;
        }

        int separatorIndex = normalizedEmail.LastIndexOf('@');
        if (separatorIndex < 0 || separatorIndex == normalizedEmail.Length - 1)
        {
            return null;
        }

        return normalizedEmail[(separatorIndex + 1)..].ToLowerInvariant();
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
