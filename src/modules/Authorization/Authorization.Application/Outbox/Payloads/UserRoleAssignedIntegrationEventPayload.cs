namespace Authorization.Application.Outbox.Payloads;

public sealed record UserRoleAssignedIntegrationEventPayload(
    long UserId,
    long RoleId,
    string RolePublicId,
    string RoleName,
    string? RoleDisplayName,
    bool RoleIsSystem,
    long? AssignedByUserId,
    DateTime AssignedAtUtc,
    string BusinessDedupeKey);