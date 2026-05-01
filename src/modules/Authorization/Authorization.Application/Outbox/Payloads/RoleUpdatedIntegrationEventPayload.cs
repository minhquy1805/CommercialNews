namespace Authorization.Application.Outbox.Payloads;

public sealed record RoleUpdatedIntegrationEventPayload(
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