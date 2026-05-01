namespace Authorization.Application.Outbox.Payloads;

public sealed record RoleCreatedIntegrationEventPayload(
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