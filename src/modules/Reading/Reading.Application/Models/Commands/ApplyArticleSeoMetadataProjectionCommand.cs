namespace Reading.Application.Models.Commands;

public sealed record ApplyArticleSeoMetadataProjectionCommand(
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
    long SourceVersion,
    string? MessageId,
    DateTime? SourceOccurredAtUtc);