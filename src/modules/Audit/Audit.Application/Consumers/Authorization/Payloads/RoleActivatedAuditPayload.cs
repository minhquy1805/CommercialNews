namespace Audit.Application.Consumers.Authorization.Payloads;

public sealed record RoleActivatedAuditPayload(
    long RoleId,
    string RolePublicId,
    string RoleName,
    string? RoleDisplayName,
    bool RoleIsSystem,
    long? ActivatedByUserId,
    DateTime ActivatedAtUtc,
    string BusinessDedupeKey);