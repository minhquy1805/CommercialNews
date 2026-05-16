namespace Seo.Application.Models.Commands;

public sealed record SeoMetadataUpsertCommand(
    string Scope,
    string ResourceType,
    string ResourcePublicId,
    string? Slug,
    string? CanonicalUrl,
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
    long? UpdatedByUserId,
    int? ExpectedVersion);