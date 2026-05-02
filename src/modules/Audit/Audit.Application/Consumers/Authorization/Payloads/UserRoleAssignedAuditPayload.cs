namespace Audit.Application.Consumers.Authorization.Payloads;

public sealed record UserRoleAssignedAuditPayload(
    long UserId,
    long RoleId,
    string RolePublicId,
    string RoleName,
    string? RoleDisplayName,
    bool RoleIsSystem,
    long? AssignedByUserId,
    DateTime AssignedAtUtc,
    string BusinessDedupeKey);