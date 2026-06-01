using Audit.Domain.Constants.AuditLog;
using Audit.Domain.Constants.Common;
using Audit.Domain.Exceptions;

namespace Audit.Domain.ValueObjects.Evidence;

public sealed record AuditActor
{
    public long? ActorInternalId { get; }
    public string? ActorUserId { get; }
    public string? ActorEmail { get; }
    public string? ActorDisplayName { get; }
    public string ActorType { get; }

    private AuditActor(
        long? actorInternalId,
        string? actorUserId,
        string? actorEmail,
        string? actorDisplayName,
        string actorType)
    {
        ActorInternalId = actorInternalId;
        ActorUserId = actorUserId;
        ActorEmail = actorEmail;
        ActorDisplayName = actorDisplayName;
        ActorType = actorType;
    }

    public static AuditActor Create(
        long? actorInternalId,
        string? actorUserId,
        string? actorEmail,
        string? actorDisplayName,
        string? actorType)
    {
        var normalizedActorUserId = NormalizeOptional(actorUserId);
        if (normalizedActorUserId is not null &&
            normalizedActorUserId.Length != AuditConstants.PublicIdLength)
        {
            throw AuditDomainException.ActorUserIdInvalidLength();
        }

        var normalizedActorEmail = NormalizeOptional(actorEmail);
        if (normalizedActorEmail is not null &&
            normalizedActorEmail.Length > AuditConstants.MaxActorEmailLength)
        {
            throw AuditDomainException.ActorEmailTooLong();
        }

        var normalizedActorDisplayName = NormalizeOptional(actorDisplayName);
        if (normalizedActorDisplayName is not null &&
            normalizedActorDisplayName.Length > AuditConstants.MaxActorDisplayNameLength)
        {
            throw AuditDomainException.ActorDisplayNameTooLong();
        }

        var normalizedActorType = actorType?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedActorType))
        {
            throw AuditDomainException.ActorTypeRequired();
        }

        if (!AuditActorTypes.IsValid(normalizedActorType))
        {
            throw AuditDomainException.ActorTypeInvalid(normalizedActorType);
        }

        return new AuditActor(
            actorInternalId,
            normalizedActorUserId,
            normalizedActorEmail,
            normalizedActorDisplayName,
            normalizedActorType);
    }

    public static AuditActor System(string? displayName = null)
    {
        return Create(
            actorInternalId: null,
            actorUserId: null,
            actorEmail: null,
            actorDisplayName: displayName,
            actorType: AuditActorTypes.System);
    }

    public static AuditActor Worker(string? displayName = null)
    {
        return Create(
            actorInternalId: null,
            actorUserId: null,
            actorEmail: null,
            actorDisplayName: displayName,
            actorType: AuditActorTypes.Worker);
    }

    public static AuditActor Anonymous()
    {
        return Create(
            actorInternalId: null,
            actorUserId: null,
            actorEmail: null,
            actorDisplayName: null,
            actorType: AuditActorTypes.Anonymous);
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }
}