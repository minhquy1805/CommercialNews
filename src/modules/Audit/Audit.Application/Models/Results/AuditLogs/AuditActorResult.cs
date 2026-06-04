namespace Audit.Application.Models.Results.AuditLogs;

public sealed record AuditActorResult(
    long? ActorInternalId,
    string? ActorUserId,
    string? ActorEmail,
    string? ActorDisplayName,
    string ActorType);