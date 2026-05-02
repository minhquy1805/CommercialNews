namespace Audit.Application.Consumers.Authorization.Payloads;

public sealed record UserRoleRevokedAuditPayload(
    long UserId,
    long RoleId,
    string RolePublicId,
    string RoleName,
    string? RoleDisplayName,
    bool RoleIsSystem,
    long? RevokedByUserId,
    DateTime RevokedAtUtc,
    string BusinessDedupeKey);