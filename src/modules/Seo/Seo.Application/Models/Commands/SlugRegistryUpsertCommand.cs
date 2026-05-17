namespace Seo.Application.Models.Commands;

public sealed record SlugRegistryUpsertCommand(
    string Scope,
    string Slug,
    string ResourceType,
    string ResourcePublicId,
    string? CanonicalUrl,
    bool IsIndexable,
    bool IsActive,
    long? ActorUserId,
    int? ExpectedVersion);