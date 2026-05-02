namespace Audit.Application.Consumers.Authorization.Payloads;

public sealed record RoleCreatedAuditPayload(
    long RoleId,
    string RolePublicId,
    string RoleName,
    string RoleNameNormalized,
    string? RoleDisplayName,
    string? RoleDescription,
    bool RoleIsSystem,
    bool RoleIsActive,
    long? CreatedByUserId,
    DateTime CreatedAtUtc,
    string BusinessDedupeKey);