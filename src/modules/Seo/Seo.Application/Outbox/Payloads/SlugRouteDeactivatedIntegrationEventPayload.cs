namespace Seo.Application.Outbox.Payloads;

public sealed record SlugRouteDeactivatedIntegrationEventPayload(
    string Scope,
    string ResourceType,
    string ResourcePublicId,
    string Slug,
    string? CanonicalUrl,
    bool IsActive,
    bool IsIndexable,
    long? ActorUserId,
    long Version,
    DateTime DeactivatedAtUtc,
    string BusinessDedupeKey);
