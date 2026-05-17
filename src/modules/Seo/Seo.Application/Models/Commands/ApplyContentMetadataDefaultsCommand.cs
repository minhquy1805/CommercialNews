namespace Seo.Application.Models.Commands;

public sealed record ApplyContentMetadataDefaultsCommand(
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
    long SourceAggregateVersion,
    string LastAppliedMessageId,
    DateTime? LastSyncedAtUtc);