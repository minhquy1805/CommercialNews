namespace Authorization.Application.Outbox.Payloads;

public sealed record UserRoleRevokedIntegrationEventPayload(
    long UserId,
    long RoleId,
    string RolePublicId,
    string RoleName,
    string? RoleDisplayName,
    bool RoleIsSystem,
    long? RevokedByUserId,
    DateTime RevokedAtUtc,
    string BusinessDedupeKey);