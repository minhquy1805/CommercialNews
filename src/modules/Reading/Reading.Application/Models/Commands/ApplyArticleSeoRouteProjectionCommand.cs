namespace Reading.Application.Models.Commands;

public sealed record ApplyArticleSeoRouteProjectionCommand(
    string Scope,
    string ResourceType,
    string ResourcePublicId,
    string Slug,
    string? CanonicalUrl,
    bool IsActive,
    bool IsIndexable,
    long SourceVersion,
    string? MessageId,
    DateTime? SourceOccurredAtUtc);