namespace Authorization.Application.Outbox.Payloads;

public sealed record RoleActivatedIntegrationEventPayload(
    long RoleId,
    string RolePublicId,
    string RoleName,
    string? RoleDisplayName,
    bool RoleIsSystem,
    long? ActivatedByUserId,
    DateTime ActivatedAtUtc,
    string BusinessDedupeKey);