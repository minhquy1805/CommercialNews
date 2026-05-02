namespace Audit.Application.Consumers.Authorization.Payloads;

public sealed record RoleUpdatedAuditPayload(
    long RoleId,
    string RolePublicId,
    string RoleName,
    string RoleNameNormalized,
    string? RoleDisplayName,
    string? RoleDescription,
    bool RoleIsSystem,
    bool RoleIsActive,
    long? UpdatedByUserId,
    DateTime UpdatedAtUtc,
    string BusinessDedupeKey);