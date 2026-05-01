namespace Authorization.Application.Outbox.Payloads;

public sealed record RoleDeactivatedIntegrationEventPayload(
    long RoleId,
    string RolePublicId,
    string RoleName,
    string? RoleDisplayName,
    bool RoleIsSystem,
    long? DeactivatedByUserId,
    DateTime DeactivatedAtUtc,
    string BusinessDedupeKey);