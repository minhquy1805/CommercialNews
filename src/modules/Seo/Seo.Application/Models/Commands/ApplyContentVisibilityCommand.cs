namespace Seo.Application.Models.Commands;

public sealed record ApplyContentVisibilityCommand(
    string Scope,
    string? Slug,
    string ResourceType,
    string ResourcePublicId,
    string? CanonicalUrl,
    bool IsIndexable,
    bool IsActive,
    long SourceAggregateVersion,
    string LastAppliedMessageId,
    DateTime? LastSyncedAtUtc);