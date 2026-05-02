namespace Audit.Application.Consumers.Authorization.Payloads;

public sealed record RoleDeactivatedAuditPayload(
    long RoleId,
    string RolePublicId,
    string RoleName,
    string? RoleDisplayName,
    bool RoleIsSystem,
    long? DeactivatedByUserId,
    DateTime DeactivatedAtUtc,
    string BusinessDedupeKey);