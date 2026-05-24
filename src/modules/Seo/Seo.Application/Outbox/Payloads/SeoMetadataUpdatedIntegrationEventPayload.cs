namespace Seo.Application.Outbox.Payloads;

public sealed record SeoMetadataUpdatedIntegrationEventPayload(
    string Scope,
    string ResourceType,
    string ResourcePublicId,
    string? MetaTitle,
    string? MetaDescription,
    string? OgTitle,
    string? OgDescription,
    string? OgImageUrl,
    string? TwitterTitle,
    string? TwitterDescription,
    string? TwitterImageUrl,
    string? Robots,
    bool IsManualOverride,
    long? ActorUserId,
    long Version,
    DateTime UpdatedAtUtc,
    string BusinessDedupeKey);
